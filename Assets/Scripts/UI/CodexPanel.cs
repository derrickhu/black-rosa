using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴：「字谱」收单字和词，「秘卷」收招牌两两。
    // 一格三种样子：用过 = 彩色字 + 名；见过没用 = 灰字 + 锁；没见过 = 问号。
    public sealed class CodexPanel : MonoBehaviour
    {
        const float BoardW = 660f;
        const float BoardH = 1060f;
        const int Cols = 4;
        const float CellW = 138f;
        const float CellH = 168f;
        const float Gap = 12f;
        const float ListTop = 226f;

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _body;
        bool _secret;
        bool[] _seen;
        float _scroll = -1f;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "codex";
            var panel = dim.gameObject.AddComponent<CodexPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel._seen = CodexCatalog.SeenCards(meta);
            panel._board = PanelKit.Board(dim, "图鉴", new Vector2(0f, -10f), new Vector2(BoardW, BoardH), Pin.Center, panel.Close);
            panel.Refresh();
        }

        void Close()
        {
            AudioBus.Tap();
            _changed?.Invoke();
            Destroy(gameObject);
        }

        void Refresh()
        {
            if (_body != null) Destroy(_body.gameObject);
            _body = UiKit.Panel(_board, "body", Vector2.zero, Vector2.zero, Color.clear);
            _body.anchorMin = Vector2.zero;
            _body.anchorMax = Vector2.one;
            _body.offsetMin = _body.offsetMax = Vector2.zero;
            _body.GetComponent<Image>().raycastTarget = false;
            _body.SetSiblingIndex(0);

            IReadOnlyList<CodexEntry> baseList = CodexCatalog.Base;
            IReadOnlyList<CodexEntry> pairs = CodexCatalog.Pairs;
            Tab("tab_base", $"字谱 {CodexCatalog.CountKnown(_meta, baseList)}/{baseList.Count}", -136f, !_secret,
                _meta.CodexBaseNew, () => Switch(false));
            Tab("tab_secret", $"秘卷 {CodexCatalog.CountKnown(_meta, pairs)}/{pairs.Count}", 136f, _secret,
                _meta.CodexPairNew, () => Switch(true));

            string hint = _secret
                ? "两个字叠在同一发炮弹上，会化出新的形"
                : "在关卡里用过一次的字，才会显出真本事";
            var h = UiKit.Label(_body, "hint", hint, 23, new Vector2(0f, 186f), new Vector2(BoardW - 60f, 30f),
                TextAnchor.MiddleCenter, Pin.Top);
            h.color = InkTheme.TextMid;

            BuildGrid(_secret ? pairs : baseList);
        }

        void Switch(bool secret)
        {
            if (_secret == secret) return;
            AudioBus.Tap();
            _secret = secret;
            _scroll = -1f;
            Refresh();
        }

        void Tab(string name, string text, float x, bool on, bool dot, Action tap)
        {
            var size = new Vector2(262f, 74f);
            var tab = UiKit.Stroke(_body, name, new Vector2(x, 94f), size, Pin.Top, on ? 5f : 3f,
                InkTheme.Outline, on ? InkTheme.Seal : InkTheme.CardFace, 37f);
            var t = UiKit.Label(tab, "t", text, 30, Vector2.zero, size);
            t.color = on ? InkTheme.CardFace : InkTheme.TextDark;
            UiKit.Bold(t);
            var btn = tab.gameObject.AddComponent<Button>();
            btn.targetGraphic = tab.GetComponent<Image>();
            btn.onClick.AddListener(() => tap());
            if (dot) Dot(tab, new Vector2(size.x * 0.5f - 16f, size.y * 0.5f - 10f));
        }

        void BuildGrid(IReadOnlyList<CodexEntry> list)
        {
            var vp = new GameObject("list", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            vp.transform.SetParent(_body, false);
            var viewport = vp.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(16f, 28f);
            viewport.offsetMax = new Vector2(-16f, -ListTop);
            vp.GetComponent<Image>().color = Color.clear;

            var go = new GameObject("cells", typeof(RectTransform));
            go.transform.SetParent(viewport, false);
            var content = go.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            int rows = (list.Count + Cols - 1) / Cols;
            content.sizeDelta = new Vector2(0f, rows * (CellH + Gap) + 16f);

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            for (int i = 0; i < list.Count; i++) Cell(content, list[i], i);
            Canvas.ForceUpdateCanvases();
            if (_scroll >= 0f) scroll.verticalNormalizedPosition = _scroll;
            scroll.onValueChanged.AddListener(v => _scroll = v.y);
        }

        void Cell(RectTransform content, CodexEntry e, int i)
        {
            float x = (i % Cols - (Cols - 1) * 0.5f) * (CellW + Gap);
            float y = 8f + (i / Cols) * (CellH + Gap);
            CodexState st = CodexCatalog.StateOf(_meta, e, _seen);
            bool known = st == CodexState.Known;
            var size = new Vector2(CellW, CellH);
            var card = UiKit.Stroke(content, "c" + i, new Vector2(x, y), size, Pin.Top, known ? 5f : 3f,
                known ? Tone(e) : InkTheme.LineDim, known ? InkTheme.CardFace : InkTheme.CardDim, 20f);
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => Open(e, st));

            if (st == CodexState.Unknown)
            {
                var q = UiKit.Label(card, "q", "？", 76, new Vector2(0f, 16f), new Vector2(CellW, 96f));
                q.color = InkTheme.LineDim;
                UiKit.Bold(q);
                bool hinted = e.Kind == CodexKind.Pair && CodexCatalog.PairHinted(_meta, e.Index);
                var n = UiKit.Label(card, "n", hinted ? "似曾相识" : (e.Kind == CodexKind.Pair ? "？ + ？" : "？？"), 22,
                    new Vector2(0f, -56f), new Vector2(CellW, 30f));
                n.color = hinted ? InkTheme.Gold : InkTheme.TextDim;
                return;
            }

            Color tint = known ? Color.white : new Color(0.55f, 0.52f, 0.48f, 0.55f);
            if (e.Kind == CodexKind.Glyph)
            {
                UiKit.Icon(card, InkArt.Glyph((CardId)e.Index, 160), new Vector2(0f, 16f), 102f).color = tint;
            }
            else if (e.Kind == CodexKind.Word)
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int k = 0; k < parts.Length; k++)
                    UiKit.Icon(card, InkArt.Glyph(parts[k], 160), new Vector2(k == 0 ? -32f : 32f, 16f), 66f).color = tint;
            }
            else
            {
                UiKit.Icon(card, PairFrame(e.Index), new Vector2(0f, 20f), 104f);
                SignaturePair p = SignaturePairs.All[e.Index];
                var parts = UiKit.Label(card, "parts", CardCatalog.Get(p.A).Name + " + " + CardCatalog.Get(p.B).Name, 18,
                    new Vector2(0f, -74f), new Vector2(CellW, 24f));
                parts.color = InkTheme.TextMid;
            }

            var name = UiKit.Label(card, "n", CodexCatalog.Title(e), 28,
                new Vector2(0f, e.Kind == CodexKind.Pair ? -48f : -56f), new Vector2(CellW, 36f));
            name.color = known ? InkTheme.TextDark : InkTheme.TextDim;
            UiKit.Bold(name);

            if (!known)
            {
                var lk = UiKit.Icon(card, InkArt.Icon(InkShape.Lock, 64), new Vector2(CellW * 0.5f - 22f, CellH * 0.5f - 22f), 30f);
                lk.color = InkTheme.GraphiteHi;
            }
            else if (_meta.CodexIsNew(e.Kind, e.Index))
                Dot(card, new Vector2(CellW * 0.5f - 14f, CellH * 0.5f - 14f));
        }

        void Open(CodexEntry e, CodexState st)
        {
            AudioBus.Tap();
            if (st == CodexState.Unknown)
            {
                InkToast.Show(_layer, e.Kind == CodexKind.Pair ? "两字同弹，方见真形" : "还没在关卡里遇见过");
                return;
            }
            if (st == CodexState.Known) _meta.CodexSeen(e.Kind, e.Index);
            CodexDetail.Show(_layer, e, st == CodexState.Known, Refresh);
        }

        static void Dot(Transform parent, Vector2 pos)
        {
            var ring = UiKit.Icon(parent, UiSprites.Disc(), pos, 26f);
            ring.gameObject.name = "dot";
            ring.color = InkTheme.CardFace;
            UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 20f).color = InkTheme.Seal;
        }

        public static Color Tone(CodexEntry e)
        {
            switch (e.Kind)
            {
                case CodexKind.Glyph: return CardCatalog.Accent((CardId)e.Index);
                case CodexKind.Word: return InkTheme.Gold;
                default:
                    SignaturePair p = SignaturePairs.All[e.Index];
                    return Color.Lerp(CardCatalog.Accent(p.A), CardCatalog.Accent(p.B), 0.5f);
            }
        }

        public static Sprite PairFrame(int pair)
        {
            if (!InkVfx.Flat(SignaturePairs.All[pair].Form, out InkVfx.FlatBody body)) return null;
            Sprite[] frames = InkVfx.Frames(body.Frames, body.Count);
            return frames != null && frames.Length > 0 ? frames[0] : null;
        }
    }

    // 点开一格：左边字、右边炮弹，下面是说明和三档数值。点星级那一行，炮弹跟着换。
    public sealed class CodexDetail : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 900f;
        const float RowW = 572f;

        static readonly Color PickFill = InkTheme.Hex("FFF6E6");

        CodexEntry _e;
        bool _known;
        Action _closed;
        RectTransform _board;
        CodexShot _shot;
        Image _glyph;
        readonly List<RectTransform> _rows = new List<RectTransform>();

        public static void Show(RectTransform layer, CodexEntry e, bool known, Action closed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "codex_detail";
            var d = dim.gameObject.AddComponent<CodexDetail>();
            d._e = e;
            d._known = known;
            d._closed = closed;
            d._board = PanelKit.Board(dim, CodexCatalog.Title(e), new Vector2(0f, -10f), new Vector2(BoardW, BoardH), Pin.Center, d.Close);
            d.Build();
        }

        void Close()
        {
            AudioBus.Tap();
            _closed?.Invoke();
            Destroy(gameObject);
        }

        void Build()
        {
            BuildFace();
            BuildShotBox();

            string note;
            string lore;
            if (_e.Kind == CodexKind.Glyph)
            {
                var id = (CardId)_e.Index;
                note = CodexCatalog.WakeNote(id);
                lore = CodexCatalog.Lore(id);
            }
            else if (_e.Kind == CodexKind.Word)
            {
                var w = (WordId)_e.Index;
                note = CodexCatalog.WakeNote(w);
                lore = CodexCatalog.WordLore(w);
            }
            else
            {
                SignaturePair p = SignaturePairs.All[_e.Index];
                note = $"{CardCatalog.Get(p.A).Name} + {CardCatalog.Get(p.B).Name}";
                lore = $"「{CardCatalog.Get(p.A).Name}」和「{CardCatalog.Get(p.B).Name}」叠在同一发炮弹上，炮弹化成这副模样，还多出一点本事。";
            }
            if (!_known)
            {
                note = "";
                lore = "？？？\n在关卡里用上一次，就能看清它的本事。";
            }

            var n = UiKit.Label(_board, "note", note, 22, new Vector2(0f, 366f), new Vector2(RowW, 30f), TextAnchor.MiddleCenter, Pin.Top);
            n.color = InkTheme.TextMid;

            var para = UiKit.Label(_board, "lore", lore, 26, new Vector2(0f, 408f), new Vector2(RowW, 120f), TextAnchor.UpperLeft, Pin.Top);
            para.horizontalOverflow = HorizontalWrapMode.Wrap;
            para.lineSpacing = 1.15f;
            para.color = _known ? InkTheme.TextDark : InkTheme.TextDim;

            if (_e.Kind == CodexKind.Pair) BuildPairEffect();
            else BuildStars();
        }

        void BuildFace()
        {
            var size = new Vector2(220f, 250f);
            var card = UiKit.Stroke(_board, "face", new Vector2(-166f, 100f), size, Pin.Top, 5f,
                _known ? CodexPanel.Tone(_e) : InkTheme.LineDim, _known ? InkTheme.CardFace : InkTheme.CardDim, 22f);
            card.GetComponent<Image>().raycastTarget = false;
            Color tint = _known ? Color.white : new Color(0.55f, 0.52f, 0.48f, 0.55f);
            if (_e.Kind == CodexKind.Glyph)
            {
                _glyph = UiKit.Icon(card, InkArt.Glyph((CardId)_e.Index, 160), Vector2.zero, 180f);
                _glyph.color = tint;
                return;
            }
            CardId[] parts;
            if (_e.Kind == CodexKind.Word) parts = CodexCatalog.PartsOf((WordId)_e.Index);
            else
            {
                SignaturePair p = SignaturePairs.All[_e.Index];
                parts = new[] { p.A, p.B };
            }
            UiKit.Icon(card, InkArt.Glyph(parts[0], 160), new Vector2(0f, 58f), 110f).color = tint;
            UiKit.Icon(card, InkArt.Glyph(parts[1], 160), new Vector2(0f, -58f), 110f).color = tint;
            var plus = UiKit.Label(card, "plus", "+", 34, new Vector2(78f, 0f), new Vector2(40f, 40f));
            plus.color = InkTheme.TextMid;
            UiKit.Bold(plus);
        }

        void BuildShotBox()
        {
            var size = new Vector2(330f, 250f);
            var box = UiKit.Stroke(_board, "shotBox", new Vector2(118f, 100f), size, Pin.Top, 4f,
                InkTheme.Outline, InkTheme.Stage, 22f);
            box.GetComponent<Image>().raycastTarget = false;
            var cap = UiKit.Label(box, "cap", "炮弹", 20, new Vector2(-size.x * 0.5f + 38f, size.y * 0.5f - 22f), new Vector2(60f, 26f));
            cap.color = InkTheme.TextMid;
            if (!_known)
            {
                var q = UiKit.Label(box, "q", "？", 110, new Vector2(0f, 4f), size);
                q.color = InkTheme.LineDim;
                UiKit.Bold(q);
                return;
            }
            _shot = CodexShot.Build(box, size - new Vector2(10f, 10f));
            if (_e.Kind == CodexKind.Glyph) _shot.ShowGlyph((CardId)_e.Index, 1);
            else if (_e.Kind == CodexKind.Word) _shot.ShowWord((WordId)_e.Index);
            else _shot.ShowPair(_e.Index);
            if (_e.Kind == CodexKind.Glyph && !ChangesLook((CardId)_e.Index))
            {
                var t = UiKit.Label(box, "plain", "不改炮弹外形", 20, new Vector2(0f, -size.y * 0.5f + 22f), new Vector2(size.x, 26f));
                t.color = InkTheme.TextMid;
            }
        }

        static bool ChangesLook(CardId id)
        {
            if (id == CardId.Split) return true;
            GlyphDef g = GlyphTable.Get(id);
            return g.Form != ShotFx.None || g.Trail != ShotFx.None || g.Halo != ShotFx.None
                   || g.Orbit != ShotFx.None || g.Bloom != ShotFx.None;
        }

        void BuildStars()
        {
            const float top = 544f;
            const float rowH = 90f;
            var cap = UiKit.Label(_board, "cap", "星级数值", 24, new Vector2(-RowW * 0.5f + 60f, top - 8f), new Vector2(140f, 30f), TextAnchor.MiddleCenter, Pin.Top);
            cap.color = InkTheme.TextDark;
            UiKit.Bold(cap);
            if (_known && _e.Kind == CodexKind.Glyph)
            {
                var tip = UiKit.Label(_board, "tip", "点一行看对应星级的炮弹", 20, new Vector2(RowW * 0.5f - 130f, top - 8f), new Vector2(260f, 30f), TextAnchor.MiddleRight, Pin.Top);
                tip.color = InkTheme.TextDim;
            }
            for (int s = 1; s <= GameConstants.MaxStar; s++)
            {
                int star = s;
                var row = UiKit.Stroke(_board, "s" + s, new Vector2(0f, top + 28f + (s - 1) * (rowH + 8f)), new Vector2(RowW, rowH),
                    Pin.Top, 3f, InkTheme.Outline, InkTheme.CardFace, 18f);
                _rows.Add(row);
                for (int k = 0; k < s; k++)
                    UiKit.Icon(row, InkSprites.Ui("star"), new Vector2(-RowW * 0.5f + 34f + k * 30f, 0f), 30f);
                string text = !_known ? "？？？"
                    : _e.Kind == CodexKind.Glyph ? CodexCatalog.StarLine((CardId)_e.Index, s)
                    : CodexCatalog.WordStarLine((WordId)_e.Index, s);
                var t = UiKit.Label(row, "v", text, 23, new Vector2(62f, 0f), new Vector2(RowW - 150f, rowH - 8f), TextAnchor.MiddleLeft);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.color = _known ? InkTheme.TextDark : InkTheme.TextDim;
                if (_known && _e.Kind == CodexKind.Glyph)
                {
                    var btn = row.gameObject.AddComponent<Button>();
                    btn.targetGraphic = row.GetComponent<Image>();
                    btn.onClick.AddListener(() => PickStar(star));
                }
                else row.GetComponent<Image>().raycastTarget = false;
            }
            if (_known && _e.Kind == CodexKind.Glyph) Highlight(1);
        }

        void PickStar(int star)
        {
            AudioBus.Tap();
            var id = (CardId)_e.Index;
            if (_shot != null) _shot.ShowGlyph(id, star);
            if (_glyph != null)
            {
                Sprite s = InkSprites.Heap(id, star);
                if (s != null) _glyph.sprite = s;
            }
            Highlight(star);
        }

        void Highlight(int star)
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].GetComponent<Image>().color = i + 1 == star ? PickFill : InkTheme.CardFace;
        }

        void BuildPairEffect()
        {
            SignaturePair p = SignaturePairs.All[_e.Index];
            const float top = 544f;
            var cap = UiKit.Label(_board, "cap", "额外效果", 24, new Vector2(-RowW * 0.5f + 60f, top - 8f), new Vector2(140f, 30f), TextAnchor.MiddleCenter, Pin.Top);
            cap.color = InkTheme.TextDark;
            UiKit.Bold(cap);
            var box = UiKit.Stroke(_board, "effect", new Vector2(0f, top + 28f), new Vector2(RowW, 120f), Pin.Top, 4f,
                CodexPanel.Tone(_e), PickFill, 18f);
            box.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(box, "v", p.Note, 28, Vector2.zero, new Vector2(RowW - 40f, 100f));
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.color = InkTheme.TextDark;
            UiKit.Bold(t);
            var how = UiKit.Label(_board, "how", "再叠第三个字也照样生效，炮弹会多挂一颗对应颜色的小珠。", 21,
                new Vector2(0f, top + 176f), new Vector2(RowW, 60f), TextAnchor.UpperCenter, Pin.Top);
            how.horizontalOverflow = HorizontalWrapMode.Wrap;
            how.color = InkTheme.TextMid;
        }
    }
}

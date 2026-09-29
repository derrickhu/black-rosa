using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴三栏：字谱、秘卷、墨谱。每栏头顶一条收集度，攒到印上就能领墨。
    // 一格三种样子：收录 = 彩色；见过未收录 = 灰影 + 锁；没见过 = 问号。
    public sealed class CodexPanel : MonoBehaviour
    {
        const float BoardW = 660f;
        const float Gap = 12f;
        const float ListTop = 348f;

        public static readonly Vector4 CardBorder = new Vector4(36f, 36f, 36f, 36f);
        public static readonly Vector4 CardOnBorder = new Vector4(52f, 52f, 52f, 52f);

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _body;
        CodexTab _tab;
        bool[] _seenCards;
        bool[] _seenEnemies;
        float _scroll = -1f;
        UiAnim _anim;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "codex";
            var panel = dim.gameObject.AddComponent<CodexPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel._seenCards = CodexCatalog.SeenCards(meta);
            panel._seenEnemies = CodexCatalog.SeenEnemies(meta);
            panel._anim = UiAnim.On(panel);
            float top = ScreenFit.TopPad + 48f;
            float bottom = ScreenFit.BottomPad + 28f;
            float h = Mathf.Max(900f, ScreenFit.CanvasH - top - bottom);
            panel._board = PanelKit.Board(dim, "图鉴", new Vector2(0f, top), new Vector2(BoardW, h), Pin.Top, panel.Close);
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

            BuildTabs();
            BuildProgress();
            var hint = UiKit.Label(_body, "hint", CodexCatalog.TabHint(_tab), 21,
                new Vector2(0f, 316f), new Vector2(BoardW - 56f, 28f), TextAnchor.MiddleCenter, Pin.Top);
            hint.color = InkTheme.TextMid;
            BuildGrid(CodexCatalog.Of(_tab));
        }

        void Switch(CodexTab tab)
        {
            if (_tab == tab) return;
            AudioBus.Tap();
            _tab = tab;
            _scroll = -1f;
            Refresh();
        }

        void BuildTabs()
        {
            CodexTab[] tabs = { CodexTab.Glyph, CodexTab.Pair, CodexTab.Enemy };
            float w = 196f;
            float step = 212f;
            for (int i = 0; i < tabs.Length; i++)
            {
                CodexTab tab = tabs[i];
                bool on = _tab == tab;
                var size = new Vector2(w, 70f);
                float x = (i - 1) * step;
                var plate = UiKit.Stroke(_body, "tab" + i, new Vector2(x, 86f), size, Pin.Top, on ? 5f : 3f,
                    InkTheme.Outline, on ? InkTheme.Seal : InkTheme.CardFace, 28f);
                Sprite ico = InkSprites.Load(CodexCatalog.TabIcon(tab));
                if (ico != null)
                    UiKit.Icon(plate, ico, new Vector2(-size.x * 0.5f + 34f, 2f), 40f).color =
                        on ? Color.white : new Color(1f, 1f, 1f, 0.85f);
                var t = UiKit.Label(plate, "t", CodexCatalog.TabName(tab), 28,
                    new Vector2(16f, 0f), new Vector2(size.x - 48f, size.y));
                t.color = on ? InkTheme.CardFace : InkTheme.TextDark;
                UiKit.Bold(t);
                var btn = plate.gameObject.AddComponent<Button>();
                btn.targetGraphic = plate.GetComponent<Image>();
                btn.onClick.AddListener(() => Switch(tab));
                if (_meta.CodexTabNew(tab))
                    Dot(plate, new Vector2(size.x * 0.5f - 14f, size.y * 0.5f - 10f));
            }
        }

        void BuildProgress()
        {
            IReadOnlyList<CodexEntry> list = CodexCatalog.Of(_tab);
            int known = CodexCatalog.CountKnown(_meta, list);
            CodexMile[] miles = CodexCatalog.Miles(_tab);
            float ratio = list.Count > 0 ? (float)known / list.Count : 0f;

            var strip = UiKit.Panel(_body, "prog", new Vector2(0f, 168f), new Vector2(600f, 44f), Color.clear, Pin.Top);
            strip.GetComponent<Image>().raycastTarget = false;
            UiKit.Art(strip, "plaque", "Ui/codex_plaque", Vector2.zero,
                new Vector2(600f, 44f), Pin.Center, new Vector4(48f, 20f, 48f, 20f));
            var bar = ResultKit.Bar(strip, new Vector2(-48f, 0f), 360f, 18f, InkTheme.Seal);
            ResultKit.SetBar(bar, ratio);
            var n = UiKit.Label(strip, "count", known + "/" + list.Count, 22,
                new Vector2(236f, 0f), new Vector2(90f, 28f), TextAnchor.MiddleRight);
            n.color = InkTheme.CardFace;
            UiKit.Bold(n);

            float step = 188f;
            for (int i = 0; i < miles.Length; i++)
            {
                bool reached = known >= miles[i].Need;
                bool claimed = _meta.MileClaimed(_tab, i);
                bool ready = reached && !claimed;
                string key = claimed ? "Ui/codex_seal_on" : ready ? "Ui/codex_seal_ready" : "Ui/codex_seal_off";
                float x = (i - 1) * step;
                var host = UiKit.Panel(_body, "mile" + i, new Vector2(x, 236f), new Vector2(170f, 78f), Color.clear, Pin.Top);
                host.GetComponent<Image>().raycastTarget = true;
                Sprite spr = InkSprites.Load(key);
                if (spr != null) UiKit.Icon(host, spr, new Vector2(-48f, 4f), 58f);
                string line = claimed ? "已领" : ready ? "可领  +" + miles[i].Ink : miles[i].Need + " 枚  +" + miles[i].Ink;
                var lab = UiKit.Label(host, "m", line, 20, new Vector2(28f, 0f), new Vector2(110f, 60f));
                lab.color = claimed ? InkTheme.Seal : ready ? InkTheme.Gold : InkTheme.TextMid;
                UiKit.Bold(lab);
                int idx = i;
                var btn = host.gameObject.AddComponent<Button>();
                btn.targetGraphic = host.GetComponent<Image>();
                btn.onClick.AddListener(() => Claim(idx));
                if (ready) _anim.Breathe(host, 0f, 0.06f, 1.2f);
            }
        }

        void Claim(int i)
        {
            int ink = _meta.ClaimMile(_tab, i);
            if (ink <= 0)
            {
                AudioBus.Tap();
                CodexMile mile = CodexCatalog.Miles(_tab)[i];
                if (_meta.MileClaimed(_tab, i))
                    InkToast.Show(_layer, "这枚印已经领过了");
                else
                    InkToast.Show(_layer, "再收录 " + (mile.Need - CodexCatalog.CountKnown(_meta, CodexCatalog.Of(_tab))) + " 个");
                return;
            }
            AudioBus.Unlock();
            InkToast.Show(_layer, CodexCatalog.TabName(_tab) + "印 · 墨 +" + ink);
            Refresh();
        }

        void BuildGrid(IReadOnlyList<CodexEntry> list)
        {
            int cols = _tab == CodexTab.Enemy ? 3 : 4;
            float cellW = _tab == CodexTab.Enemy ? 186f : 138f;
            float cellH = _tab == CodexTab.Enemy ? 196f : 168f;
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
            int rows = (list.Count + cols - 1) / cols;
            content.sizeDelta = new Vector2(0f, rows * (cellH + Gap) + 16f);

            var scroll = vp.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            for (int i = 0; i < list.Count; i++) Cell(content, list[i], i, cols, cellW, cellH);
            Canvas.ForceUpdateCanvases();
            if (_scroll >= 0f) scroll.verticalNormalizedPosition = _scroll;
            scroll.onValueChanged.AddListener(v => _scroll = v.y);
        }

        void Cell(RectTransform content, CodexEntry e, int i, int cols, float cellW, float cellH)
        {
            float x = (i % cols - (cols - 1) * 0.5f) * (cellW + Gap);
            float y = 8f + (i / cols) * (cellH + Gap);
            bool[] seen = e.Kind == CodexKind.Enemy ? _seenEnemies : _seenCards;
            CodexState st = CodexCatalog.StateOf(_meta, e, seen);
            bool known = st == CodexState.Known;
            var size = new Vector2(cellW, cellH);
            string art = known ? "Ui/codex_card_on" : st == CodexState.Unknown ? "Ui/codex_card_dim" : "Ui/codex_card";
            Vector4 border = known ? CardOnBorder : CardBorder;
            var card = UiKit.Art(content, "c" + i, art, new Vector2(x, y), size, Pin.Top, border);
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card.GetComponent<Image>();
            btn.onClick.AddListener(() => Open(e, st));

            if (st == CodexState.Unknown)
            {
                var q = UiKit.Label(card, "q", "？", 76, new Vector2(0f, 16f), new Vector2(cellW, 96f));
                q.color = InkTheme.LineDim;
                UiKit.Bold(q);
                bool hinted = e.Kind == CodexKind.Pair && CodexCatalog.PairHinted(_meta, e.Index);
                string blank = hinted ? "似曾相识" : e.Kind == CodexKind.Pair ? "？ + ？" : "？？";
                var n = UiKit.Label(card, "n", blank, 22, new Vector2(0f, -cellH * 0.32f), new Vector2(cellW, 30f));
                n.color = hinted ? InkTheme.Gold : InkTheme.TextDim;
                return;
            }

            Color tint = known ? Color.white : new Color(0.55f, 0.52f, 0.48f, 0.55f);
            if (e.Kind == CodexKind.Glyph)
                UiKit.Icon(card, InkArt.Glyph((CardId)e.Index, 160), new Vector2(0f, 16f), 102f).color = tint;
            else if (e.Kind == CodexKind.Word)
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int k = 0; k < parts.Length; k++)
                    UiKit.Icon(card, InkArt.Glyph(parts[k], 160), new Vector2(k == 0 ? -32f : 32f, 16f), 66f).color = tint;
            }
            else if (e.Kind == CodexKind.Enemy)
                UiKit.Icon(card, InkSprites.Person((EnemyId)e.Index), new Vector2(0f, 14f), cellH * 0.52f).color = tint;
            else
            {
                UiKit.Icon(card, PairFrame(e.Index), new Vector2(0f, 20f), 104f);
                SignaturePair p = SignaturePairs.All[e.Index];
                var parts = UiKit.Label(card, "parts", CardCatalog.Get(p.A).Name + " + " + CardCatalog.Get(p.B).Name, 18,
                    new Vector2(0f, -cellH * 0.38f), new Vector2(cellW, 24f));
                parts.color = InkTheme.TextMid;
            }

            var name = UiKit.Label(card, "n", CodexCatalog.Title(e), 26,
                new Vector2(0f, e.Kind == CodexKind.Pair ? -cellH * 0.28f : -cellH * 0.32f), new Vector2(cellW - 8f, 36f));
            name.color = known ? InkTheme.TextDark : InkTheme.TextDim;
            UiKit.Bold(name);

            if (!known)
            {
                var lk = UiKit.Icon(card, InkArt.Icon(InkShape.Lock, 64),
                    new Vector2(cellW * 0.5f - 22f, cellH * 0.5f - 22f), 30f);
                lk.color = InkTheme.GraphiteHi;
            }
            else if (_meta.CodexIsNew(e.Kind, e.Index))
                Dot(card, new Vector2(cellW * 0.5f - 14f, cellH * 0.5f - 14f));
        }

        void Open(CodexEntry e, CodexState st)
        {
            AudioBus.Tap();
            if (st == CodexState.Unknown)
            {
                string tip = e.Kind == CodexKind.Pair ? "两字同弹，方见真形"
                    : e.Kind == CodexKind.Enemy ? "还没在关卡里遇见过" : "还没在关卡里遇见过";
                InkToast.Show(_layer, tip);
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
                case CodexKind.Enemy: return InkTheme.Ink;
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
            if (_e.Kind == CodexKind.Enemy)
            {
                BuildEnemy();
                return;
            }
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

        void BuildEnemy()
        {
            var id = (EnemyId)_e.Index;
            EnemyDef d = EnemyCatalog.Base(id);
            BuildFace();

            var size = new Vector2(330f, 250f);
            var box = UiKit.Art(_board, "stat", "Ui/codex_card", new Vector2(118f, 100f), size, Pin.Top, CodexPanel.CardBorder);
            box.GetComponent<Image>().raycastTarget = false;
            if (!_known)
            {
                var q = UiKit.Label(box, "q", "？", 90, Vector2.zero, size);
                q.color = InkTheme.LineDim;
                UiKit.Bold(q);
            }
            else
            {
                string[] rows =
                {
                    "血  " + Mathf.RoundToInt(d.Hp),
                    "速  " + d.Speed.ToString("0.00"),
                    "金  " + d.Gold,
                    "墨  " + (d.Ink < 0.05f ? "少许" : d.Ink.ToString(d.Ink >= 1f ? "0.#" : "0.00"))
                };
                for (int i = 0; i < rows.Length; i++)
                {
                    var t = UiKit.Label(box, "s" + i, rows[i], 28,
                        new Vector2(0f, 78f - i * 48f), new Vector2(size.x - 40f, 40f), TextAnchor.MiddleLeft);
                    t.color = InkTheme.TextDark;
                    UiKit.Bold(t);
                }
            }

            string lore = _known
                ? EnemyCatalog.Lore(id)
                : "打倒它一次，才写得进墨谱。";
            var para = UiKit.Label(_board, "lore", lore, 26, new Vector2(0f, 380f), new Vector2(RowW, 110f),
                TextAnchor.UpperLeft, Pin.Top);
            para.horizontalOverflow = HorizontalWrapMode.Wrap;
            para.lineSpacing = 1.15f;
            para.color = _known ? InkTheme.TextDark : InkTheme.TextDim;

            string trait = _known ? EnemyCatalog.TraitLine(d) : "？？？";
            var cap = UiKit.Label(_board, "cap", "本事", 24, new Vector2(-RowW * 0.5f + 40f, 520f),
                new Vector2(80f, 30f), TextAnchor.MiddleCenter, Pin.Top);
            cap.color = InkTheme.TextDark;
            UiKit.Bold(cap);
            var traitBox = UiKit.Art(_board, "trait", "Ui/codex_card", new Vector2(0f, 556f),
                new Vector2(RowW, 100f), Pin.Top, CodexPanel.CardBorder);
            traitBox.GetComponent<Image>().raycastTarget = false;
            var tv = UiKit.Label(traitBox, "v", trait, 26, Vector2.zero, new Vector2(RowW - 40f, 80f));
            tv.horizontalOverflow = HorizontalWrapMode.Wrap;
            tv.color = _known ? InkTheme.TextDark : InkTheme.TextDim;
        }

        void BuildFace()
        {
            var size = new Vector2(220f, 250f);
            var card = UiKit.Stroke(_board, "face", new Vector2(-166f, 100f), size, Pin.Top, 5f,
                _known ? CodexPanel.Tone(_e) : InkTheme.LineDim, _known ? InkTheme.CardFace : InkTheme.CardDim, 22f);
            card.GetComponent<Image>().raycastTarget = false;
            Color tint = _known ? Color.white : new Color(0.55f, 0.52f, 0.48f, 0.55f);
            if (_e.Kind == CodexKind.Enemy)
            {
                UiKit.Icon(card, InkSprites.Person((EnemyId)_e.Index), Vector2.zero, 180f).color = tint;
                return;
            }
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

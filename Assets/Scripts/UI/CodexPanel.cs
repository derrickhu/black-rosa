using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴三栏：字谱、秘卷、墨谱。每栏头顶一条收集度，攒到印上就能领墨。
    // 一格三种样子：收录 = 彩色；见过未收录 = 灰影 + 锁；没见过 = 问号。
    // 壳在 CodexLayout（预制 Prefabs/Codex），这里只填数据。
    public sealed class CodexPanel : MonoBehaviour
    {
        // 九宫格的边按「原图高 / 目标高」缩，整张图按比例缩到目标高度再只横向拉中段。
        // 不缩的话 640 高的卡框画进 240 的格子，边厚还是 78，卡面就只剩一条缝。
        public static RectTransform Slab(Transform parent, string name, string key, Vector2 pos, Vector2 size,
            Pin pin = Pin.Top, float refH = 0f)
        {
            var rt = UiKit.Art(parent, name, key, pos, size, pin);
            var img = rt.GetComponent<Image>();
            img.raycastTarget = false;
            if (img.sprite != null && img.type == Image.Type.Sliced)
                img.pixelsPerUnitMultiplier = img.sprite.rect.height / Mathf.Max(1f, refH > 0f ? refH : size.y);
            return rt;
        }

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        ScrollRect _list;
        RectTransform _cell;
        CodexTab _tab;
        bool[] _seenCards;
        bool[] _seenEnemies;
        float _scroll = -1f;
        UiAnim _anim;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = CodexLayout.Main(layer);
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
            panel._board = (RectTransform)dim.Find("board");
            panel._board.anchoredPosition = new Vector2(0f, -top);
            panel._board.sizeDelta = new Vector2(CodexLayout.BoardW, h);
            panel.Wire();
            panel.Refresh();
            PanelKit.Open(panel._board);
        }

        void Wire()
        {
            _board.Find("close").GetComponent<Button>().onClick.AddListener(Close);
            for (int i = 0; i < CodexLayout.Tabs.Length; i++)
            {
                CodexTab tab = CodexLayout.Tabs[i];
                _board.Find("tab" + i).GetComponent<Button>().onClick.AddListener(() => Switch(tab));
            }
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _board.Find("mile" + i).GetComponent<Button>().onClick.AddListener(() => Claim(idx));
            }
            _list = _board.Find("list").GetComponent<ScrollRect>();
            _cell = (RectTransform)_board.Find("cell");
            _list.onValueChanged.AddListener(v => _scroll = v.y);
        }

        void Close()
        {
            AudioBus.Tap();
            _changed?.Invoke();
            Destroy(gameObject);
        }

        void Refresh()
        {
            float keep = _scroll;
            PaintTabs();
            PaintProgress();
            _board.Find("hint").GetComponent<Text>().text = CodexCatalog.TabHint(_tab);
            FillGrid(CodexCatalog.Of(_tab), keep);
        }

        void Switch(CodexTab tab)
        {
            if (_tab == tab) return;
            AudioBus.Tap();
            _tab = tab;
            _scroll = -1f;
            Refresh();
        }

        void PaintTabs()
        {
            for (int i = 0; i < CodexLayout.Tabs.Length; i++)
            {
                CodexTab tab = CodexLayout.Tabs[i];
                bool on = _tab == tab;
                Transform plate = _board.Find("tab" + i);
                plate.GetComponent<Image>().sprite = InkSprites.Load(on ? "Ui/codex_tab_on" : "Ui/codex_tab_off");
                plate.Find("t").GetComponent<Text>().color = on ? InkTheme.CardFace : InkTheme.TextDark;
                plate.Find("dot").gameObject.SetActive(_meta.CodexTabNew(tab));
            }
        }

        void PaintProgress()
        {
            IReadOnlyList<CodexEntry> list = CodexCatalog.Of(_tab);
            int known = CodexCatalog.CountKnown(_meta, list);
            CodexMile[] miles = CodexCatalog.Miles(_tab);
            float ratio = list.Count > 0 ? (float)known / list.Count : 0f;

            Transform track = _board.Find("track");
            const float trackW = CodexLayout.TrackW;
            const float trackH = CodexLayout.TrackH;
            // 凹槽在原图里占高的 44%、略偏上，两头各缩进 9%。
            float grooveH = trackH * 0.44f;
            float inset = trackH * 0.25f;
            var fill = (RectTransform)track.Find("fill");
            fill.gameObject.SetActive(ratio > 0.01f);
            if (ratio > 0.01f)
            {
                float inner = trackW - inset * 2f;
                float fillH = grooveH - 2f;
                float fillW = Mathf.Clamp(inner * ratio, fillH * 1.2f, inner);
                fill.anchoredPosition = new Vector2(-inner * 0.5f + fillW * 0.5f, trackH * 0.04f);
                fill.sizeDelta = new Vector2(fillW, fillH);
            }
            track.Find("count").GetComponent<Text>().text = "已收录 " + known + "/" + list.Count;

            for (int i = 0; i < miles.Length && i < 3; i++)
            {
                bool reached = known >= miles[i].Need;
                bool claimed = _meta.MileClaimed(_tab, i);
                bool ready = reached && !claimed;
                Transform host = _board.Find("mile" + i);
                host.GetComponent<Image>().sprite = InkSprites.Load(
                    claimed ? "Ui/codex_mile_done" : ready ? "Ui/codex_mile_ready" : "Ui/codex_mile_off");
                var condLab = host.Find("need").GetComponent<Text>();
                var inkLab = host.Find("ink").GetComponent<Text>();
                condLab.text = claimed ? "已领" : "收录 " + miles[i].Need + " 个";
                inkLab.text = "墨 +" + miles[i].Ink;
                condLab.color = ready ? InkTheme.TextDark : InkTheme.TextMid;
                inkLab.color = claimed ? InkTheme.TextDim : ready ? InkTheme.Gold : InkTheme.TextDark;
                var pulse = UiAnim.On(host);
                pulse.Clear();
                host.localScale = Vector3.one;
                if (ready) pulse.Breathe(host, 0f, 0.045f, 1.15f);
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
            AudioBus.UnlockSting();
            Transform ticket = _board.Find("mile" + i);
            Vector2 from = RewardFly.Local(_layer, ticket);
            RewardFly.Play(_layer, from, new[] { RewardFly.Ink(_layer, ink) });
            Burst(new Vector2((i - 1) * CodexLayout.MileStep, CodexLayout.MileY + CodexLayout.MileH * 0.5f), ink);
            Refresh();
        }

        // 印落在奖励票上，墨点溅开，数字往上飘。挂在板子上，刷新格子不会把它清掉。
        void Burst(Vector2 pos, int ink)
        {
            var stamp = UiKit.Art(_board, "stamp", "Ui/codex_stamp", pos, new Vector2(112f, 112f), Pin.Top);
            var stampImg = stamp.GetComponent<Image>();
            stampImg.raycastTarget = false;
            stamp.SetAsLastSibling();
            stamp.localRotation = Quaternion.Euler(0f, 0f, -18f);
            _anim.Pop(stamp, 0f, 0.28f, 1.7f);
            _anim.Tween(0f, 0.28f, k =>
            {
                if (stamp != null) stamp.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-18f, -6f, k));
            });
            _anim.Fade(stampImg, 0.55f, 0.3f, 1f, 0f);
            _anim.At(0.9f, () => { if (stamp != null) Destroy(stamp.gameObject); });

            for (int n = 0; n < 6; n++)
            {
                float ang = (n * 60f + 15f) * Mathf.Deg2Rad;
                string key = n % 2 == 0 ? "Ui/codex_splash" : "Ui/codex_spark";
                var bit = UiKit.Art(_board, "sp" + n, key, pos, new Vector2(46f, 46f), Pin.Top);
                bit.GetComponent<Image>().raycastTarget = false;
                Vector2 home = bit.anchoredPosition;
                Vector2 to = home + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (42f + n * 8f);
                _anim.Move(bit, home, to, 0.02f, 0.36f);
                _anim.Fade(bit.GetComponent<Image>(), 0.14f, 0.3f, 1f, 0f);
                var dead = bit;
                _anim.At(0.5f, () => { if (dead != null) Destroy(dead.gameObject); });
            }

            var lab = UiKit.Label(_board, "gain", "墨 +" + ink, 34, pos, new Vector2(180f, 44f),
                TextAnchor.MiddleCenter, Pin.Top);
            lab.color = InkTheme.Gold;
            UiKit.Bold(lab);
            Vector2 from = lab.rectTransform.anchoredPosition;
            _anim.Move(lab.rectTransform, from, from + new Vector2(0f, 70f), 0.06f, 0.48f);
            _anim.Fade(lab, 0.34f, 0.28f, 1f, 0f);
            _anim.At(0.68f, () => { if (lab != null) Destroy(lab.gameObject); });
        }

        void FillGrid(IReadOnlyList<CodexEntry> list, float keep)
        {
            RectTransform content = _list.content;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject old = content.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            int rows = (list.Count + CodexLayout.Cols - 1) / CodexLayout.Cols;
            content.sizeDelta = new Vector2(0f, rows * (CodexLayout.CellH + CodexLayout.Gap) + 16f);
            for (int i = 0; i < list.Count; i++) Cell(content, list[i], i);
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = keep >= 0f ? keep : 1f;
        }

        void Cell(RectTransform content, CodexEntry e, int i)
        {
            const float cellW = CodexLayout.CellW;
            const float cellH = CodexLayout.CellH;
            const int cols = CodexLayout.Cols;
            float x = (i % cols - (cols - 1) * 0.5f) * (cellW + CodexLayout.Gap);
            float y = 8f + (i / cols) * (cellH + CodexLayout.Gap);
            bool[] seen = e.Kind == CodexKind.Enemy ? _seenEnemies : _seenCards;
            CodexState st = CodexCatalog.StateOf(_meta, e, seen);
            bool known = st == CodexState.Known;

            var card = Instantiate(_cell, content, false);
            card.name = "c" + i;
            card.anchoredPosition = new Vector2(x, -y);
            card.gameObject.SetActive(true);
            Sprite frame = InkSprites.Load(known ? "Ui/codex_face" : st == CodexState.Unknown ? "Ui/codex_back" : "Ui/codex_face_dim");
            if (frame != null) card.GetComponent<Image>().sprite = frame;
            card.GetComponent<Button>().onClick.AddListener(() => Open(e, st));

            var name = card.Find("n").GetComponent<Text>();
            if (st == CodexState.Unknown)
            {
                bool hinted = e.Kind == CodexKind.Pair && CodexCatalog.PairHinted(_meta, e.Index);
                name.text = hinted ? "似曾相识" : "？？";
                name.color = hinted ? InkTheme.Gold : InkTheme.TextDim;
                return;
            }

            // 见过没收录：字图半透明叠在同色卡面上，看得出是哪个字但发虚。
            Color tint = known ? Color.white : new Color(1f, 1f, 1f, 0.42f);
            if (e.Kind == CodexKind.Glyph)
                Put(card, "art", InkArt.Glyph((CardId)e.Index, 160), 144f, tint);
            else if (e.Kind == CodexKind.Word)
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int k = 0; k < parts.Length && k < 2; k++)
                    Put(card, k == 0 ? "ga" : "gb", InkArt.Glyph(parts[k], 160), 74f, tint);
            }
            else if (e.Kind == CodexKind.Enemy)
                Put(card, "art", InkSprites.Person((EnemyId)e.Index), 136f,
                    known ? Color.white : new Color(0.3f, 0.26f, 0.22f, 0.6f));
            else
                Put(card, "art", PairFrame(e.Index), 124f, tint);

            name.text = CodexCatalog.Title(e);
            name.color = known ? InkTheme.TextDark : InkTheme.TextDim;
            card.Find("lock").gameObject.SetActive(!known);
            card.Find("dot").gameObject.SetActive(known && _meta.CodexIsNew(e.Kind, e.Index));
        }

        public static void Put(Transform host, string path, Sprite sprite, float size, Color tint)
        {
            Transform t = host.Find(path);
            if (t == null || sprite == null) return;
            var img = t.GetComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            t.gameObject.SetActive(true);
        }

        void Open(CodexEntry e, CodexState st)
        {
            AudioBus.Tap();
            if (st == CodexState.Unknown)
            {
                string tip = e.Kind == CodexKind.Pair ? "两字同弹，方见真形" : "还没在关卡里遇见过";
                InkToast.Show(_layer, tip);
                return;
            }
            if (st == CodexState.Known) _meta.CodexSeen(e.Kind, e.Index);
            CodexDetail.Show(_layer, e, st == CodexState.Known, Refresh);
        }

        public static Sprite PairFrame(int pair)
        {
            if (!InkVfx.Flat(SignaturePairs.All[pair].Form, out InkVfx.FlatBody body)) return null;
            Sprite[] frames = InkVfx.Frames(body.Frames, body.Count);
            return frames != null && frames.Length > 0 ? frames[0] : null;
        }
    }

    // 点开一格：左边字、右边炮弹，下面是说明和三档数值。点星级那一行，炮弹跟着换。
    // 壳在 CodexLayout（预制 Prefabs/CodexDetail），按种类开关 stars / pair / enemy 三组。
    public sealed class CodexDetail : MonoBehaviour
    {
        CodexEntry _e;
        bool _known;
        Action _closed;
        RectTransform _board;
        CodexShot _shot;
        Image _glyph;
        readonly List<Image> _rows = new List<Image>();

        public static void Show(RectTransform layer, CodexEntry e, bool known, Action closed)
        {
            var dim = CodexLayout.Detail(layer);
            var d = dim.gameObject.AddComponent<CodexDetail>();
            d._e = e;
            d._known = known;
            d._closed = closed;
            d._board = (RectTransform)dim.Find("board");
            CodexLayout.SetTitle(d._board, CodexCatalog.Title(e));
            d._board.Find("close").GetComponent<Button>().onClick.AddListener(d.Close);
            d.Build();
        }

        void Close()
        {
            AudioBus.Tap();
            _closed?.Invoke();
            Destroy(gameObject);
        }

        Text Lab(string path) => _board.Find(path).GetComponent<Text>();

        void Build()
        {
            bool enemy = _e.Kind == CodexKind.Enemy;
            bool pair = _e.Kind == CodexKind.Pair;
            _board.Find("shotBox").gameObject.SetActive(!enemy);
            _board.Find("note").gameObject.SetActive(!enemy);
            _board.Find("enemy").gameObject.SetActive(enemy);
            _board.Find("pair").gameObject.SetActive(pair);
            _board.Find("stars").gameObject.SetActive(!enemy && !pair);
            BuildFace();
            if (enemy)
            {
                BuildEnemy();
                return;
            }
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
            Lab("note").text = note;
            var para = Lab("lore");
            para.text = lore;
            para.color = _known ? InkTheme.TextDark : InkTheme.TextDim;

            if (pair) BuildPairEffect();
            else BuildStars();
        }

        void BuildEnemy()
        {
            var id = (EnemyId)_e.Index;
            EnemyDef d = EnemyCatalog.Base(id);
            Transform box = _board.Find("enemy/stat");
            box.Find("q").gameObject.SetActive(!_known);
            string[] rows =
            {
                "血  " + Mathf.RoundToInt(d.Hp),
                "速  " + d.Speed.ToString("0.00"),
                "金  " + d.Gold,
                "墨  " + (d.Ink < 0.05f ? "少许" : d.Ink.ToString(d.Ink >= 1f ? "0.#" : "0.00"))
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var t = box.Find("s" + i).GetComponent<Text>();
                t.gameObject.SetActive(_known);
                t.text = rows[i];
                t.color = InkTheme.TextDark;
            }

            var para = Lab("lore");
            para.text = _known ? EnemyCatalog.Lore(id) : "打倒它一次，才写得进墨谱。";
            para.color = _known ? InkTheme.TextDark : InkTheme.TextDim;
            para.rectTransform.anchoredPosition = new Vector2(0f, -390f);
            para.rectTransform.sizeDelta = new Vector2(CodexLayout.RowW, 110f);

            var tv = Lab("enemy/trait/v");
            tv.text = _known ? EnemyCatalog.TraitLine(d) : "？？？";
            tv.color = _known ? InkTheme.TextDark : InkTheme.TextDim;
        }

        void BuildFace()
        {
            var card = _board.Find("face");
            Sprite frame = InkSprites.Load(_known ? "Ui/codex_face" : "Ui/codex_face_dim");
            if (frame != null) card.GetComponent<Image>().sprite = frame;
            var name = card.Find("n").GetComponent<Text>();
            name.text = CodexCatalog.Title(_e);
            name.color = _known ? InkTheme.TextDark : InkTheme.TextDim;
            Color tint = _known ? Color.white : new Color(1f, 1f, 1f, 0.42f);
            if (_e.Kind == CodexKind.Enemy)
            {
                CodexPanel.Put(card, "person", InkSprites.Person((EnemyId)_e.Index), 140f,
                    _known ? Color.white : new Color(0.3f, 0.26f, 0.22f, 0.6f));
                return;
            }
            if (_e.Kind == CodexKind.Glyph)
            {
                CodexPanel.Put(card, "glyph", InkArt.Glyph((CardId)_e.Index, 160), 150f, tint);
                _glyph = card.Find("glyph").GetComponent<Image>();
                return;
            }
            CardId[] parts;
            if (_e.Kind == CodexKind.Word) parts = CodexCatalog.PartsOf((WordId)_e.Index);
            else
            {
                SignaturePair p = SignaturePairs.All[_e.Index];
                parts = new[] { p.A, p.B };
            }
            CodexPanel.Put(card, "ga", InkArt.Glyph(parts[0], 160), 84f, tint);
            CodexPanel.Put(card, "gb", InkArt.Glyph(parts[1], 160), 84f, tint);
            card.Find("plus").gameObject.SetActive(_e.Kind == CodexKind.Pair);
        }

        void BuildShotBox()
        {
            var box = (RectTransform)_board.Find("shotBox");
            box.Find("q").gameObject.SetActive(!_known);
            if (!_known) return;
            _shot = CodexShot.Build(box, box.sizeDelta - new Vector2(56f, 56f));
            if (_e.Kind == CodexKind.Glyph) _shot.ShowGlyph((CardId)_e.Index, 1);
            else if (_e.Kind == CodexKind.Word) _shot.ShowWord((WordId)_e.Index);
            else _shot.ShowPair(_e.Index);
            box.Find("plain").gameObject.SetActive(_e.Kind == CodexKind.Glyph && !ChangesLook((CardId)_e.Index));
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
            bool pickable = _known && _e.Kind == CodexKind.Glyph;
            Transform g = _board.Find("stars");
            g.Find("tip").gameObject.SetActive(pickable);
            for (int s = 1; s <= GameConstants.MaxStar; s++)
            {
                int star = s;
                Transform row = g.Find("s" + s);
                if (row == null) continue;
                var img = row.GetComponent<Image>();
                _rows.Add(img);
                var t = row.Find("v").GetComponent<Text>();
                t.text = !_known ? "？？？"
                    : _e.Kind == CodexKind.Glyph ? CodexCatalog.StarLine((CardId)_e.Index, s)
                    : CodexCatalog.WordStarLine((WordId)_e.Index, s);
                t.color = _known ? InkTheme.TextDark : InkTheme.TextDim;
                img.raycastTarget = pickable;
                if (pickable) row.GetComponent<Button>().onClick.AddListener(() => PickStar(star));
            }
            if (pickable) Highlight(1);
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
            Sprite on = InkSprites.Load("Ui/codex_row_on");
            Sprite off = InkSprites.Load("Ui/codex_row");
            for (int i = 0; i < _rows.Count; i++)
            {
                Sprite s = i + 1 == star ? on : off;
                if (s != null) _rows[i].sprite = s;
            }
        }

        void BuildPairEffect()
        {
            Lab("pair/effect/v").text = SignaturePairs.All[_e.Index].Note;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 点开一张字卡：字浮在放射光中间，外圈一圈金色光点，三颗插槽落在上、左、右；
    // 下面是本事、伤害加成从几到几、十格等级、升级和看广告试用。
    // 点槽从底部弹出镶嵌面板。左右箭头在当前分类里已有的字之间切换，不用退回列表。
    public sealed class GlyphDetailPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 1060f;
        const float OrbY = 150f;
        static readonly Vector2 OrbSize = new Vector2(620f, 440f);
        const float GlyphBox = 280f;
        // 三颗槽都落在同一圈星尘上，圈心就是字的中心。
        const float Ring = 172f;
        const float SlotSize = 84f;
        const float StudSize = 60f;
        const float SheetY = 540f;
        static readonly Vector2 SheetSize = new Vector2(604f, 500f);

        // 三颗槽：上、左、右。
        static readonly Vector2[] SlotAt =
        {
            new Vector2(0f, Ring),
            new Vector2(-Ring, 0f),
            new Vector2(Ring, 0f),
        };

        MetaProgress _meta;
        RectTransform _layer;
        Action _closed;
        RectTransform _board;
        RectTransform _body;
        RectTransform _orb;
        List<CodexEntry> _list;
        int _at;
        int _sel = -1;
        UiAnim _anim;
        float _busyUntil;

        public static void Show(RectTransform layer, MetaProgress meta, List<CodexEntry> list, int at, Action closed)
        {
            if (list == null || list.Count == 0) return;
            meta.EnsureSlots();
            var dim = UiKit.Dimmer(layer);
            dim.name = "study_detail";
            var p = dim.gameObject.AddComponent<GlyphDetailPanel>();
            p._meta = meta;
            p._layer = layer;
            p._closed = closed;
            p._list = list;
            p._at = Mathf.Clamp(at, 0, list.Count - 1);
            p._anim = UiAnim.On(p);
            p._board = PanelKit.Board(dim, CodexCatalog.Title(list[p._at]), Vector2.zero, new Vector2(BoardW, BoardH),
                Pin.Center, p.Close);
            p.Paint();
        }

        CodexEntry Cur => _list[_at];

        void Close()
        {
            AudioBus.Tap();
            _closed?.Invoke();
            Destroy(gameObject);
        }

        void Paint()
        {
            if (_body != null)
            {
                _body.gameObject.SetActive(false);
                Destroy(_body.gameObject);
            }
            _body = UiKit.Panel(_board, "body", Vector2.zero, Vector2.zero, Color.clear);
            _body.anchorMin = Vector2.zero;
            _body.anchorMax = Vector2.one;
            _body.offsetMin = Vector2.zero;
            _body.offsetMax = Vector2.zero;
            _body.GetComponent<Image>().raycastTarget = false;
            Transform ribbon = _board.Find("ribbon");
            Text title = ribbon != null ? ribbon.GetComponentInChildren<Text>() : null;
            if (title != null) title.text = CodexCatalog.Title(Cur);
            // 关闭键压在横幅和正文上面。
            Transform close = _board.Find("close");
            if (close != null) close.SetAsLastSibling();

            CodexEntry e = Cur;
            int lv = _meta.StudyLevel(e);
            BuildBadge(e, lv);
            BuildOrbit(e, lv);
            BuildBonus(lv);
            BuildActions(e, lv);
            if (_sel >= 0) BuildSheet(e, _sel);
        }

        void BuildBadge(CodexEntry e, int lv)
        {
            // 等级放到下面和等级点一起；顶上左边是试用角标，右边是刷新。
            if (_meta.HasTrial(e))
            {
                var tag = UiKit.Stroke(_body, "trial_tag", new Vector2(-246f, 105f), new Vector2(76f, 34f), Pin.Top, 3f,
                    null, InkTheme.Teal, 17f);
                var tt = UiKit.Label(tag, "t", "试用", 20, Vector2.zero, new Vector2(76f, 34f));
                tt.color = InkTheme.CardFace;
                UiKit.Bold(tt);
            }
            if (_meta.SealOpen && _meta.SlotsOpen(e) > 0) BuildReroll();
        }

        // 右上角一枚刷新图标，底下标着钻石价。点下去先问一声，确认了才刷。
        void BuildReroll()
        {
            var hold = UiKit.Panel(_body, "rr", new Vector2(236f, 62f), new Vector2(92f, 118f), Color.clear, Pin.Top);
            var hit = hold.GetComponent<Image>();
            hit.raycastTarget = true;
            UiKit.Icon(hold, InkSprites.Ui("reroll"), new Vector2(0f, 20f), 76f);
            UiKit.Icon(hold, InkSprites.Ui("diamond"), new Vector2(-16f, -38f), 22f);
            var n = UiKit.Label(hold, "n", SealCatalog.RerollDiamond.ToString(), 22, new Vector2(12f, -38f), new Vector2(40f, 28f));
            n.color = InkTheme.TextDark;
            UiKit.Bold(n);
            var btn = hold.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(AskReroll);
        }

        // ---- 星尘轨道 ----

        void BuildOrbit(CodexEntry e, int lv)
        {
            _orb = UiKit.Panel(_body, "orb", new Vector2(0f, OrbY), OrbSize, Color.clear, Pin.Top);
            _orb.GetComponent<Image>().raycastTarget = false;

            var rays = UiKit.Icon(_orb, InkSprites.Load("Ui/seal_burst"), Vector2.zero, 500f);
            rays.color = Color.white;
            _anim.Spin(rays.transform, lv >= StudyCatalog.MaxLevel ? 8f : 5f);

            BuildDust();

            // 字的托：升级演出按它的位置和大小放光。
            var c = UiKit.Panel(_orb, "c", Vector2.zero, new Vector2(GlyphBox, GlyphBox), Color.clear);
            c.GetComponent<Image>().raycastTarget = false;
            if (e.Kind == CodexKind.Glyph)
                UiKit.Icon(c, Bare(InkArt.Glyph((CardId)e.Index, 256)), Vector2.zero, GlyphBox);
            else
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int i = 0; i < parts.Length && i < 2; i++)
                    UiKit.Icon(c, Bare(InkArt.Glyph(parts[i], 256)), new Vector2(i == 0 ? -72f : 72f, 0f), 160f);
            }
            _anim.Breathe(c, 0f, 0.02f, 0.4f);

            if (_list.Count > 1)
            {
                Arrow("prev", "<", new Vector2(-292f, 0f), -1);
                Arrow("next", ">", new Vector2(292f, 0f), 1);
            }

            bool open = _meta.SealOpen;
            int slots = _meta.SlotsOpen(e);
            for (int k = 0; k < SealCatalog.SlotsPer; k++) BuildSlot(e, k, open, k < slots);
            if (!open)
            {
                var gate = UiKit.Label(_orb, "gate", "通关第五章开放插槽", 21, new Vector2(0f, -Ring + 4f), new Vector2(360f, 30f));
                gate.color = InkTheme.TextMid;
            }
        }

        // 槽名和数值贴在槽外：上面那颗在右侧，左右两颗在下方。光点要给这几块让位。
        static Rect CaptionRect(int k)
        {
            float r = SlotSize * 0.5f;
            if (k == 0) return new Rect(r + 8f, Ring - 30f, 184f, 60f);
            return new Rect(SlotAt[k].x - 84f, -(r + 62f), 168f, 60f);
        }

        // 一圈金色光点，下沿留空给字透气。大小、深浅错开，几颗慢慢一闪一闪。
        void BuildDust()
        {
            var dust = InkSprites.Load("Ui/seal_dust");
            const int n = 48;
            for (int i = 0; i < n; i++)
            {
                float deg = -58f + i * (296f / (n - 1));
                float rad = deg * Mathf.Deg2Rad;
                float r = Ring + ((i * 7) % 5 - 2) * 3f;
                Vector2 p = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
                if (Blocked(p)) continue;
                float size = i % 3 == 0 ? 18f : i % 3 == 1 ? 13f : 8f;
                var dot = UiKit.Icon(_orb, dust, p, size);
                dot.color = new Color(1f, 1f, 1f, i % 3 == 2 ? 0.55f : 0.9f);
                if (i % 4 == 0) _anim.Breathe(dot.transform, i * 0.13f, 0.35f, 0.6f + (i % 3) * 0.2f);
            }
        }

        static bool Blocked(Vector2 p)
        {
            for (int k = 0; k < SlotAt.Length; k++)
            {
                if ((p - SlotAt[k]).magnitude < SlotSize * 0.5f + 14f) return true;
                Rect c = CaptionRect(k);
                c.xMin -= 6f;
                c.xMax += 6f;
                c.yMin -= 6f;
                c.yMax += 6f;
                if (c.Contains(p)) return true;
            }
            return false;
        }

        void Arrow(string name, string text, Vector2 pos, int d)
        {
            var b = UiKit.Btn(_orb, name, text, pos, new Vector2(56f, 84f), () => Step(d), false);
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.fontSize = 32;
        }

        void BuildSlot(CodexEntry e, int k, bool open, bool unlocked)
        {
            Vector2 at = SlotAt[k];
            if (!open || !unlocked)
            {
                var stud = SealArt.Stud(_orb, "slot" + k, at, StudSize);
                Hit(stud, 80f, () => TapLocked(k));
                if (open) SlotCaption(k, "Lv." + SealCatalog.SlotLevelOf(k) + " 解锁", "", InkTheme.TextMid);
                return;
            }
            SealType t = _meta.SlotTypeOf(e, k);
            int key = SealCatalog.KeyOf(e);
            bool filled = _meta.SlotSealOf(e, k, out SealQuality q);
            if (_sel == k)
            {
                var halo = UiKit.Icon(_orb, InkSprites.Load("Ui/seal_burst"), at, SlotSize * 2.4f);
                halo.color = Color.white;
                _anim.Spin(halo.transform, 24f);
            }
            RectTransform node = filled
                ? SealArt.Medal(_orb, "slot" + k, t, q, at, SlotSize)
                : SealArt.Empty(_orb, "slot" + k, t, at, SlotSize);
            if (_sel == k || (filled && q == SealQuality.Gold)) _anim.Breathe(node, 0.4f, 0.05f, 0.85f);
            else if (!filled && _meta.BagCount(t) > 0) _anim.Breathe(node, 0.4f, 0.06f, 0.85f);
            Hit(node, 96f, () => Select(k));

            // 第二行先说提高什么，再用品质色写这枚加多少；空槽写「可镶嵌」或「空」。
            SealCatalog.Line(key, t, filled ? q : SealQuality.Green, out string what, out string amount);
            string tail = filled ? Tint(amount, SealCatalog.QualityDeep(q))
                : _meta.BagCount(t) > 0 ? Tint("可镶嵌", InkTheme.Seal) : Tint("空", InkTheme.TextDim);
            SlotCaption(k, SealCatalog.TypeName(t), what + " " + tail, InkTheme.TextMid);
        }

        static string Tint(string s, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

        void SlotCaption(int k, string line1, string line2, Color valueColor)
        {
            Rect r = CaptionRect(k);
            bool top = k == 0;
            TextAnchor anchor = top ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            Vector2 mid = r.center;
            if (k != 0 && line2 == "") mid.y = r.yMax - 14f;
            var l1 = UiKit.Label(_orb, "slot" + k + "_t", line1, line2 == "" ? 19 : 23,
                mid + new Vector2(0f, line2 == "" ? 0f : 14f), new Vector2(r.width, 30f), anchor);
            if (line2 == "")
            {
                l1.color = valueColor;
                return;
            }
            l1.color = InkTheme.TextDark;
            UiKit.Bold(l1);
            var l2 = UiKit.Label(_orb, "slot" + k + "_v", line2, 19, mid + new Vector2(0f, -15f), new Vector2(r.width, 26f), anchor);
            l2.color = valueColor;
            UiKit.Bold(l2);
        }

        // 字图的底是一块近白的纸。详情里抠掉，字才浮在放射光上。
        static readonly Dictionary<int, Sprite> BareCache = new Dictionary<int, Sprite>();

        static Sprite Bare(Sprite art)
        {
            if (art == null || art.texture == null) return art;
            int id = art.GetInstanceID();
            if (BareCache.TryGetValue(id, out Sprite cached) && cached != null) return cached;
            Texture2D src = art.texture;
            Rect rect = art.textureRect;
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width);
            int h = Mathf.RoundToInt(rect.height);
            Color[] px;
            try { px = src.GetPixels(x, y, w, h); }
            catch (UnityException) { return art; }
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float sat = max - min;
                if (min > 0.90f && sat < 0.08f) c.a = 0f;
                else if (min > 0.78f && sat < 0.07f) c.a *= Mathf.Clamp01((0.90f - min) / 0.12f);
                px[i] = c;
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply();
            cached = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), art.pixelsPerUnit);
            BareCache[id] = cached;
            return cached;
        }

        static void Hit(RectTransform node, float size, Action click)
        {
            var img = node.GetComponent<Image>();
            if (img == null)
            {
                img = node.gameObject.AddComponent<Image>();
                img.color = Color.clear;
            }
            img.raycastTarget = true;
            if (size > 0f) node.sizeDelta = new Vector2(size, size);
            var btn = node.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => click());
        }

        void TapLocked(int k)
        {
            AudioBus.Deny();
            InkToast.Show(_layer, _meta.SealOpen ? "升到 " + SealCatalog.SlotLevelOf(k) + " 级解锁这个插槽" : "通关第五章开放插槽");
        }

        void Select(int k)
        {
            if (Time.unscaledTime < _busyUntil) return;
            AudioBus.Tap();
            _sel = _sel == k ? -1 : k;
            _anim.Clear();
            Paint();
            if (_sel >= 0 && _body.Find("sheet") is RectTransform sheet)
                _anim.Move(sheet, sheet.anchoredPosition + new Vector2(0f, -120f), sheet.anchoredPosition, 0f, 0.24f, Ease.OutBack);
        }

        // ---- 底部镶嵌面板 ----

        void BuildSheet(CodexEntry e, int k)
        {
            if (k >= _meta.SlotsOpen(e)) return;
            SealType t = _meta.SlotTypeOf(e, k);
            int key = SealCatalog.KeyOf(e);
            bool filled = _meta.SlotSealOf(e, k, out SealQuality cur);
            float w = SheetSize.x, h = SheetSize.y;

            // 面板后面垫一层吃点击的底，点空白处收起。
            var catcher = UiKit.Panel(_body, "sheet_catch", new Vector2(0f, SheetY - 20f), new Vector2(BoardW, BoardH - SheetY + 20f),
                Color.clear, Pin.Top);
            Hit(catcher, 0f, () => Select(_sel));
            var sheet = UiKit.Stroke(_body, "sheet", new Vector2(0f, SheetY), SheetSize, Pin.Top, 5f, null, InkTheme.Hex("FFF8EA"), 28f);

            // 头部：这一槽现在的样子 + 类型名 + 效果。
            Vector2 mp = new Vector2(-w * 0.5f + 78f, h * 0.5f - 76f);
            var plate = UiKit.Icon(sheet, InkSprites.Load("Ui/seal_burst"), mp, 172f);
            plate.color = Color.white;
            _anim.Spin(plate.transform, 10f);
            if (filled) SealArt.Medal(sheet, "cur", t, cur, mp, 108f);
            else SealArt.Empty(sheet, "cur", t, mp, 108f);
            const float textL = 150f;
            float textW = w - textL - 150f;
            var head = UiKit.Label(sheet, "h", SealCatalog.TypeName(t), 32,
                new Vector2(-w * 0.5f + textL + textW * 0.5f, h * 0.5f - 46f), new Vector2(textW, 42f), TextAnchor.MiddleLeft);
            head.color = InkTheme.TextDark;
            UiKit.Bold(head);
            SealCatalog.Line(key, t, filled ? cur : SealQuality.Green, out string what, out string amount);
            string now = filled
                ? SealCatalog.QualityName(cur) + "印章 · " + what + " " + Tint(amount, SealCatalog.QualityDeep(cur))
                : "空槽 · 镶上后" + what + "提高";
            var sub = UiKit.Label(sheet, "k", now, 21,
                new Vector2(-w * 0.5f + textL + textW * 0.5f, h * 0.5f - 80f), new Vector2(textW, 28f), TextAnchor.MiddleLeft);
            sub.color = InkTheme.TextDark;
            UiKit.Bold(sub);
            var eff = UiKit.Label(sheet, "eff", SealCatalog.EffectOf(key, t) + "。品质越高，加得越多", 18,
                new Vector2(-w * 0.5f + textL + (w - textL - 30f) * 0.5f, h * 0.5f - 122f), new Vector2(w - textL - 30f, 52f),
                TextAnchor.UpperLeft);
            eff.horizontalOverflow = HorizontalWrapMode.Wrap;
            eff.verticalOverflow = VerticalWrapMode.Truncate;
            eff.color = InkTheme.TextMid;
            var x = UiKit.Btn(sheet, "x", "收起", new Vector2(w * 0.5f - 72f, h * 0.5f - 48f), new Vector2(108f, 54f),
                () => Select(_sel), false);
            var xt = x.GetComponentInChildren<Text>();
            if (xt != null) xt.fontSize = 22;

            // 四个品质一字排开：有货的可以点，点了就镶（已经镶着的换下来退回背包）。
            const float cellW = 134f, cellH = 162f, gap = 10f;
            float x0 = -(cellW + gap) * 1.5f;
            float cy = -6f;
            int have = _meta.BagCount(t);
            for (int qi = 0; qi < SealCatalog.QualityCount; qi++)
            {
                var q = (SealQuality)qi;
                int n = _meta.BagCount(t, q);
                bool on = filled && q == cur;
                bool any = on || n > 0;
                Vector2 pos = new Vector2(x0 + qi * (cellW + gap), cy);
                var cell = UiKit.Stroke(sheet, "q" + qi, pos, new Vector2(cellW, cellH), Pin.Center, on ? 5f : 3f,
                    on ? SealCatalog.QualityDeep(q) : any ? SealCatalog.QualityColor(q) : InkTheme.LineDim,
                    on ? InkTheme.Hex("FFEFC4") : any ? InkTheme.CardFace : InkTheme.Hex("F1E9DA"), 22f);
                var medal = SealArt.Medal(cell, "m", t, q, new Vector2(0f, 20f), 88f);
                if (!any)
                {
                    foreach (var img in medal.GetComponentsInChildren<Image>()) img.color *= new Color(1f, 1f, 1f, 0.32f);
                }
                SealCatalog.Line(key, t, q, out _, out string qAmount);
                var v = UiKit.Label(cell, "v", qAmount, 24, new Vector2(0f, -50f), new Vector2(cellW, 28f));
                v.color = any ? SealCatalog.QualityDeep(q) : InkTheme.TextDim;
                UiKit.Bold(v);
                if (on)
                {
                    var tag = UiKit.Stroke(cell, "on", new Vector2(0f, cellH * 0.5f), new Vector2(76f, 32f), Pin.Center, 3f,
                        null, InkTheme.Seal, 16f);
                    var tt = UiKit.Label(tag, "t", "镶着", 19, Vector2.zero, new Vector2(76f, 32f));
                    tt.color = InkTheme.CardFace;
                    UiKit.Bold(tt);
                }
                else
                {
                    Vector2 cp = new Vector2(cellW * 0.5f - 24f, cellH * 0.5f - 20f);
                    var chip = UiKit.Stroke(cell, "n", cp, new Vector2(48f, 30f), Pin.Center, 2.5f,
                        n > 0 ? null : InkTheme.LineDim, n > 0 ? InkTheme.TextDark : InkTheme.Hex("E6DCC8"), 15f);
                    var ct = UiKit.Label(chip, "t", "×" + n, 18, Vector2.zero, new Vector2(48f, 30f));
                    ct.color = n > 0 ? InkTheme.CardFace : InkTheme.TextDim;
                    UiKit.Bold(ct);
                }
                SealQuality pick = q;
                Hit(cell, 0f, () => Socket(pick));
            }
            if (have == 0 && !filled)
            {
                var none = UiKit.Label(sheet, "none", "还没有" + SealCatalog.TypeName(t) + "印章 · 第五章后残局通关、银金皇家箱会掉",
                    18, new Vector2(0f, cy - cellH * 0.5f - 18f), new Vector2(w - 40f, 26f));
                none.color = InkTheme.TextMid;
            }

            if (filled)
            {
                var off = UiKit.Btn(sheet, "off", "卸下", new Vector2(0f, -h * 0.5f + 56f), new Vector2(220f, 68f), Unsocket, false);
                var ot = off.GetComponentInChildren<Text>();
                if (ot != null) ot.fontSize = 24;
            }
        }

        void Socket(SealQuality q)
        {
            if (Time.unscaledTime < _busyUntil) return;
            CodexEntry e = Cur;
            int k = _sel;
            SealType t = _meta.SlotTypeOf(e, k);
            if (_meta.SlotSealOf(e, k, out SealQuality cur) && cur == q)
            {
                AudioBus.Tap();
                return;
            }
            if (_meta.BagCount(t, q) <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "没有" + SealCatalog.TypeName(t) + " · " + SealCatalog.QualityName(q) + "印章");
                return;
            }
            if (!_meta.Socket(e, k, q)) return;
            AudioBus.Stamp();
            _anim.Clear();
            Paint();
            _closed?.Invoke();
            if (_orb.Find("slot" + k) is RectTransform node)
            {
                _anim.Pop(node, 0f, 0.36f, 1.8f);
                Vector2 at = SlotAt[k];
                _anim.At(0.12f, () =>
                {
                    if (_orb == null) return;
                    UiConfetti.Sparks(_orb, at, SealCatalog.QualityColor(q), q == SealQuality.Gold ? 26 : 12, 460f);
                    if (q == SealQuality.Gold)
                    {
                        UiConfetti.Sparks(_orb, at, FoilArt.Face, 18, 620f);
                        AudioBus.StudyTier();
                    }
                });
            }
            if (q == SealQuality.Gold && _board.parent is RectTransform dim) UiConfetti.Burst(dim, 60);
        }

        void Unsocket()
        {
            if (!_meta.Unsocket(Cur, _sel)) return;
            AudioBus.Tap();
            InkToast.Show(_layer, "印章退回背包");
            _anim.Clear();
            Paint();
            _closed?.Invoke();
        }

        void AskReroll()
        {
            if (Time.unscaledTime < _busyUntil) return;
            CodexEntry e = Cur;
            MetaProgress.SealBlock block = _meta.RerollBlock(e);
            if (block != MetaProgress.SealBlock.None)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, block == MetaProgress.SealBlock.Diamond
                    ? "钻石不够，刷新要 " + SealCatalog.RerollDiamond : "还没有可以刷新的插槽");
                return;
            }
            AudioBus.Tap();
            ConfirmPanel.Ask(_layer,
                "把当前已开放的插槽全部重刷？\n镶着的印章会先卸下，退回背包\n花费 " + SealCatalog.RerollDiamond + " 钻石",
                "取消", "刷新", () =>
                {
                    if (this != null) DoReroll(e);
                });
        }

        void DoReroll(CodexEntry e)
        {
            int open = _meta.SlotsOpen(e);
            bool had = false;
            for (int k = 0; k < open; k++)
                if (_meta.SlotSealOf(e, k, out _)) had = true;
            if (_meta.Reroll(e) <= 0) return;
            _busyUntil = Time.unscaledTime + 0.4f;
            AudioBus.CardFlip(1.15f);
            _anim.Clear();
            Paint();
            _closed?.Invoke();
            string names = "";
            for (int k = 0; k < open; k++)
            {
                SealType t = _meta.SlotTypeOf(e, k);
                if (t == SealType.None) continue;
                if (names.Length > 0) names += "、";
                names += SealCatalog.TypeName(t);
                if (_orb.Find("slot" + k) is RectTransform node) SpinSlot(node, SlotAt[k]);
            }
            InkToast.Show(_layer, "刷出了「" + names + "」" + (had ? "，原来的印章退回背包" : ""));
        }

        void SpinSlot(RectTransform node, Vector2 at)
        {
            _anim.Tween(0f, 0.4f, u =>
            {
                if (node == null) return;
                node.localRotation = Quaternion.Euler(0f, 0f, (1f - Ease.OutCubic(u)) * 360f);
            }).Pop(node, 0f, 0.4f, 0.4f);
            _anim.At(0.3f, () =>
            {
                if (_orb != null) UiConfetti.Sparks(_orb, at, FoilArt.Face, 10, 380f);
            });
        }

        // ---- 加成、升级 ----

        void BuildBonus(int lv)
        {
            var box = UiKit.Panel(_body, "bonus", new Vector2(0f, 616f), new Vector2(580f, 118f), Color.clear, Pin.Top);
            box.GetComponent<Image>().raycastTarget = false;
            int now = Mathf.RoundToInt(StudyCatalog.BonusOf(lv) * 100f);
            // 上面是等级和圆点，下面才是这个等级给的整体伤害。
            const float rowY = -26f;
            bool max = lv >= StudyCatalog.MaxLevel;
            var what = UiKit.Label(box, "what", "字的整体伤害", 22, new Vector2(max ? -96f : -172f, rowY),
                new Vector2(160f, 40f), TextAnchor.MiddleRight);
            what.color = InkTheme.TextMid;
            UiKit.Bold(what);
            if (max)
            {
                var full = UiKit.Label(box, "v", "+" + now + "%", 40, new Vector2(60f, rowY), new Vector2(160f, 52f));
                full.color = InkTheme.Seal;
                UiKit.Bold(full);
            }
            else
            {
                int next = Mathf.RoundToInt(StudyCatalog.BonusOf(lv + 1) * 100f);
                var a = UiKit.Label(box, "a", "+" + now + "%", 40, new Vector2(-24f, rowY), new Vector2(130f, 52f));
                UiKit.Bold(a);
                var arrow = UiKit.Label(box, "arrow", "→", 34, new Vector2(62f, rowY), new Vector2(50f, 52f));
                arrow.color = InkTheme.TextMid;
                var b = UiKit.Label(box, "b", "+" + next + "%", 40, new Vector2(148f, rowY), new Vector2(130f, 52f));
                b.color = InkTheme.Seal;
                UiKit.Bold(b);
            }

            // 上面一行：等级药丸 + 十颗等级点。
            const float dotY = 28f;
            Vector2 pillSize = new Vector2(104f, 42f);
            var pill = UiKit.Stroke(box, "lv", new Vector2(-226f, dotY), pillSize, Pin.Center, 4f, null, StudyCard.Badge(lv), 21f);
            var pt = UiKit.Label(pill, "t", max ? "MAX" : "Lv." + lv, 26, Vector2.zero, pillSize);
            pt.color = InkTheme.CardFace;
            UiKit.Bold(pt);

            int cap = _meta.StudyCap;
            const float step = 36f;
            for (int i = 0; i < StudyCatalog.MaxLevel; i++)
            {
                Vector2 at = new Vector2(42f + (i - (StudyCatalog.MaxLevel - 1) * 0.5f) * step, dotY);
                var ring = UiKit.Icon(box, UiSprites.Disc(), at, 26f);
                ring.name = "dot" + i;
                ring.color = i < cap ? InkTheme.Outline : InkTheme.LineDim;
                var fill = UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 19f);
                fill.color = i < lv ? StudyCatalog.TierColor(StudyCatalog.TierOf(i + 1))
                    : i < cap ? InkTheme.CardFace : InkTheme.CardDim;
                if (i == lv && i < cap) _anim.Breathe(ring.transform, 0f, 0.08f, 1.1f);
            }
        }

        void BuildActions(CodexEntry e, int lv)
        {
            MetaProgress.StudyBlock block = _meta.UpgradeBlock(e);
            int cost = StudyCatalog.CostOf(lv, e.Kind == CodexKind.Word);
            if (lv < StudyCatalog.MaxLevel)
            {
                // 贴在升级按钮上沿，整行按按钮居中，图标和数字同一条线。
                const float costH = 48f;
                const float iconS = 36f;
                const float numW = 86f;
                const float ownW = 210f;
                float total = iconS + 6f + numW + 8f + ownW;
                float left = -total * 0.5f;
                var line = UiKit.Panel(_body, "cost", new Vector2(0f, 810f - 6f - costH), new Vector2(total + 8f, costH),
                    Color.clear, Pin.Top);
                line.GetComponent<Image>().raycastTarget = false;
                Color ink = _meta.Foil >= cost ? InkTheme.TextDark : InkTheme.Seal;
                UiKit.Icon(line, FoilArt.Icon, new Vector2(left + iconS * 0.5f, -1f), iconS);
                var n = UiKit.Label(line, "n", cost.ToString(), 34,
                    new Vector2(left + iconS + 6f + numW * 0.5f, 0f), new Vector2(numW, costH), TextAnchor.MiddleLeft);
                n.color = ink;
                UiKit.Bold(n);
                var own = UiKit.Label(line, "own", "（拥有 " + _meta.Foil + "）", 26,
                    new Vector2(left + iconS + 6f + numW + 8f + ownW * 0.5f, 0f), new Vector2(ownW, costH),
                    TextAnchor.MiddleLeft);
                own.color = ink;
            }

            string label = block == MetaProgress.StudyBlock.Max ? "已满级"
                : block == MetaProgress.StudyBlock.Cap ? "Lv." + _meta.StudyCap + " 已到上限"
                : "升级";
            var up = UiKit.Btn(_body, "up", label, new Vector2(0f, 810f), new Vector2(400f, 104f), Upgrade, true, Pin.Top);
            var upText = up.GetComponentInChildren<Text>();
            if (upText != null) upText.fontSize = 40;
            PanelKit.Dim(up, block != MetaProgress.StudyBlock.None);
            if (block == MetaProgress.StudyBlock.None) _anim.Breathe(up.transform, 0.2f, 0.035f, 1.2f);

            bool trial = _meta.HasTrial(e);
            string tl = trial ? "今日有效 · 用到才算"
                : "看广告试用 +" + StudyCatalog.TrialBonus + " 级（剩 " + _meta.TrialLeft + "）";
            var ad = UiKit.Btn(_body, "trial", tl, new Vector2(0f, 948f), new Vector2(420f, 76f), Trial, false, Pin.Top);
            var adText = ad.GetComponentInChildren<Text>();
            if (adText != null) adText.fontSize = 24;
            if (!trial) ResultKit.AdMark(ad, 32f);
            PanelKit.Dim(ad, !_meta.CanTrial(e), false);
        }

        void Step(int d)
        {
            if (Time.unscaledTime < _busyUntil) return;
            AudioBus.Tap();
            _at = (_at + d + _list.Count) % _list.Count;
            _sel = -1;
            _anim.Clear();
            Paint();
        }

        void Upgrade()
        {
            // 蓄力那 0.3 秒里旧字还在，连点或翻页会让爆点落到别的字上。
            if (Time.unscaledTime < _busyUntil) return;
            CodexEntry e = Cur;
            MetaProgress.StudyBlock block = _meta.UpgradeBlock(e);
            if (block != MetaProgress.StudyBlock.None)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, BlockTip(block));
                return;
            }
            int was = _meta.StudyLevel(e);
            int slotsWas = _meta.SlotsOpen(e);
            if (!_meta.UpgradeStudy(e)) return;
            int lv = _meta.StudyLevel(e);
            _busyUntil = Time.unscaledTime + StudyUpFx.Hit + 0.05f;
            AudioBus.StudyUp();
            _anim.Clear();
            var up = _body.Find("up");
            if (up != null) up.localScale = Vector3.one;
            bool tierUp = StudyCatalog.TierOf(lv) != StudyCatalog.TierOf(was);
            StudyUpFx.Play(_board, Vector2.zero, _body.Find("orb/c") as RectTransform, _body.Find("cost/icon") as RectTransform,
                lv, tierUp, () => Reveal(lv, slotsWas));
        }

        // 爆点那一下重绘，加成数字和新点亮的那颗等级点跟着跳；开了新槽，那颗槽也弹出来。
        RectTransform Reveal(int lv, int slotsWas)
        {
            Paint();
            // 重绘会把新内容盖到演出上面，品质横幅要留在最上层。
            Transform fx = _board.Find("study_up_fx");
            if (fx != null) fx.SetAsLastSibling();
            Transform bonus = _body.Find("bonus");
            if (bonus != null)
            {
                Transform num = bonus.Find(lv >= StudyCatalog.MaxLevel ? "v" : "a");
                if (num != null) _anim.Punch(num, 0.05f, 0.3f, 0.4f);
                if (bonus.Find("dot" + (lv - 1)) is RectTransform dot)
                {
                    _anim.Pop(dot, 0.12f, 0.42f, 0.2f);
                    _anim.At(0.12f, () =>
                    {
                        if (dot == null) return;
                        UiConfetti.Sparks(_body, StudyUpFx.Local(_body, dot),
                            StudyCatalog.TierColor(StudyCatalog.TierOf(lv)), 8, 300f);
                    });
                }
            }
            int slots = _meta.SlotsOpen(Cur);
            for (int k = slotsWas; k < slots; k++)
            {
                if (!(_orb.Find("slot" + k) is RectTransform node)) continue;
                int slot = k;
                Vector2 at = SlotAt[k];
                _anim.Pop(node, 0.35f, 0.45f, 0f);
                _anim.At(0.5f, () =>
                {
                    if (_orb == null) return;
                    UiConfetti.Sparks(_orb, at, FoilArt.Face, 14, 420f);
                    string name = SealCatalog.TypeName(_meta.SlotTypeOf(Cur, slot));
                    InkToast.Show(_layer, string.IsNullOrEmpty(name) ? "开了新插槽" : "开了新插槽：" + name);
                });
            }
            return _body.Find("orb/c") as RectTransform;
        }

        string BlockTip(MetaProgress.StudyBlock block)
        {
            switch (block)
            {
                case MetaProgress.StudyBlock.Max: return "已经满级了";
                case MetaProgress.StudyBlock.Cap:
                    int ch = _meta.ClearedCount() / GameConstants.ChapterSize + 1;
                    return "通关第" + ch + "章，等级上限 +1";
                case MetaProgress.StudyBlock.Foil: return "金箔不够，点上面的金箔能看到怎么拿";
                case MetaProgress.StudyBlock.NotOwned: return "还没在关卡里用过它";
                default: return "通关第四章开放字卡";
            }
        }

        void Trial()
        {
            CodexEntry e = Cur;
            if (_meta.HasTrial(e))
            {
                AudioBus.Deny();
                InkToast.Show(_layer, "今天打出来才算用掉，不用就还在，过了今天会清掉");
                return;
            }
            if (!_meta.CanTrial(e))
            {
                AudioBus.Deny();
                string waiting = _meta.TrialWaiting ? "「" + _meta.TrialTitle() + "」还没打出来，" : "";
                InkToast.Show(_layer, waiting + "今天的试用次数用完了");
                return;
            }
            if (_meta.TrialWaiting)
            {
                string now = CodexCatalog.Title(e);
                string old = _meta.TrialTitle();
                ConfirmPanel.Ask(_layer,
                    "「" + old + "」的试用还没打出来。\n换成「" + now + "」会清掉它，并再扣一次。\n今天不用，过了今天也会清掉。",
                    "先去打", "还是要换", () => WatchTrial(e));
                return;
            }
            WatchTrial(e);
        }

        void WatchTrial(CodexEntry e)
        {
            AdStub.Reward("glyph_trial", () =>
            {
                _meta.GrantTrial(e);
                if (this == null) return;
                InkToast.Show(_layer, CodexCatalog.Title(e) + " 今日 +" + StudyCatalog.TrialBonus + " 级，用到才算");
                _anim.Clear();
                Paint();
            });
        }
    }
}

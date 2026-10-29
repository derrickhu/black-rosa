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
        static readonly Vector2 OrbSize = new Vector2(620f, 430f);
        const float GlyphBox = 188f;
        // 字和左右槽相对轨道中心下移，顶上那颗槽才不会压住等级牌。
        const float Cluster = -48f;
        const float SlotSize = 96f;
        const float SheetY = 600f;
        static readonly Vector2 SheetSize = new Vector2(604f, 444f);

        // 三颗槽：上、左、右。上面那颗单独抬高，左右两颗跟着字走。
        static readonly Vector2[] SlotAt =
        {
            new Vector2(0f, 132f),
            new Vector2(-196f, Cluster),
            new Vector2(196f, Cluster),
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

            string wake = e.Kind == CodexKind.Glyph ? CodexCatalog.WakeNote((CardId)e.Index) : CodexCatalog.WakeNote((WordId)e.Index);
            var note = UiKit.Label(_body, "note", StudyCatalog.FamilyName(e) + " · " + wake, 22,
                new Vector2(0f, 556f), new Vector2(580f, 30f), TextAnchor.MiddleCenter, Pin.Top);
            note.color = InkTheme.TextDark;
            UiKit.Bold(note);
            string lore = e.Kind == CodexKind.Glyph ? CodexCatalog.Lore((CardId)e.Index) : CodexCatalog.WordLore((WordId)e.Index);
            var para = UiKit.Label(_body, "lore", lore, 19, new Vector2(0f, 588f), new Vector2(560f, 54f),
                TextAnchor.UpperCenter, Pin.Top);
            para.horizontalOverflow = HorizontalWrapMode.Wrap;
            para.verticalOverflow = VerticalWrapMode.Truncate;
            para.color = InkTheme.TextMid;

            BuildBonus(lv);
            BuildActions(e, lv);
            if (_sel >= 0) BuildSheet(e, _sel);
        }

        void BuildBadge(CodexEntry e, int lv)
        {
            Vector2 size = new Vector2(150f, 52f);
            var pill = UiKit.Stroke(_body, "lv", new Vector2(0f, 96f), size, Pin.Top, 4f, null, StudyCard.Badge(lv), 26f);
            var t = UiKit.Label(pill, "t", lv >= StudyCatalog.MaxLevel ? "MAX" : "Lv." + lv, 30, Vector2.zero, size);
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
            if (_meta.HasTrial(e))
            {
                var tag = UiKit.Stroke(_body, "trial_tag", new Vector2(130f, 106f), new Vector2(76f, 34f), Pin.Top, 3f,
                    null, InkTheme.Teal, 17f);
                var tt = UiKit.Label(tag, "t", "试用", 20, Vector2.zero, new Vector2(76f, 34f));
                tt.color = InkTheme.CardFace;
                UiKit.Bold(tt);
            }
        }

        // ---- 星尘轨道 ----

        void BuildOrbit(CodexEntry e, int lv)
        {
            _orb = UiKit.Panel(_body, "orb", new Vector2(0f, OrbY), OrbSize, Color.clear, Pin.Top);
            _orb.GetComponent<Image>().raycastTarget = false;

            var rays = UiKit.Icon(_orb, InkSprites.Load("Ui/result_rays"), new Vector2(0f, Cluster), 400f);
            rays.color = lv >= StudyCatalog.MaxLevel ? new Color(1f, 0.8f, 0.36f, 0.75f) : new Color(1f, 0.86f, 0.5f, 0.5f);
            _anim.Spin(rays.transform, lv >= StudyCatalog.MaxLevel ? 18f : 8f);
            var glow = UiKit.Icon(_orb, InkFx.SoftDisc(), new Vector2(0f, Cluster), 280f);
            glow.color = new Color(1f, 0.97f, 0.86f, 0.9f);

            BuildDust();

            // 字的托：升级演出按它的位置和大小放光。
            var c = UiKit.Panel(_orb, "c", new Vector2(0f, Cluster), new Vector2(GlyphBox, GlyphBox), Color.clear);
            c.GetComponent<Image>().raycastTarget = false;
            if (e.Kind == CodexKind.Glyph)
                UiKit.Icon(c, InkArt.Glyph((CardId)e.Index, 256), Vector2.zero, 176f);
            else
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int i = 0; i < parts.Length && i < 2; i++)
                    UiKit.Icon(c, InkArt.Glyph(parts[i], 256), new Vector2(i == 0 ? -48f : 48f, 0f), 108f);
            }
            _anim.Breathe(c, 0f, 0.025f, 0.45f);
            if (_meta.HasLuckSeal(e)) LuckMark(c);

            if (_list.Count > 1)
            {
                Arrow("prev", "<", new Vector2(-292f, Cluster), -1);
                Arrow("next", ">", new Vector2(292f, Cluster), 1);
            }

            bool open = _meta.SealOpen;
            int slots = _meta.SlotsOpen(e);
            for (int k = 0; k < SealCatalog.SlotsPer; k++) BuildSlot(e, k, open, k < slots);
            if (!open)
            {
                var gate = UiKit.Label(_orb, "gate", "通关第五章开放插槽", 21, new Vector2(0f, Cluster - 132f), new Vector2(360f, 30f));
                gate.color = InkTheme.TextMid;
            }
        }

        // 外圈一圈金色光点，下沿留空给字透气。大小、深浅错开，几颗慢慢一闪一闪。
        void BuildDust()
        {
            var dust = InkSprites.Load("Ui/seal_dust");
            const int n = 34;
            var hub = new Vector2(0f, Cluster);
            for (int i = 0; i < n; i++)
            {
                // 下沿留空，光点不压到槽下面的字，也不压到本事那一行。
                float deg = -50f + i * (280f / (n - 1));
                bool near = false;
                for (int k = 0; k < SlotAt.Length; k++)
                {
                    Vector2 d = SlotAt[k] - hub;
                    float sd = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    if (Mathf.Abs(Mathf.DeltaAngle(deg, sd)) < 16f) near = true;
                }
                if (near) continue;
                float rad = deg * Mathf.Deg2Rad;
                float r = 152f + ((i * 7) % 5 - 2) * 3f;
                float size = i % 3 == 0 ? 20f : i % 3 == 1 ? 13f : 9f;
                var dot = UiKit.Icon(_orb, dust, hub + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r, size);
                dot.color = new Color(1f, 1f, 1f, i % 3 == 2 ? 0.55f : 0.9f);
                if (i % 4 == 0) _anim.Breathe(dot.transform, i * 0.13f, 0.35f, 0.6f + (i % 3) * 0.2f);
            }
        }

        void Arrow(string name, string text, Vector2 pos, int d)
        {
            var b = UiKit.Btn(_orb, name, text, pos, new Vector2(56f, 84f), () => Step(d), false);
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.fontSize = 32;
        }

        static void LuckMark(RectTransform c)
        {
            var ring = UiKit.Icon(c, UiSprites.Disc(), new Vector2(GlyphBox * 0.42f, GlyphBox * 0.4f), 46f);
            ring.color = InkTheme.Outline;
            UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 40f).color = InkTheme.Seal;
            var t = UiKit.Label(ring.transform, "t", "运", 24, Vector2.zero, new Vector2(46f, 46f));
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
        }

        void BuildSlot(CodexEntry e, int k, bool open, bool unlocked)
        {
            Vector2 at = SlotAt[k];
            bool top = k == 0;
            if (!open || !unlocked)
            {
                var stud = SealArt.Stud(_orb, "slot" + k, at, SlotSize);
                Hit(stud, 118f, () => TapLocked(k));
                if (open) SlotCaption(k, at, "Lv." + SealCatalog.SlotLevelOf(k), "", InkTheme.TextMid, false);
                return;
            }
            SealType t = _meta.SlotTypeOf(e, k);
            int key = SealCatalog.KeyOf(e);
            bool filled = _meta.SlotSealOf(e, k, out SealQuality q);
            bool sel = _sel == k;
            if (sel)
            {
                var halo = UiKit.Icon(_orb, InkFx.SoftDisc(), at, 150f);
                halo.color = new Color(1f, 0.84f, 0.4f, 0.85f);
                _anim.Breathe(halo.transform, 0f, 0.08f, 0.8f);
            }
            RectTransform node = filled
                ? SealArt.Medal(_orb, "slot" + k, t, q, at, SlotSize)
                : SealArt.Empty(_orb, "slot" + k, t, at, SlotSize);
            if (filled && q == SealQuality.Gold) _anim.Breathe(node, 0.4f, 0.06f, 0.9f);
            else if (!filled && _meta.BagCount(t) > 0) _anim.Breathe(node, 0.4f, 0.07f, 0.9f);
            Hit(node, 96f, () => Select(k));

            string line1 = SealCatalog.TypeName(t);
            string line2 = filled ? SealCatalog.ValueText(key, t, q) : _meta.BagCount(t) > 0 ? "可镶嵌" : "空槽";
            Color c2 = filled ? SealCatalog.QualityDeep(q) : _meta.BagCount(t) > 0 ? InkTheme.Seal : InkTheme.TextMid;
            SlotCaption(k, at, line1, line2, c2, true);
            if (sel) RerollTag(e, k, at, top);
        }

        // 字写在槽外面：上面那颗在右侧，左右两颗在下方，都不压在章面上。
        void SlotCaption(int k, Vector2 at, string line1, string line2, Color valueColor, bool bold)
        {
            bool top = k == 0;
            Vector2 lp = top ? at + new Vector2(132f, 0f) : at + new Vector2(0f, -82f);
            var l1 = UiKit.Label(_orb, "slot" + k + "_t", line1, 22, lp + new Vector2(top ? 8f : 0f, line2 == "" ? 0f : 14f),
                new Vector2(top ? 150f : 120f, 28f), top ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            if (bold) UiKit.Bold(l1);
            if (line2 == "")
            {
                l1.color = valueColor;
                return;
            }
            var l2 = UiKit.Label(_orb, "slot" + k + "_v", line2, 20, lp + new Vector2(top ? 8f : 0f, -14f),
                new Vector2(top ? 160f : 130f, 26f), top ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            l2.color = valueColor;
        }

        // 选中的那颗旁边一行小字「刷新 · 10」，点了直接花钻石刷。
        void RerollTag(CodexEntry e, int k, Vector2 at, bool top)
        {
            Vector2 pos = top ? at + new Vector2(-132f, 0f) : at + new Vector2(0f, 86f);
            Vector2 size = new Vector2(136f, 40f);
            var chip = UiKit.Stroke(_orb, "reroll_tag", pos, size, Pin.Center, 3f, InkTheme.Outline, InkTheme.Hex("FFF1C9"), 20f);
            UiKit.Icon(chip, InkSprites.Ui("diamond"), new Vector2(-48f, 0f), 30f);
            var t = UiKit.Label(chip, "t", "刷新 · " + SealCatalog.RerollDiamond, 20, new Vector2(14f, 0f), size);
            t.color = _meta.Diamond >= SealCatalog.RerollDiamond ? InkTheme.TextDark : InkTheme.Seal;
            UiKit.Bold(t);
            Hit(chip, 0f, () => Reroll(false));
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

            var head = UiKit.Label(sheet, "h", "第 " + (k + 1) + " 槽 · " + SealCatalog.TypeName(t), 28,
                new Vector2(-w * 0.5f + 34f, h * 0.5f - 40f), new Vector2(400f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(head);
            var x = UiKit.Btn(sheet, "x", "收起", new Vector2(w * 0.5f - 76f, h * 0.5f - 40f), new Vector2(112f, 56f),
                () => Select(_sel), false);
            var xt = x.GetComponentInChildren<Text>();
            if (xt != null) xt.fontSize = 22;
            var eff = UiKit.Label(sheet, "eff", SealCatalog.EffectOf(key, t), 21,
                new Vector2(-w * 0.5f + 34f, h * 0.5f - 84f), new Vector2(w - 60f, 30f), TextAnchor.MiddleLeft);
            eff.color = InkTheme.TextMid;

            // 四个品质一字排开：有货的可以点，点了就镶（已经镶着的换下来退回背包）。
            const float cellW = 132f, cellH = 168f, gap = 10f;
            float x0 = -(cellW + gap) * 1.5f;
            int have = _meta.BagCount(t);
            for (int qi = 0; qi < SealCatalog.QualityCount; qi++)
            {
                var q = (SealQuality)qi;
                int n = _meta.BagCount(t, q);
                bool on = filled && q == cur;
                Vector2 pos = new Vector2(x0 + qi * (cellW + gap), 6f);
                var cell = UiKit.Stroke(sheet, "q" + qi, pos, new Vector2(cellW, cellH), Pin.Center, on ? 5f : 3f,
                    on ? SealCatalog.QualityDeep(q) : InkTheme.LineDim, on ? InkTheme.Hex("FFEFC4") : InkTheme.CardFace, 20f);
                var medal = SealArt.Medal(cell, "m", t, q, new Vector2(0f, 26f), 78f);
                var v = UiKit.Label(cell, "v", SealCatalog.ValueText(key, t, q), 20, new Vector2(0f, -32f), new Vector2(cellW, 26f));
                v.color = SealCatalog.QualityDeep(q);
                UiKit.Bold(v);
                var c = UiKit.Label(cell, "n", on ? "镶着" : "×" + n, 19, new Vector2(0f, -60f), new Vector2(cellW, 24f));
                c.color = on ? InkTheme.Seal : n > 0 ? InkTheme.TextDark : InkTheme.TextDim;
                if (!on && n <= 0)
                {
                    foreach (var img in medal.GetComponentsInChildren<Image>()) img.color *= new Color(1f, 1f, 1f, 0.35f);
                }
                SealQuality pick = q;
                Hit(cell, 0f, () => Socket(pick));
            }
            if (have == 0 && !filled)
            {
                var none = UiKit.Label(sheet, "none", "还没有" + SealCatalog.TypeName(t) + "印章 · 首通第五章后的关卡、开银金皇家宝箱会掉",
                    18, new Vector2(0f, -94f), new Vector2(w - 40f, 26f));
                none.color = InkTheme.TextMid;
            }

            // 底下一排：卸下（有印章时）、刷新类型（金箔 / 广告）。
            float by = -h * 0.5f + 52f;
            if (filled)
            {
                var off = UiKit.Btn(sheet, "off", "卸下", new Vector2(-w * 0.5f + 92f, by), new Vector2(140f, 70f), Unsocket, false);
                var ot = off.GetComponentInChildren<Text>();
                if (ot != null) ot.fontSize = 24;
            }
            var gem = UiKit.Btn(sheet, "rr_gem", "刷新 " + SealCatalog.RerollDiamond, new Vector2(filled ? 26f : -100f, by),
                new Vector2(170f, 70f), () => Reroll(false), false);
            UiKit.Icon(gem.transform.Find("face"), InkSprites.Ui("diamond"), new Vector2(-62f, 0f), 34f);
            var ft = gem.GetComponentInChildren<Text>();
            if (ft != null)
            {
                ft.fontSize = 24;
                ft.rectTransform.anchoredPosition = new Vector2(14f, 0f);
            }
            PanelKit.Dim(gem, _meta.Diamond < SealCatalog.RerollDiamond, false);
            var ad = UiKit.Btn(sheet, "rr_ad", "刷新（剩 " + _meta.SealAdLeft + "）", new Vector2(filled ? 200f : 110f, by),
                new Vector2(filled ? 170f : 210f, 70f), () => Reroll(true), false);
            var at = ad.GetComponentInChildren<Text>();
            if (at != null) at.fontSize = 22;
            ResultKit.AdMark(ad, 30f);
            PanelKit.Dim(ad, _meta.SealAdLeft <= 0, false);
            var tip = UiKit.Label(sheet, "tip", "刷新只换这一槽的类型，槽里的印章退回背包", 17,
                new Vector2(0f, -h * 0.5f + 12f), new Vector2(w, 22f));
            tip.color = InkTheme.TextDim;
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

        void Reroll(bool ad)
        {
            if (Time.unscaledTime < _busyUntil) return;
            CodexEntry e = Cur;
            int k = _sel;
            MetaProgress.SealBlock block = _meta.RerollBlock(e, k, ad);
            if (block != MetaProgress.SealBlock.None)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, block == MetaProgress.SealBlock.Diamond ? "钻石不够，刷新要 " + SealCatalog.RerollDiamond
                    : block == MetaProgress.SealBlock.AdUsed ? "今天看广告刷新的次数用完了" : "这个槽还没开");
                return;
            }
            if (!ad)
            {
                DoReroll(e, k, false);
                return;
            }
            AdStub.Reward("seal_reroll", () =>
            {
                if (this == null) return;
                DoReroll(e, k, true);
            });
        }

        void DoReroll(CodexEntry e, int k, bool ad)
        {
            bool had = _meta.SlotSealOf(e, k, out _);
            SealType t = _meta.Reroll(e, k, ad);
            if (t == SealType.None) return;
            _busyUntil = Time.unscaledTime + 0.4f;
            AudioBus.CardFlip(1.15f);
            _anim.Clear();
            Paint();
            _closed?.Invoke();
            if (_orb.Find("slot" + k) is RectTransform node)
            {
                Vector2 at = SlotAt[k];
                _anim.Tween(0f, 0.4f, u =>
                {
                    if (node == null) return;
                    node.localRotation = Quaternion.Euler(0f, 0f, (1f - Ease.OutCubic(u)) * 360f);
                }).Pop(node, 0f, 0.4f, 0.4f);
                _anim.At(0.3f, () =>
                {
                    if (_orb != null) UiConfetti.Sparks(_orb, at, FoilArt.Face, 12, 420f);
                });
            }
            InkToast.Show(_layer, "刷出了「" + SealCatalog.TypeName(t) + "」" + (had ? "，原来的印章退回背包" : ""));
        }

        // ---- 加成、升级 ----

        void BuildBonus(int lv)
        {
            var box = UiKit.Panel(_body, "bonus", new Vector2(0f, 646f), new Vector2(580f, 104f), Color.clear, Pin.Top);
            box.GetComponent<Image>().raycastTarget = false;
            int now = Mathf.RoundToInt(StudyCatalog.BonusOf(lv) * 100f);
            if (lv >= StudyCatalog.MaxLevel)
            {
                var full = UiKit.Label(box, "v", "伤害 +" + now + "%", 40, new Vector2(0f, 22f), new Vector2(400f, 52f));
                full.color = InkTheme.Seal;
                UiKit.Bold(full);
            }
            else
            {
                int next = Mathf.RoundToInt(StudyCatalog.BonusOf(lv + 1) * 100f);
                var a = UiKit.Label(box, "a", "+" + now + "%", 40, new Vector2(-96f, 22f), new Vector2(180f, 52f));
                UiKit.Bold(a);
                var arrow = UiKit.Label(box, "arrow", "→", 34, new Vector2(0f, 22f), new Vector2(60f, 52f));
                arrow.color = InkTheme.TextMid;
                var b = UiKit.Label(box, "b", "+" + next + "%", 40, new Vector2(96f, 22f), new Vector2(180f, 52f));
                b.color = InkTheme.Seal;
                UiKit.Bold(b);
            }

            int cap = _meta.StudyCap;
            const float step = 40f;
            for (int i = 0; i < StudyCatalog.MaxLevel; i++)
            {
                Vector2 at = new Vector2((i - (StudyCatalog.MaxLevel - 1) * 0.5f) * step, -30f);
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
            int cost = StudyCatalog.CostOf(lv);
            if (lv < StudyCatalog.MaxLevel)
            {
                var line = UiKit.Panel(_body, "cost", new Vector2(0f, 752f), new Vector2(460f, 44f), Color.clear, Pin.Top);
                line.GetComponent<Image>().raycastTarget = false;
                Color ink = _meta.Foil >= cost ? InkTheme.TextDark : InkTheme.Seal;
                UiKit.Icon(line, FoilArt.Icon, new Vector2(-190f, 0f), 36f);
                var n = UiKit.Label(line, "n", cost.ToString(), 26, new Vector2(-118f, 0f),
                    new Vector2(76f, 40f), TextAnchor.MiddleLeft);
                n.color = ink;
                UiKit.Bold(n);
                var own = UiKit.Label(line, "own", "（拥有 " + _meta.Foil + "）", 22, new Vector2(48f, 0f),
                    new Vector2(210f, 40f), TextAnchor.MiddleLeft);
                own.color = ink;
            }

            string label = block == MetaProgress.StudyBlock.Max ? "已满级"
                : block == MetaProgress.StudyBlock.Cap ? "Lv." + _meta.StudyCap + " 已到上限"
                : "升级";
            var up = UiKit.Btn(_body, "up", label, new Vector2(0f, 798f), new Vector2(400f, 104f), Upgrade, true, Pin.Top);
            var upText = up.GetComponentInChildren<Text>();
            if (upText != null) upText.fontSize = 40;
            PanelKit.Dim(up, block != MetaProgress.StudyBlock.None);
            if (block == MetaProgress.StudyBlock.None) _anim.Breathe(up.transform, 0.2f, 0.035f, 1.2f);

            bool trial = _meta.HasTrial(e);
            string tl = trial ? "今日有效 · 用到才算"
                : "看广告试用 +" + StudyCatalog.TrialBonus + " 级（剩 " + _meta.TrialLeft + "）";
            var ad = UiKit.Btn(_body, "trial", tl, new Vector2(0f, 924f), new Vector2(420f, 76f), Trial, false, Pin.Top);
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
                Vector2 at = SlotAt[k];
                _anim.Pop(node, 0.35f, 0.45f, 0f);
                _anim.At(0.5f, () =>
                {
                    if (_orb == null) return;
                    UiConfetti.Sparks(_orb, at, FoilArt.Face, 14, 420f);
                    InkToast.Show(_layer, "开了新插槽：" + SealCatalog.TypeName(_meta.SlotTypeOf(Cur, k)));
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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 字卡详情弹窗（Figma Page 1 Panel/字卡弹窗 v2）：
    // 上半品质色舞台，书法字居中落于米字格圆盘，三颗镶嵌座沿罗盘环排在上、左、右，左上票据标签（类型、品质、试用）；
    // 下半十格等级条、整体伤害加成暗牌、看广告试用与金箔升级按钮；点开已解锁插槽从底部弹出镶嵌选择抽屉。
    public sealed class GlyphDetailPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/GlyphDetail";
        public const string Mark = "glyph_detail_v1";

        const float BoardW = 648f;
        const float BoardH = 828f;
        const float HeadH = 484f;
        const float StageCx = 324f;
        const float StageCy = 272f;
        const float OrbCenterY = BoardH * 0.5f - StageCy; // 142
        const float Ring = 186f;
        const float GlyphBox = 192f;
        const float SlotW = 100f;
        const float SlotH = 104f;

        static readonly Vector4 BorderTicket = new Vector4(32f, 0f, 32f, 0f);
        static readonly Vector4 BorderSlotCap = new Vector4(24f, 0f, 24f, 0f);
        static readonly Vector4 BorderBonus = new Vector4(40f, 0f, 40f, 0f);
        static readonly Vector4 BorderLvTrack = new Vector4(28f, 0f, 28f, 0f);

        // 三颗槽相对字盘中心：上、左、右。
        static readonly Vector2[] SlotAt =
        {
            new Vector2(0f, Ring),
            new Vector2(-Ring, 0f),
            new Vector2(Ring, 0f),
        };

        MetaProgress _meta;
        RectTransform _layer;
        RectTransform _stage;
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

            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "study_detail", Mark, null, BuildPrefab, out RectTransform stage);
            var p = dim.gameObject.AddComponent<GlyphDetailPanel>();
            p._meta = meta;
            p._layer = layer;
            p._stage = stage;
            p._closed = closed;
            p._list = list;
            p._at = Mathf.Clamp(at, 0, list.Count - 1);
            p._anim = UiAnim.On(p);

            p._board = stage.Find("board") as RectTransform;
            ModalKit.BindClose(p._board, p.Close);

            var prev = stage.Find("prev");
            var next = stage.Find("next");
            bool multi = list.Count > 1;
            if (prev != null)
            {
                prev.gameObject.SetActive(multi);
                if (multi) ModalKit.BindBtn(stage, "prev", () => p.Step(-1));
            }
            if (next != null)
            {
                next.gameObject.SetActive(multi);
                if (multi) ModalKit.BindBtn(stage, "next", () => p.Step(1));
            }

            p.Paint();
            PanelKit.Open(p._board);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "study_detail", 0.80f, null, out RectTransform stage);
            var board = BuildShell(stage, () => { });
            ModalKit.AddMark(board, Mark);
            BuildArrowNode(stage, "prev", "Ui/home_arrow_on_l", 4f);
            BuildArrowNode(stage, "next", "Ui/home_arrow_on", ModalKit.DesignW - 4f - 47f);
            return dim;
        }

        static RectTransform BuildShell(RectTransform stage, Action onClose)
        {
            var go = new GameObject("board", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(stage, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(BoardW, BoardH);

            var img = go.GetComponent<Image>();
            img.sprite = ModalKit.Load2x("Ui/modal_panel_body", ModalKit.BorderPanel);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = Color.white;
            img.raycastTarget = true;

            // 下半奶油底散落浅暗纹四角星
            float[,] marks =
            {
                { 0.14f, 0.64f, 18f }, { 0.86f, 0.63f, 16f },
                { 0.48f, 0.82f, 20f }, { 0.28f, 0.94f, 15f }, { 0.78f, 0.93f, 17f },
            };
            for (int i = 0; i < marks.GetLength(0); i++)
            {
                float s = marks[i, 2];
                float x = marks[i, 0] * BoardW - s * 0.5f;
                float y = marks[i, 1] * BoardH - s * 0.5f;
                ModalKit.Pic(rt, "mark" + i, "Ui/modal_star_mark", x, y, s, s, true);
            }

            ModalKit.CloseBtn(rt, 0f, 0f, onClose);
            return rt;
        }

        static void BuildArrowNode(RectTransform stage, string name, string sprKey, float x)
        {
            const float aw = 47f, ah = 51f;
            float boardTop = (ModalKit.DesignH - BoardH) * 0.5f;
            float cy = boardTop + StageCy;

            var hitGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            hitGo.transform.SetParent(stage, false);
            ModalKit.TopLeft(hitGo.GetComponent<RectTransform>(), x - 9f, cy - 44f, aw + 18f, 88f);
            var hit = hitGo.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.005f);
            hit.raycastTarget = true;

            var art = ModalKit.Pic(hitGo.transform, "art", sprKey, 9f, (88f - ah) * 0.5f, aw, ah, true);
            art.raycastTarget = false;

            var btn = hitGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
        }

        void OnRectTransformDimensionsChange()
        {
            if (_stage != null) ModalKit.FitStage(_stage, _layer);
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

            CodexEntry e = Cur;
            int lv = _meta.StudyLevel(e);
            StudyCatalog.Tier tier = StudyCatalog.TierOf(lv);

            BuildStageAndOrbit(e, lv, tier);
            BuildHeader(e, lv);
            BuildBonus(lv, tier);
            BuildActions(e, lv);
            if (_sel >= 0) BuildSheet(e, _sel, tier);

            Transform close = _board.Find("close");
            if (close != null) close.SetAsLastSibling();
        }

        // ---- 上半品质舞台 + 字盘 + 三颗环绕插槽 ----

        void BuildStageAndOrbit(CodexEntry e, int lv, StudyCatalog.Tier tier)
        {
            var stageArt = ModalKit.Pic(_body, "stage_bg", "Ui/glyph_stage_" + StageKey(tier), 0f, 0f, BoardW, HeadH, false);
            stageArt.raycastTarget = false;

            _orb = UiKit.Panel(_body, "orb", new Vector2(0f, OrbCenterY), new Vector2(BoardW, HeadH), Color.clear);
            _orb.GetComponent<Image>().raycastTarget = false;

            // 中央书法字（升级演出对准 orb/c 蓄力与爆光）
            var c = UiKit.Panel(_orb, "c", Vector2.zero, new Vector2(GlyphBox, GlyphBox), Color.clear);
            c.GetComponent<Image>().raycastTarget = false;
            if (e.Kind == CodexKind.Glyph)
            {
                var g = UiKit.Icon(c, InkArt.CardGlyph((CardId)e.Index, false), Vector2.zero, GlyphBox);
                g.raycastTarget = false;
            }
            else
            {
                CardId[] parts = CodexCatalog.PartsOf((WordId)e.Index);
                for (int i = 0; i < parts.Length && i < 2; i++)
                {
                    float ox = parts.Length == 1 ? 0f : (i == 0 ? -44f : 44f);
                    float sz = parts.Length == 1 ? GlyphBox : 84f;
                    var g = UiKit.Icon(c, InkArt.WordGlyph(parts[i], true), new Vector2(ox, 0f), sz);
                    g.raycastTarget = false;
                }
            }
            _anim.Breathe(c, 0f, 0.02f, 0.4f);

            bool open = _meta.SealOpen;
            int slots = _meta.SlotsOpen(e);
            for (int k = 0; k < SealCatalog.SlotsPer; k++)
                BuildSlot(e, lv, tier, k, open, k < slots);
        }

        void BuildSlot(CodexEntry e, int lv, StudyCatalog.Tier tier, int k, bool open, bool unlocked)
        {
            Vector2 at = SlotAt[k];
            if (!open || !unlocked)
            {
                var stud = MakeSlotNode(_orb, "slot" + k, at, "Ui/glyph_slot_locked");
                Hit(stud, 94f, () => TapLocked(k));
                int needLv = SealCatalog.SlotLevelOf(k);
                string lockCap = !open && lv >= needLv ? "五章开放" : "Lv." + needLv + " 解锁";
                SlotCaption(at, lockCap, "", InkTheme.Hex("CFC6B8"));
                return;
            }

            SealType t = _meta.SlotTypeOf(e, k);
            int key = SealCatalog.KeyOf(e);
            bool filled = _meta.SlotSealOf(e, k, out SealQuality q);

            if (_sel == k || (filled && q == SealQuality.Gold))
            {
                var halo = UiKit.Icon(_orb, InkSprites.Load("Ui/seal_burst"), at, _sel == k ? 196f : 168f);
                halo.color = Color.white;
                halo.raycastTarget = false;
                _anim.Spin(halo.transform, 22f);
            }

            RectTransform node;
            if (filled)
            {
                node = MakeSlotNode(_orb, "slot" + k, at, "Ui/glyph_slot_" + QualityKey(q));
                var ico = UiKit.Icon(node, SealArt.Icon(t), Vector2.zero, 50f);
                ico.raycastTarget = false;
                if (_sel == k || q == SealQuality.Gold) _anim.Breathe(node, 0.4f, 0.05f, 0.85f);

                SealCatalog.Line(key, t, q, out _, out string amount);
                SlotCaption(at, SealCatalog.TypeName(t), amount, FilledValueColor(q));
            }
            else
            {
                bool hasBag = _meta.BagCount(t) > 0;
                node = MakeSlotNode(_orb, "slot" + k, at, "Ui/glyph_slot_empty_" + StageKey(tier));
                var ico = UiKit.Icon(node, SealArt.Icon(t), Vector2.zero, 44f);
                ico.color = new Color(1f, 1f, 1f, 0.55f);
                ico.raycastTarget = false;
                if (hasBag)
                {
                    var plus = UiKit.Icon(node, ModalKit.Load("Ui/glyph_slot_plus"), new Vector2(35f, 34f), 28f);
                    plus.raycastTarget = false;
                    _anim.Breathe(node, 0.4f, 0.05f, 0.85f);
                }
                SlotCaption(at, SealCatalog.TypeName(t), hasBag ? "可镶嵌" : "空",
                    hasBag ? InkTheme.Hex("FFE455") : InkTheme.Hex("9EA4AE"));
            }

            Hit(node, 96f, () => Select(k));
        }

        static RectTransform MakeSlotNode(Transform parent, string name, Vector2 at, string spriteKey)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = at;
            rt.sizeDelta = new Vector2(SlotW, SlotH);
            var img = go.GetComponent<Image>();
            img.sprite = ModalKit.Load(spriteKey);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;
            return rt;
        }

        void SlotCaption(Vector2 slotPos, string label, string val, Color valColor)
        {
            string plain = string.IsNullOrEmpty(val) ? label : label + " " + val;
            float w = Mathf.Max(84f, TextWidth(plain, 15f) + 26f);
            const float h = 28f;

            var capGo = new GameObject("cap", typeof(RectTransform), typeof(Image));
            capGo.transform.SetParent(_orb, false);
            var rt = capGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = slotPos + new Vector2(0f, -68f);
            rt.sizeDelta = new Vector2(w, h);

            var bg = capGo.GetComponent<Image>();
            bg.sprite = ModalKit.Load2x("Ui/glyph_slot_cap", BorderSlotCap);
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1f;
            bg.color = Color.white;
            bg.raycastTarget = false;

            string rich = string.IsNullOrEmpty(val) ? label : label + " " + Tint(val, valColor);
            var t = UiKit.Label(rt, "t", rich, 15, Vector2.zero, new Vector2(w - 8f, h));
            t.supportRichText = true;
            t.color = string.IsNullOrEmpty(val) ? valColor : ModalKit.CreamLight;
            t.raycastTarget = false;
            UiKit.Bold(t);
        }

        // ---- 左上票据标签 + 右上刷新 ----

        void BuildHeader(CodexEntry e, int lv)
        {
            float tx = 22f;
            string fam = StudyCatalog.FamilyName(e);
            if (!string.IsNullOrEmpty(fam))
                tx += Ticket(_body, tx, 22f, fam, FamilyTagKey(e)) + 8f;

            if (_meta.HasTrial(e))
                Ticket(_body, tx, 22f, "试用", "teal");

            if (_meta.SealOpen && _meta.SlotsOpen(e) > 0)
                BuildReroll();
        }

        static float Ticket(Transform parent, float x, float y, string text, string tone)
        {
            float w = Mathf.Max(68f, TextWidth(text, 21f) + 40f);
            const float h = 42f;
            var tag = ModalKit.Sliced(parent, "tag_" + text, "Ui/glyph_tag_" + tone, BorderTicket, x, y, w, h, false);
            ModalKit.OutlinedText(tag, "t", text, 21, 0f, 0f, w, h - 3f, Color.white, TextAnchor.MiddleCenter, 1.6f);
            return w;
        }

        void BuildReroll()
        {
            var go = new GameObject("rr", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_body, false);
            ModalKit.TopLeft(go.GetComponent<RectTransform>(), BoardW - 104f, 52f, 76f, 92f);
            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0.005f);
            hit.raycastTarget = true;

            ModalKit.Pic(go.transform, "ico", null, 8f, 0f, 60f, 60f, true).sprite = InkSprites.Ui("reroll");
            ModalKit.Pic(go.transform, "gem", null, 10f, 64f, 24f, 24f, true).sprite = InkSprites.Ui("diamond");
            ModalKit.OutlinedText(go.transform, "n", SealCatalog.RerollDiamond.ToString(), 18,
                36f, 62f, 40f, 28f, Color.white, TextAnchor.MiddleLeft, 1.45f);

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(AskReroll);
        }

        // ---- 十格等级条 + 整体伤害加成 ----

        void BuildBonus(int lv, StudyCatalog.Tier tier)
        {
            var bonusGo = new GameObject("bonus", typeof(RectTransform));
            bonusGo.transform.SetParent(_body, false);
            var bonus = bonusGo.GetComponent<RectTransform>();
            bonus.anchorMin = Vector2.zero;
            bonus.anchorMax = Vector2.one;
            bonus.offsetMin = Vector2.zero;
            bonus.offsetMax = Vector2.zero;

            ModalKit.OutlinedText(bonus, "lv", "Lv." + lv, 26, 28f, 510f, 92f, 40f,
                LvTextColor(tier), TextAnchor.MiddleLeft, 1.6f);

            const float trackX = 124f, trackY = 515f, trackW = 498f, trackH = 30f;
            ModalKit.Sliced(bonus, "lv_track", "Ui/glyph_lv_track", BorderLvTrack, trackX, trackY, trackW, trackH, false);

            string segOn = "Ui/glyph_lv_seg_" + SegKey(tier);
            const float segW = 45f, segH = 20f, gap = 4f;
            for (int i = 0; i < StudyCatalog.MaxLevel; i++)
            {
                float sx = trackX + 6f + i * (segW + gap);
                float sy = trackY + 5f;
                var seg = ModalKit.Pic(bonus, "dot" + i, i < lv ? segOn : "Ui/glyph_lv_seg_off", sx, sy, segW, segH, false);
                seg.raycastTarget = false;
            }

            var bar = ModalKit.Sliced(bonus, "bonus_bar", "Ui/glyph_bonus_bar", BorderBonus, 24f, 566f, 600f, 70f, false);
            int now = Mathf.RoundToInt(StudyCatalog.BonusOf(lv) * 100f);
            bool max = lv >= StudyCatalog.MaxLevel;
            if (max)
            {
                ModalKit.SolidText(bar, "what", "当前效果：字的整体伤害", 20, 26f, 0f, 260f, 70f,
                    ModalKit.CreamLight, TextAnchor.MiddleLeft, true);
                // 挂在 bonus 下且以 v 命名，升级满级爆点时 Reveal 会弹它
                var v = ModalKit.OutlinedText(bonus, "v", "+" + now + "%", 34,
                    24f + 380f, 566f, 194f, 70f, InkTheme.Hex("FFE455"), TextAnchor.MiddleRight, 1.6f);
                v.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                v.rectTransform.anchoredPosition = new Vector2(24f + 380f + 97f, -(566f + 35f));
            }
            else
            {
                ModalKit.SolidText(bar, "what", "下一级效果", 15, 26f, 9f, 200f, 24f,
                    InkTheme.Hex("FFE455"), TextAnchor.MiddleLeft, true);
                ModalKit.SolidText(bar, "sub", "字的整体伤害", 20, 26f, 32f, 200f, 30f,
                    ModalKit.CreamLight, TextAnchor.MiddleLeft, true);

                int next = Mathf.RoundToInt(StudyCatalog.BonusOf(lv + 1) * 100f);
                var a = ModalKit.OutlinedText(bonus, "a", "+" + now + "%", 32,
                    24f + 236f, 566f, 124f, 70f, Color.white, TextAnchor.MiddleRight, 1.6f);
                a.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                a.rectTransform.anchoredPosition = new Vector2(24f + 236f + 62f, -(566f + 35f));

                ModalKit.SolidText(bar, "arrow", "→", 26, 370f, 0f, 50f, 70f,
                    InkTheme.Hex("CFC6B8"), TextAnchor.MiddleCenter, true);
                ModalKit.OutlinedText(bar, "b", "+" + next + "%", 34,
                    428f, 0f, 152f, 70f, InkTheme.Hex("FFE455"), TextAnchor.MiddleLeft, 1.6f);
            }
        }

        // ---- 试用 + 金箔升级 ----

        void BuildActions(CodexEntry e, int lv)
        {
            bool trial = _meta.HasTrial(e);
            string trialNote = trial ? "用到才算" : "今日剩 " + _meta.TrialLeft + " 次";
            ModalKit.SolidText(_body, "trial_note", trialNote, 16, 34f, 676f, 176f, 28f,
                ModalKit.TextMuted, TextAnchor.MiddleCenter, false);

            string trialKind = (!trial && !_meta.CanTrial(e)) ? "md_gray" : "md_cyan";
            var trBtn = ModalKit.HexBtn(_body, trialKind, 34f, 724f, "", Trial, out _);
            ModalKit.SetAdBtnLabel(trBtn, trialKind,
                trial ? "今日试用中" : "试用 +" + StudyCatalog.TrialBonus + "级",
                !trial);

            MetaProgress.StudyBlock block = _meta.UpgradeBlock(e);
            int cost = StudyCatalog.CostOf(lv, e.Kind == CodexKind.Word);
            if (lv < StudyCatalog.MaxLevel)
            {
                var costHold = new GameObject("cost", typeof(RectTransform));
                costHold.transform.SetParent(_body, false);
                var crt = costHold.GetComponent<RectTransform>();
                ModalKit.TopLeft(crt, 300f, 672f, 314f, 34f);

                string costStr = cost.ToString();
                string ownStr = "（拥有 " + _meta.Foil + "）";
                float costW = Mathf.Max(34f, TextWidth(costStr, 22f) + 6f);
                float ownW = TextWidth(ownStr, 16f) + 8f;
                float totalW = 34f + 6f + costW + 2f + ownW;
                float startX = Mathf.Max(0f, (314f - totalW) * 0.5f);

                bool enough = _meta.Foil >= cost;
                // 居中锚定在图标自身中心，确保 StudyUpFx.Local 取飞入起点准确
                var icoGo = new GameObject("icon", typeof(RectTransform), typeof(Image));
                icoGo.transform.SetParent(crt, false);
                var irt = icoGo.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 1f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = new Vector2(startX + 17f, -17f);
                irt.sizeDelta = new Vector2(34f, 34f);
                var iimg = icoGo.GetComponent<Image>();
                iimg.sprite = FoilArt.Icon;
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;

                ModalKit.SolidText(crt, "n", costStr, 22, startX + 40f, 2f, costW, 30f,
                    enough ? ModalKit.TextDark : InkTheme.Seal, TextAnchor.MiddleLeft, true);
                ModalKit.SolidText(crt, "own", ownStr, 16, startX + 40f + costW + 2f, 2f, ownW, 30f,
                    enough ? ModalKit.TextMuted : InkTheme.Seal, TextAnchor.MiddleLeft, false);
            }
            else
            {
                ModalKit.SolidText(_body, "max_note", "已经满级", 16, 300f, 676f, 314f, 28f,
                    ModalKit.TextMuted, TextAnchor.MiddleCenter, false);
            }

            string upLabel = block == MetaProgress.StudyBlock.Max ? "已满级"
                : block == MetaProgress.StudyBlock.Cap ? "Lv." + _meta.StudyCap + " 已到上限"
                : "升级";
            bool canUp = block == MetaProgress.StudyBlock.None;
            var up = ModalKit.HexBtn(_body, canUp ? "wide_yellow" : "wide_gray", 300f, 716f, upLabel, Upgrade, out _);
            up.gameObject.name = "up";
            if (canUp)
            {
                // 把按钮 pivot 移到几何中心，呼吸缩放不漂移
                var urt = up.GetComponent<RectTransform>();
                urt.pivot = new Vector2(0.5f, 0.5f);
                urt.anchoredPosition = new Vector2(300f + 157f, -(716f + 40f));
                _anim.Breathe(up.transform, 0.2f, 0.03f, 1.2f);
            }
        }

        // ---- 底部印章镶嵌选择抽屉 ----

        void BuildSheet(CodexEntry e, int k, StudyCatalog.Tier tier)
        {
            if (k >= _meta.SlotsOpen(e)) return;
            SealType t = _meta.SlotTypeOf(e, k);
            int key = SealCatalog.KeyOf(e);
            bool filled = _meta.SlotSealOf(e, k, out SealQuality cur);

            var catchGo = new GameObject("sheet_catch", typeof(RectTransform), typeof(Image), typeof(Button));
            catchGo.transform.SetParent(_body, false);
            ModalKit.TopLeft(catchGo.GetComponent<RectTransform>(), 0f, 0f, BoardW, BoardH);
            var catchImg = catchGo.GetComponent<Image>();
            catchImg.color = new Color(0.05f, 0.02f, 0.04f, 0.28f);
            catchImg.raycastTarget = true;
            var catchBtn = catchGo.GetComponent<Button>();
            catchBtn.transition = Selectable.Transition.None;
            catchBtn.targetGraphic = catchImg;
            catchBtn.onClick.AddListener(() => Select(_sel));

            const float sx = 16f, sy = 368f, sw = 616f, sh = 444f;
            var sheet = ModalKit.Sliced(_body, "sheet", "Ui/modal_panel_body", ModalKit.BorderPanel, sx, sy, sw, sh, true);
            sheet.pivot = new Vector2(0.5f, 0.5f);
            sheet.anchoredPosition = new Vector2(sx + sw * 0.5f, -(sy + sh * 0.5f));

            // 头部：当前镶嵌座预览 + 类型名称与完整效果说明 + 右上收起按钮
            string curSpr = filled ? "Ui/glyph_slot_" + QualityKey(cur) : "Ui/glyph_slot_empty_" + StageKey(tier);
            var curPreview = ModalKit.Pic(sheet, "cur_base", curSpr, 24f, 20f, 88f, 92f, true);
            var curIco = ModalKit.Pic(curPreview.rectTransform, "ico", null, 20f, 20f, 48f, 48f, true);
            curIco.sprite = SealArt.Icon(t);
            if (!filled) curIco.color = new Color(1f, 1f, 1f, 0.55f);

            ModalKit.SolidText(sheet, "h", SealCatalog.TypeName(t), 26, 126f, 20f, 320f, 34f,
                ModalKit.TextDark, TextAnchor.MiddleLeft, true);

            SealCatalog.Line(key, t, filled ? cur : SealQuality.Green, out string what, out string amount);
            string nowLine = filled
                ? SealCatalog.QualityName(cur) + "印章 · " + what + " " + Tint(amount, SealCatalog.QualityDeep(cur))
                : "空槽 · 镶上后" + what + "提高";
            var sub = ModalKit.SolidText(sheet, "k", nowLine, 18, 126f, 54f, 340f, 26f,
                ModalKit.TextBody, TextAnchor.MiddleLeft, true);
            sub.supportRichText = true;

            var eff = ModalKit.SolidText(sheet, "eff", SealCatalog.EffectOf(key, t) + "。品质越高，加得越多", 15,
                126f, 82f, 464f, 42f, ModalKit.TextMuted, TextAnchor.UpperLeft, false);
            eff.horizontalOverflow = HorizontalWrapMode.Wrap;

            ModalKit.HexBtn(sheet, "sm_gray", sw - 22f - 132f, 22f, "收起", () => Select(_sel), out _);

            // 四品质印章格
            const float cellW = 136f, cellH = 192f, gap = 12f;
            float rowLeft = (sw - (cellW * 4f + gap * 3f)) * 0.5f;
            const float rowY = 142f;
            int have = _meta.BagCount(t);
            string[] frameTones = { "green", "blue", "purple", "orange" };

            for (int qi = 0; qi < SealCatalog.QualityCount; qi++)
            {
                var q = (SealQuality)qi;
                int n = _meta.BagCount(t, q);
                bool on = filled && q == cur;
                bool any = on || n > 0;
                float cx = rowLeft + qi * (cellW + gap);

                string subKey = on ? "Ui/modal_sub_warm" : any ? "Ui/modal_sub_bright" : "Ui/modal_sub_cream";
                var cell = ModalKit.Sliced(sheet, "q" + qi, subKey, ModalKit.BorderSub16, cx, rowY, cellW, cellH, true);

                var slotRt = ModalKit.ItemSlot(cell, frameTones[qi], 86, (cellW - 86f) * 0.5f, 22f,
                    null, on ? null : "×" + n);
                var medal = ModalKit.Pic(slotRt, "m", "Ui/glyph_slot_" + QualityKey(q), 9f, 7f, 68f, 70f, true);
                var mIco = ModalKit.Pic(medal.rectTransform, "i", null, 16f, 16f, 36f, 36f, true);
                mIco.sprite = SealArt.Icon(t);
                if (!any)
                {
                    medal.color = new Color(1f, 1f, 1f, 0.38f);
                    mIco.color = new Color(1f, 1f, 1f, 0.38f);
                }

                SealCatalog.Line(key, t, q, out _, out string qAmount);
                ModalKit.SolidText(cell, "what", what, 14, 4f, 112f, cellW - 8f, 28f,
                    ModalKit.TextMuted, TextAnchor.MiddleCenter, false);
                ModalKit.SolidText(cell, "v", qAmount, 22, 4f, 142f, cellW - 8f, 36f,
                    any ? SealCatalog.QualityDeep(q) : InkTheme.TextDim, TextAnchor.MiddleCenter, true);

                if (on)
                    ModalKit.Tag(cell, "Ui/modal_tag_coral", (cellW - 76f) * 0.5f, -10f, 76f, 24f, "镶着", false, 13);

                SealQuality pick = q;
                var cellBtn = cell.gameObject.AddComponent<Button>();
                cellBtn.transition = Selectable.Transition.None;
                cellBtn.targetGraphic = cell.GetComponent<Image>();
                cellBtn.onClick.AddListener(() => Socket(pick));
            }

            if (have == 0 && !filled)
            {
                ModalKit.SolidText(sheet, "none", "还没有" + SealCatalog.TypeName(t) + "印章 · 开宝箱、每日副本可以获得",
                    16, 20f, sh - 66f, sw - 40f, 32f, ModalKit.TextMuted, TextAnchor.MiddleCenter, false);
            }
            else if (filled)
            {
                ModalKit.HexBtn(sheet, "md_cyan", (sw - 176f) * 0.5f, sh - 78f, "卸下", Unsocket, out _);
            }
        }

        // ---- 交互与动画 ----

        static void Hit(RectTransform node, float size, Action click)
        {
            var hitGo = new GameObject("hit", typeof(RectTransform), typeof(Image), typeof(Button));
            hitGo.transform.SetParent(node, false);
            var rt = hitGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size > 0f ? new Vector2(size, size) : node.sizeDelta;
            var img = hitGo.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.005f);
            img.raycastTarget = true;
            var btn = hitGo.GetComponent<Button>();
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
                _anim.Move(sheet, sheet.anchoredPosition + new Vector2(0f, -110f), sheet.anchoredPosition, 0f, 0.24f, Ease.OutBack);
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
            _sel = -1;
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
            if (q == SealQuality.Gold && _board.parent is RectTransform stg && stg.parent is RectTransform dim)
                UiConfetti.Burst(dim, 60);
        }

        void Unsocket()
        {
            if (!_meta.Unsocket(Cur, _sel)) return;
            _sel = -1;
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
            _sel = -1;
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

        RectTransform Reveal(int lv, int slotsWas)
        {
            Paint();
            Transform fx = _board.Find("study_up_fx");
            if (fx != null) fx.SetAsLastSibling();
            Transform bonus = _body.Find("bonus");
            if (bonus != null)
            {
                Transform num = bonus.Find(lv >= StudyCatalog.MaxLevel ? "v" : "a");
                if (num != null) _anim.Punch(num, 0.05f, 0.3f, 0.4f);
                if (bonus.Find("dot" + (lv - 1)) is RectTransform dot)
                {
                    dot.pivot = new Vector2(0.5f, 0.5f);
                    dot.anchoredPosition += new Vector2(dot.sizeDelta.x * 0.5f, -dot.sizeDelta.y * 0.5f);
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

        // ---- 配色映射 ----

        static string Tint(string s, Color c) => "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + s + "</color>";

        static float TextWidth(string s, float fontSize)
        {
            float w = 0f;
            for (int i = 0; i < s.Length; i++)
                w += s[i] > 255 ? fontSize : fontSize * 0.58f;
            return Mathf.Round(w);
        }

        static string StageKey(StudyCatalog.Tier t)
        {
            switch (t)
            {
                case StudyCatalog.Tier.Green: return "green";
                case StudyCatalog.Tier.Blue: return "blue";
                case StudyCatalog.Tier.Purple: return "purple";
                case StudyCatalog.Tier.Gold: return "gold";
                default: return "gray";
            }
        }

        static string SegKey(StudyCatalog.Tier t)
        {
            switch (t)
            {
                case StudyCatalog.Tier.Blue: return "blue";
                case StudyCatalog.Tier.Purple: return "purple";
                case StudyCatalog.Tier.Gold: return "gold";
                default: return "green";
            }
        }

        static string QualityKey(SealQuality q)
        {
            switch (q)
            {
                case SealQuality.Blue: return "blue";
                case SealQuality.Purple: return "purple";
                case SealQuality.Gold: return "gold";
                default: return "green";
            }
        }

        static Color FilledValueColor(SealQuality q)
        {
            switch (q)
            {
                case SealQuality.Blue: return InkTheme.Hex("8AD4FF");
                case SealQuality.Purple: return InkTheme.Hex("E2A8FF");
                case SealQuality.Gold: return InkTheme.Hex("FFE455");
                default: return InkTheme.Hex("A8E86A");
            }
        }

        static Color LvTextColor(StudyCatalog.Tier t)
        {
            switch (t)
            {
                case StudyCatalog.Tier.Green: return InkTheme.Hex("8ACB29");
                case StudyCatalog.Tier.Blue: return InkTheme.Hex("4BB3FD");
                case StudyCatalog.Tier.Purple: return InkTheme.Hex("C875FF");
                case StudyCatalog.Tier.Gold: return InkTheme.Hex("FFB82E");
                default: return InkTheme.Hex("9AA2AC");
            }
        }

        static string FamilyTagKey(CodexEntry e)
        {
            if (e.Kind == CodexKind.Word) return "orange";
            switch (GlyphTable.Get((CardId)e.Index).Family)
            {
                case GlyphFamily.Hurt: return "coral";
                case GlyphFamily.Status: return "magenta";
                case GlyphFamily.Move: return "orange";
                case GlyphFamily.Ballistic: return "brown";
                case GlyphFamily.Area: return "teal";
                default: return "gray";
            }
        }
    }
}

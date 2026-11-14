using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 七日签到（Figma Panel/首页弹窗/01_签到，预制体 Prefabs/CheckIn）：
    // 全屏暗场点击空白处关闭（无右上角红叉），顶部金环百宝箱 + 无框描边主标题 + 圆点暗条，
    // 前六天 3×2 对角异形圆角变体卡，第七天通栏异形宽卡，内部统一复用通用方形物品框与尖角主按钮。
    public sealed class CheckInPanel : MonoBehaviour
    {
        public const string PrefabPath = "Prefabs/CheckIn";
        public const string Mark = "check_in_v1";

        static readonly string[] DayNames =
        {
            "第一天", "第二天", "第三天", "第四天", "第五天", "第六天", "第七天"
        };

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _stage;
        RectTransform _body;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = ModalKit.SpawnPrefab(layer, PrefabPath, "check_in", Mark, null, BuildPrefab, out RectTransform stage);
            var panel = dim.gameObject.AddComponent<CheckInPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel._stage = stage;
            panel._body = stage.Find("body") as RectTransform;

            var bgBtn = dim.GetComponent<Button>() ?? dim.gameObject.AddComponent<Button>();
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.targetGraphic = dim.GetComponent<Image>();
            bgBtn.onClick.RemoveAllListeners();
            bgBtn.onClick.AddListener(panel.Close);

            panel.BindButtons();
            panel.Refresh();
            PanelKit.Open(stage);
        }

        public static RectTransform BuildPrefab()
        {
            var dim = ModalKit.Stage(null, "check_in", 0.85f, null, out RectTransform stage);
            ModalKit.AddMark(stage, Mark);

            var go = new GameObject("body", typeof(RectTransform));
            go.transform.SetParent(stage, false);
            var body = go.GetComponent<RectTransform>();
            ModalKit.TopLeft(body, 0f, 0f, ModalKit.DesignW, ModalKit.DesignH);

            var hero = ModalKit.Pic(body, "hero", "Ui/checkin_hero", 180f, 20f, 360f, 284f, true);
            hero.raycastTarget = true;
            ModalKit.SolidText(body, "title_sh", "签到", 34, 0f, 18f, ModalKit.DesignW, 48f,
                ModalKit.Ink, TextAnchor.MiddleCenter, true);
            ModalKit.OutlinedText(body, "title", "签到", 34, 0f, 15f, ModalKit.DesignW, 48f,
                ModalKit.TitleGold, TextAnchor.MiddleCenter, 1.6f);

            var infoBar = ModalKit.Pic(body, "info_bar", "Ui/checkin_info_bar", 162f, 278f, 396f, 40f, false);
            infoBar.raycastTarget = true;
            ModalKit.SolidText(infoBar.rectTransform, "sub",
                $"已连签 0 / {GameConstants.CheckDays} 天 · 断签从第 1 天重来",
                16, 28f, 0f, 340f, 40f, ModalKit.GoldLight, TextAnchor.MiddleCenter, true);

            const float cW = 208f, cH = 216f, gapX = 12f, gapY = 14f, startX = 36f, startY = 334f;
            for (int d = 1; d < GameConstants.CheckDays; d++)
            {
                int i = d - 1;
                float cx = startX + (i % 3) * (cW + gapX);
                float cy = startY + (i / 3) * (cH + gapY);
                BuildDayCardNode(body, d, cx, cy, cW, cH);
            }

            BuildDay7CardNode(body, 36f, 794f, 648f, 162f);

            const float btnY = 982f;
            var signBtn = ModalKit.HexBtn(body, "lg_cyan", 50f, btnY, "签到领取", null, out _);
            signBtn.name = "sign";
            var dblBtn = ModalKit.HexBtn(body, "lg_yellow", 374f, btnY, "", null, out _);
            dblBtn.name = "double";
            ModalKit.SetAdBtnLabel(dblBtn, "lg_yellow", "双倍签到", true);
            var dblTag = ModalKit.Tag(body, "Ui/modal_tag_coral", 374f + 168f, btnY - 11f, 86f, 26f, "奖励×2");
            dblTag.name = "dbl_tag";

            var bonusBtn = ModalKit.HexBtn(body, "lg_yellow", 212f, btnY, "", null, out _);
            bonusBtn.name = "bonus";
            ModalKit.SetAdBtnLabel(bonusBtn, "lg_yellow", "再领一份", true);
            bonusBtn.gameObject.SetActive(false);

            var doneBtn = ModalKit.HexBtn(body, "lg_gray", 212f, btnY, "今日已签 · 明天再来", null, out _);
            doneBtn.name = "done";
            doneBtn.gameObject.SetActive(false);

            ModalKit.SolidText(body, "close_tip", "点击空白处关闭", 16, 0f, 1108f, ModalKit.DesignW, 28f,
                InkTheme.Hex("AEA296"), TextAnchor.MiddleCenter, false);

            return dim;
        }

        static void BuildDayCardNode(RectTransform body, int day, float x, float y, float w, float h)
        {
            var glow = ModalKit.Pic(body, "glow" + day, "Ui/checkin_today_glow", x - 4f, y - 4f, 216f, 224f, false);
            glow.gameObject.SetActive(false);

            var cardImg = ModalKit.Pic(body, "day" + day, "Ui/checkin_card_day", x, y, w, h, false);
            cardImg.raycastTarget = true;
            var card = cardImg.rectTransform;

            string dayTitle = day >= 1 && day <= DayNames.Length ? DayNames[day - 1] : $"第 {day} 天";
            ModalKit.OutlinedText(card, "day", dayTitle, 19, 0f, 6f, w, 30f, Color.white, TextAnchor.MiddleCenter, 1.3f);

            int gems = MetaProgress.CheckDiamondOf(day);
            var s0 = ModalKit.ItemSlot(card, ModalKit.CurrencyTone, 74, 67f, 50f, ModalKit.Load("Ui/ico_diamond"), gems.ToString());
            s0.name = "s_gem";
            var s1 = ModalKit.ItemSlot(card, ModalKit.CurrencyTone, 74, 24f, 131f, ModalKit.Load("Ui/ico_stamina"),
                GameConstants.CheckStamina.ToString());
            s1.name = "s_stam";
            var s2 = ModalKit.ItemSlot(card, ModalKit.InkTone, 74, 110f, 131f, ModalKit.Load("Ui/ico_ink"),
                GameConstants.CheckInk.ToString());
            s2.name = "s_pay";

            var chk = ModalKit.Pic(body, "chk" + day, "Ui/checkin_check", x + w - 32f, y - 6f, 36f, 36f, true);
            chk.gameObject.SetActive(false);
            var rdy = ModalKit.Tag(body, "Ui/modal_tag_coral", x + w - 58f, y - 8f, 60f, 26f, "可签到");
            rdy.name = "rdy" + day;
            rdy.gameObject.SetActive(false);
        }

        static void BuildDay7CardNode(RectTransform body, float x, float y, float w, float h)
        {
            var cardImg = ModalKit.Pic(body, "day7", "Ui/checkin_card_day7", x, y, w, h, false);
            cardImg.raycastTarget = true;
            var card = cardImg.rectTransform;

            ModalKit.OutlinedText(card, "day", DayNames[6], 19, 0f, 6f, w, 30f, Color.white, TextAnchor.MiddleCenter, 1.3f);

            const float s = 86f, gap = 24f;
            float x0 = Mathf.Round((w - (5f * s + 4f * gap)) * 0.5f);

            var s0 = ModalKit.ItemSlot(card, ModalKit.CurrencyTone, 86, x0, 58f, ModalKit.Load("Ui/ico_stamina"),
                GameConstants.CheckStamina.ToString());
            s0.name = "s_stam";
            var s1 = ModalKit.ItemSlot(card, ModalKit.InkTone, 86, x0 + (s + gap), 58f,
                ModalKit.Load("Ui/ico_ink"), GameConstants.CheckInk.ToString());
            s1.name = "s_pay";
            var s2 = ModalKit.ItemSlot(card, ModalKit.CurrencyTone, 86, x0 + 2f * (s + gap), 58f, ModalKit.Load("Ui/ico_diamond"),
                MetaProgress.CheckDiamondOf(GameConstants.CheckDays).ToString());
            s2.name = "s_gem";

            float chestX = x0 + 3f * (s + gap);
            var s3 = ModalKit.ItemSlot(card, ModalKit.ChestTone(ChestTier.Gold), 86, chestX, 58f, ModalKit.Load("Ui/chest_gold"), "1");
            s3.name = "s_chest";
            var chestTag = ModalKit.Tag(card, "Ui/modal_tag_coral", chestX + s - 50f, 48f, 52f, 22f, "金箱", false, 13);
            chestTag.name = "chest_tag";

            float skinX = x0 + 4f * (s + gap);
            SkinDef skinDef = SkinCatalog.Get(GameConstants.CheckSkin);
            var s4 = ModalKit.ItemSlot(card, ModalKit.SkinTone(skinDef.Rarity), 86, skinX, 58f,
                ModalKit.Load("Ui/ico_skin_" + skinDef.Key), "1");
            s4.name = "s_skin";
            var skinTag = ModalKit.Tag(card, "Ui/modal_tag_gold", skinX + s - 58f, 48f, 62f, 22f, "首周送", true, 13);
            skinTag.name = "skin_tag";

            var chk = ModalKit.Pic(body, "chk7", "Ui/checkin_check", x + w - 32f, y - 6f, 36f, 36f, true);
            chk.gameObject.SetActive(false);
            var rdy = ModalKit.Tag(body, "Ui/modal_tag_coral", x + w - 58f, y - 8f, 60f, 26f, "可签到");
            rdy.name = "rdy7";
            rdy.gameObject.SetActive(false);
        }

        void BindButtons()
        {
            ModalKit.BindBtn(_body, "sign", () => Sign(false));
            ModalKit.BindBtn(_body, "double", () => Sign(true));
            ModalKit.BindBtn(_body, "bonus", Bonus);
            ModalKit.BindBtn(_body, "done", () => AudioBus.Tap());
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Refresh()
        {
            if (_body == null) return;
            int shown = _meta.CheckShown;
            bool today = _meta.CheckedToday;
            int next = today ? -1 : shown + 1;

            var sub = _body.Find("info_bar/sub")?.GetComponent<Text>();
            if (sub != null)
                sub.text = $"已连签 {shown} / {GameConstants.CheckDays} 天 · 断签从第 1 天重来";

            Sprite paySpr = ModalKit.Load(PayIcon());
            for (int d = 1; d < GameConstants.CheckDays; d++)
            {
                bool signed = d <= shown;
                bool isNext = d == next;

                var glow = _body.Find("glow" + d);
                if (glow != null) glow.gameObject.SetActive(isNext);

                var cardTr = _body.Find("day" + d);
                if (cardTr != null)
                {
                    var cardImg = cardTr.GetComponent<Image>();
                    if (cardImg != null)
                    {
                        string cardKey = signed ? "Ui/checkin_card_signed"
                            : isNext ? "Ui/checkin_card_today"
                            : "Ui/checkin_card_day";
                        cardImg.sprite = ModalKit.Load(cardKey);
                    }
                    var paySlot = cardTr.Find("s_pay") as RectTransform;
                    ModalKit.SetItemSlot(paySlot, ModalKit.CurrencyFrame(PayIcon()), 74, paySpr, PayAmount(d).ToString());
                }

                var chk = _body.Find("chk" + d);
                if (chk != null) chk.gameObject.SetActive(signed);
                var rdy = _body.Find("rdy" + d);
                if (rdy != null) rdy.gameObject.SetActive(!signed && isNext);
            }

            int last = GameConstants.CheckDays;
            bool signed7 = last <= shown;
            bool next7 = last == next;
            var card7 = _body.Find("day7") as RectTransform;
            if (card7 != null)
            {
                var img7 = card7.GetComponent<Image>();
                if (img7 != null) img7.color = signed7 ? new Color(0.86f, 0.84f, 0.80f, 1f) : Color.white;

                bool hasSkin = _meta.CheckSkinPending;
                int count = hasSkin ? 5 : 4;
                const float s = 86f, gap = 24f;
                float x0 = Mathf.Round((648f - (count * s + (count - 1) * gap)) * 0.5f);

                PlaceSlot(card7, "s_stam", x0, 58f, s);
                var pay7 = PlaceSlot(card7, "s_pay", x0 + (s + gap), 58f, s);
                ModalKit.SetItemSlot(pay7, ModalKit.CurrencyFrame(PayIcon()), 86, paySpr, PayAmount(last).ToString());
                PlaceSlot(card7, "s_gem", x0 + 2f * (s + gap), 58f, s);

                float chestX = x0 + 3f * (s + gap);
                PlaceSlot(card7, "s_chest", chestX, 58f, s);
                var chestTag = card7.Find("chest_tag") as RectTransform;
                if (chestTag != null) ModalKit.TopLeft(chestTag, chestX + s - 50f, 48f, 52f, 22f);

                var skinSlot = card7.Find("s_skin") as RectTransform;
                var skinTag = card7.Find("skin_tag") as RectTransform;
                if (skinSlot != null)
                {
                    skinSlot.gameObject.SetActive(hasSkin);
                    if (hasSkin)
                    {
                        float skinX = x0 + 4f * (s + gap);
                        ModalKit.TopLeft(skinSlot, skinX, 58f, s, s);
                        SkinDef skinDef = SkinCatalog.Get(GameConstants.CheckSkin);
                        ModalKit.SetItemSlot(skinSlot, ModalKit.SkinTone(skinDef.Rarity), 86,
                            InkSprites.Ui("skin_" + skinDef.Key), "1");
                        if (skinTag != null) ModalKit.TopLeft(skinTag, skinX + s - 58f, 48f, 62f, 22f);
                    }
                }
                if (skinTag != null) skinTag.gameObject.SetActive(hasSkin);
            }

            var chk7 = _body.Find("chk7");
            if (chk7 != null) chk7.gameObject.SetActive(signed7);
            var rdy7 = _body.Find("rdy7");
            if (rdy7 != null) rdy7.gameObject.SetActive(!signed7 && next7);

            bool showSign = !today;
            bool showBonus = today && !_meta.CheckAdDoneToday;
            bool showDone = today && _meta.CheckAdDoneToday;
            SetActive(_body, "sign", showSign);
            SetActive(_body, "double", showSign);
            SetActive(_body, "dbl_tag", showSign);
            SetActive(_body, "bonus", showBonus);
            SetActive(_body, "done", showDone);
        }

        static RectTransform PlaceSlot(Transform parent, string name, float x, float y, float s)
        {
            var rt = parent.Find(name) as RectTransform;
            if (rt != null) ModalKit.TopLeft(rt, x, y, s, s);
            return rt;
        }

        static void SetActive(Transform parent, string name, bool on)
        {
            var tr = parent != null ? parent.Find(name) : null;
            if (tr != null) tr.gameObject.SetActive(on);
        }

        string PayIcon() => _meta.StudyOpen ? "Ui/ico_foil" : "Ui/ico_ink";

        int PayAmount(int day) => _meta.StudyOpen ? StudyCatalog.CheckFoilOf(day) : GameConstants.CheckInk;

        void Sign(bool doubled)
        {
            AudioBus.Tap();
            if (!doubled) { Finish(false); return; }
            AdStub.Reward("checkin_double", () =>
            {
                if (this == null) return;
                Finish(true);
            });
        }

        void Finish(bool doubled)
        {
            int day = _meta.CheckIn(doubled, out bool skin, out int chest);
            if (day <= 0) return;
            int k = doubled ? 2 : 1;
            int foil = _meta.StudyOpen ? StudyCatalog.CheckFoilOf(day) * k : 0;
            int ink = _meta.StudyOpen ? 0 : GameConstants.CheckInk * k;
            string msg = skin
                ? $"连签 {GameConstants.CheckDays} 天，{SkinCatalog.Get(GameConstants.CheckSkin).Name}皮肤已换上"
                : $"签到成功 {(foil > 0 ? "金箔×" + foil : "灵墨×" + ink)} 钻石×{MetaProgress.CheckDiamondOf(day) * k}";
            if (chest >= 0) msg += "  金宝箱已放进宝箱位";
            else if (chest == -1) msg += "  宝箱位满了，选个位置放下";
            else if (chest == -3) msg += "  宝箱位满了，金宝箱换成灵墨";
            Transform card = _body != null ? _body.Find(day == GameConstants.CheckDays ? "day7" : "day" + day) : null;
            Vector2 from = RewardFly.Local(_layer, card);
            if (chest == -3) ink += ChestCatalog.InkAvg(ChestTier.Gold);
            var pieces = new List<RewardFly.Piece>(5)
            {
                RewardFly.Stamina(_layer, GameConstants.CheckStamina * k),
                RewardFly.Diamond(_layer, MetaProgress.CheckDiamondOf(day) * k)
            };
            if (foil > 0) pieces.Insert(1, RewardFly.Foil(_layer, foil));
            if (ink > 0) pieces.Insert(1, RewardFly.Ink(_layer, ink));
            if (chest >= 0) pieces.Add(RewardFly.Chest(_layer, ChestTier.Gold, chest));
            if (skin) pieces.Add(RewardFly.Skin(_layer, GameConstants.CheckSkin));
            InkToast.Show(_layer, msg);
            RewardFly.Play(_layer, from, pieces.ToArray());
            _changed?.Invoke();
            Refresh();
            if (chest == -1) ChestOverflowView.Show(_layer, _meta, _changed);
        }

        void Bonus()
        {
            AudioBus.Tap();
            AdStub.Reward("checkin_bonus", () =>
            {
                if (this == null || !_meta.CheckAdBonus()) return;
                int foil = _meta.StudyOpen ? StudyCatalog.CheckFoilOf(_meta.CheckRun) : 0;
                string pay = foil > 0 ? "金箔×" + foil : "灵墨×" + GameConstants.CheckInk;
                string msg = $"额外领取 {pay} 钻石×{MetaProgress.CheckDiamondOf(_meta.CheckRun)}";
                Transform btn = _body != null ? _body.Find("bonus") : null;
                Vector2 from = RewardFly.Local(_layer, btn);
                InkToast.Show(_layer, msg);
                var pieces = new List<RewardFly.Piece>
                {
                    RewardFly.Stamina(_layer, GameConstants.CheckStamina),
                    RewardFly.Diamond(_layer, MetaProgress.CheckDiamondOf(_meta.CheckRun))
                };
                pieces.Insert(1, foil > 0 ? RewardFly.Foil(_layer, foil) : RewardFly.Ink(_layer, GameConstants.CheckInk));
                RewardFly.Play(_layer, from, pieces.ToArray());
                _changed?.Invoke();
                Refresh();
            });
        }
    }
}

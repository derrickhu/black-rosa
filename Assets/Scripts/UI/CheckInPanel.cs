using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 七日签到，照花花的 3+3+1：前六天两排小卡，第七天一张宽卡。
    // 每天体力 + 墨 + 钻石，签的时候看广告翻倍；已经直接签了的，当天还能补看一次再领一份。
    // 第 7 天另送金宝箱，第一次连签满 7 天还送机甲炮，断一天从第 1 天重来。
    public sealed class CheckInPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 880f;
        const float GridW = 580f;
        const float Gap = 14f;
        const float CardH = 176f;
        const float Day7H = 150f;
        const float GridTop = 112f;

        static readonly Color TodayFill = InkTheme.Hex("FFF6E6");
        static readonly Color DoneFill = InkTheme.Hex("EFE6D6");

        MetaProgress _meta;
        Action _changed;
        RectTransform _layer;
        RectTransform _board;
        RectTransform _body;

        public static void Show(RectTransform layer, MetaProgress meta, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "check_in";
            var panel = dim.gameObject.AddComponent<CheckInPanel>();
            panel._meta = meta;
            panel._changed = changed;
            panel._layer = layer;
            panel._board = PanelKit.Board(dim, "七日签到", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, panel.Close);
            panel.Refresh();
        }

        void Close()
        {
            AudioBus.Tap();
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

            int shown = _meta.CheckShown;
            bool today = _meta.CheckedToday;
            int next = today ? -1 : shown + 1;

            var sub = UiKit.Label(_body, "sub", $"已连续签到 {shown} 天 · 断签从第 1 天重来", 24,
                new Vector2(0f, 72f), new Vector2(BoardW - 60f, 32f), TextAnchor.MiddleCenter, Pin.Top);
            sub.color = InkTheme.TextMid;

            float cw = (GridW - Gap * 2f) / 3f;
            for (int d = 1; d < GameConstants.CheckDays; d++)
            {
                int i = d - 1;
                float x = (i % 3 - 1) * (cw + Gap);
                float y = GridTop + (i / 3) * (CardH + Gap);
                DayCard(d, new Vector2(x, y), new Vector2(cw, CardH), d <= shown, d == next, MetaProgress.CheckDiamondOf(d));
            }
            int last = GameConstants.CheckDays;
            Day7Card(new Vector2(0f, GridTop + 2f * (CardH + Gap)), new Vector2(GridW, Day7H), last <= shown, last == next,
                MetaProgress.CheckDiamondOf(last));

            var tip = UiKit.Label(_body, "tip", $"每天 体力×{GameConstants.CheckStamina}  墨×{GameConstants.CheckInk}  还有钻石 · 看广告翻倍", 22,
                new Vector2(0f, 176f), new Vector2(BoardW - 60f, 30f), TextAnchor.MiddleCenter, Pin.Bottom);
            tip.color = InkTheme.TextMid;

            BuildButtons(today);
        }

        void BuildButtons(bool today)
        {
            var size = new Vector2(256f, 104f);
            if (!today)
            {
                UiKit.Btn(_body, "sign", "签到", new Vector2(-138f, 52f), size, () => Sign(false), false, Pin.Bottom);
                UiKit.Btn(_body, "double", "看广告双倍", new Vector2(138f, 52f), size, () => Sign(true), true, Pin.Bottom);
                return;
            }
            if (!_meta.CheckAdDoneToday)
            {
                UiKit.Btn(_body, "bonus", "看广告再领一份", new Vector2(0f, 52f), new Vector2(430f, 104f), Bonus, true, Pin.Bottom);
                return;
            }
            var done = UiKit.Btn(_body, "done", "今日已签到，明天再来", new Vector2(0f, 52f), new Vector2(430f, 104f),
                () => AudioBus.Tap(), true, Pin.Bottom);
            PanelKit.Dim(done, true);
        }

        RectTransform Card(string name, Vector2 pos, Vector2 size, bool signed, bool isNext)
        {
            Color fill = signed ? DoneFill : (isNext ? TodayFill : InkTheme.CardFace);
            Color line = isNext ? InkTheme.Seal : InkTheme.Outline;
            var card = UiKit.Stroke(_body, name, pos, size, Pin.Top, isNext ? 5f : 3f, line, fill, 20f);
            card.GetComponent<Image>().raycastTarget = false;
            return card;
        }

        void DayCard(int day, Vector2 pos, Vector2 size, bool signed, bool isNext, int gems)
        {
            var card = Card("day" + day, pos, size, signed, isNext);
            var t = UiKit.Label(card, "day", $"第 {day} 天", 24, new Vector2(0f, size.y * 0.5f - 26f), new Vector2(size.x, 32f));
            t.color = isNext ? InkTheme.Seal : InkTheme.TextDark;
            UiKit.Bold(t);
            Pair(card, "Ui/ico_stamina", "×" + GameConstants.CheckStamina, new Vector2(0f, 22f));
            Pair(card, "Ui/ico_ink", "×" + GameConstants.CheckInk, new Vector2(0f, -18f));
            Pair(card, "Ui/ico_diamond", "×" + gems, new Vector2(0f, -58f));
            if (signed) Stamp(card, Vector2.zero);
        }

        void Day7Card(Vector2 pos, Vector2 size, bool signed, bool isNext, int gems)
        {
            var card = Card("day7", pos, size, signed, isNext);
            float left = -size.x * 0.5f;
            var t = UiKit.Label(card, "day", $"第 {GameConstants.CheckDays} 天", 26, new Vector2(left + 90f, 0f), new Vector2(150f, 36f));
            t.color = isNext ? InkTheme.Seal : InkTheme.TextDark;
            UiKit.Bold(t);
            float rowX = left + 230f;
            Pair(card, "Ui/ico_stamina", "×" + GameConstants.CheckStamina, new Vector2(rowX, 40f));
            Pair(card, "Ui/ico_ink", "×" + GameConstants.CheckInk, new Vector2(rowX, 0f));
            Pair(card, "Ui/ico_diamond", "×" + gems, new Vector2(rowX, -40f));
            UiKit.Icon(card, InkSprites.Load("Ui/chest_gold"), new Vector2(46f, 12f), 86f);
            var cn = UiKit.Label(card, "chest", ChestCatalog.Get(ChestTier.Gold).Name, 20, new Vector2(46f, -50f), new Vector2(110f, 28f));
            cn.color = InkTheme.TextDark;
            UiKit.Bold(cn);
            if (_meta.CheckSkinPending)
            {
                UiKit.Icon(card, InkSprites.Ui("skin_" + SkinCatalog.Get(GameConstants.CheckSkin).Key),
                    new Vector2(148f, 4f), 96f);
                var n = UiKit.Label(card, "skin", SkinCatalog.Get(GameConstants.CheckSkin).Name + "皮肤", 22,
                    new Vector2(236f, 18f), new Vector2(110f, 32f));
                n.color = InkTheme.Seal;
                UiKit.Bold(n);
                var hint = UiKit.Label(card, "hint", "首次连签送", 18, new Vector2(236f, -18f), new Vector2(110f, 28f));
                hint.color = InkTheme.TextMid;
            }
            if (signed) Stamp(card, new Vector2(size.x * 0.5f - 90f, 0f));
        }

        static void Pair(Transform card, string icon, string amount, Vector2 pos)
        {
            UiKit.Icon(card, InkSprites.Load(icon), pos + new Vector2(-30f, 0f), 46f);
            var a = UiKit.Label(card, "n", amount, 26, pos + new Vector2(26f, 0f), new Vector2(70f, 36f), TextAnchor.MiddleLeft);
            a.color = InkTheme.TextDark;
            UiKit.Bold(a);
        }

        // 「已签」朱印，斜压在卡上。
        static void Stamp(Transform card, Vector2 pos)
        {
            var ring = UiKit.Icon(card, UiSprites.Disc(), pos, 88f);
            ring.color = InkTheme.Seal;
            ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            var face = UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 78f);
            face.color = InkTheme.CardFace;
            var inner = UiKit.Icon(ring.transform, UiSprites.Disc(), Vector2.zero, 70f);
            inner.color = InkTheme.Seal;
            var t = UiKit.Label(ring.transform, "t", "已签", 26, new Vector2(0f, 1f), new Vector2(80f, 36f));
            t.color = InkTheme.CardFace;
            UiKit.Bold(t);
        }

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
            string msg = skin
                ? $"连签 {GameConstants.CheckDays} 天，{SkinCatalog.Get(GameConstants.CheckSkin).Name}皮肤已换上"
                : $"签到成功 墨×{GameConstants.CheckInk * k} 钻石×{MetaProgress.CheckDiamondOf(day) * k}";
            if (chest >= 0) msg += "  金宝箱已放进宝箱位";
            else if (chest == -1) msg += "  宝箱位满了，金宝箱折成墨";
            Transform card = _body != null ? _body.Find(day == GameConstants.CheckDays ? "day7" : "day" + day) : null;
            Vector2 from = RewardFly.Local(_layer, card);
            int ink = GameConstants.CheckInk * k;
            if (chest == -1) ink += ChestCatalog.InkAvg(ChestTier.Gold);
            var pieces = new System.Collections.Generic.List<RewardFly.Piece>(5)
            {
                RewardFly.Stamina(_layer, GameConstants.CheckStamina * k),
                RewardFly.Ink(_layer, ink),
                RewardFly.Diamond(_layer, MetaProgress.CheckDiamondOf(day) * k)
            };
            if (chest >= 0) pieces.Add(RewardFly.Chest(_layer, ChestTier.Gold, chest));
            if (skin) pieces.Add(RewardFly.Skin(_layer, GameConstants.CheckSkin));
            InkToast.Show(_layer, msg);
            RewardFly.Play(_layer, from, pieces.ToArray());
            _changed?.Invoke();
            Refresh();
        }

        void Bonus()
        {
            AudioBus.Tap();
            AdStub.Reward("checkin_bonus", () =>
            {
                if (this == null || !_meta.CheckAdBonus()) return;
                string msg = $"额外领取 墨×{GameConstants.CheckInk} 钻石×{MetaProgress.CheckDiamondOf(_meta.CheckRun)}";
                Transform btn = _body != null ? _body.Find("bonus") : null;
                Vector2 from = RewardFly.Local(_layer, btn);
                InkToast.Show(_layer, msg);
                RewardFly.Play(_layer, from, new[]
                {
                    RewardFly.Stamina(_layer, GameConstants.CheckStamina),
                    RewardFly.Ink(_layer, GameConstants.CheckInk),
                    RewardFly.Diamond(_layer, MetaProgress.CheckDiamondOf(_meta.CheckRun))
                });
                _changed?.Invoke();
                Refresh();
            });
        }
    }
}

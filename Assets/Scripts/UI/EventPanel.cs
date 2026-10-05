using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 「招财进宝」活动页：聚宝盆进度、里程碑领取、三档活动关、当天次数。
    // 内容少，领一次奖、看一次广告就整块重画。
    public sealed class EventPanel : MonoBehaviour
    {
        const float BoardW = 660f;
        const float BoardH = 1160f;
        const float Top = BoardH * 0.5f;

        MetaProgress _meta;
        Action<int> _start;
        Action _changed;
        RectTransform _body;

        public static void Show(RectTransform layer, MetaProgress meta, Action<int> start, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "event_panel";
            var panel = dim.gameObject.AddComponent<EventPanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            var board = PanelKit.Board(dim, EventCatalog.Title, Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, panel.Close);
            panel._body = ResultKit.Group(board, "body", Vector2.zero, new Vector2(BoardW, BoardH));
            panel.Refresh();
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        void Refresh()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);
            BuildHead();
            BuildMilestones();
            BuildTiers();
            BuildPlays();
        }

        static float Y(float fromTop) => Top - fromTop;

        void BuildHead()
        {
            bool done = _meta.EventDone;
            var date = UiKit.Label(_body, "date",
                done ? "奖励已全部领取，再打不会获得任何奖励" : $"常驻活动 · 每天 {EventCatalog.PlaysPerDay} 次", 22,
                new Vector2(0f, Y(122f)), new Vector2(BoardW - 40f, 30f));
            date.color = done ? InkTheme.Seal : InkTheme.TextMid;

            UiKit.Icon(_body, InkSprites.Ui("skin_lucky"), new Vector2(-200f, Y(222f)), 136f);
            var title = UiKit.Label(_body, "hero", $"聚宝盆攒满 {EventCatalog.SkinTarget} 金", 30,
                new Vector2(70f, Y(196f)), new Vector2(400f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(title);
            var sub = UiKit.Label(_body, "herosub", "福袋炮台到手：金币拾取 +10%", 22,
                new Vector2(70f, Y(240f)), new Vector2(400f, 30f), TextAnchor.MiddleLeft);
            sub.color = InkTheme.Seal;
            var rule = UiKit.Label(_body, "rule", "活动关不掉墨。打赢后钱袋里剩多少金币，就存多少；输了或撤退不存", 19,
                new Vector2(0f, Y(312f)), new Vector2(BoardW - 70f, 56f));
            rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            rule.color = InkTheme.TextMid;

            int max = EventCatalog.Milestones[EventCatalog.Milestones.Length - 1].Need;
            var bank = UiKit.Label(_body, "bank", $"聚宝盆 {_meta.EventBank}", 28,
                new Vector2(0f, Y(370f)), new Vector2(BoardW - 40f, 36f));
            bank.color = InkTheme.CoinDeep;
            UiKit.Bold(bank);
            var fill = ResultKit.Bar(_body, new Vector2(0f, Y(410f)), 580f, 28f, InkTheme.Gold);
            ResultKit.SetBar(fill, Mathf.Min(_meta.EventBank, max) / (float)max);
        }

        void BuildMilestones()
        {
            const float W = 96f, H = 140f, Gap = 6f;
            int n = EventCatalog.Milestones.Length;
            for (int i = 0; i < n; i++)
            {
                EventMilestone m = EventCatalog.Milestones[i];
                float x = (i - (n - 1) * 0.5f) * (W + Gap);
                bool can = _meta.EventClaimable(i);
                bool got = _meta.EventClaimed(i);
                bool skin = m.Prize == EventPrize.Skin;
                Color face = can ? InkTheme.Hex("FFF6E2") : skin ? InkTheme.Hex("FFE9D6") : InkTheme.CardFace;
                var card = UiKit.Stroke(_body, "ms" + i, new Vector2(x, Y(510f)), new Vector2(W, H), Pin.Center, 4f,
                    can ? InkTheme.Seal : (Color?)null, face, 18f);
                UiKit.Icon(card, InkSprites.Load(EventCatalog.PrizeIcon(m)), new Vector2(0f, 30f), 58f);
                var cnt = UiKit.Label(card, "n", EventCatalog.PrizeCount(m), 18, new Vector2(0f, -10f), new Vector2(W, 24f));
                cnt.color = InkTheme.TextMid;
                var need = UiKit.Label(card, "need", got ? "已领" : can ? "领取" : m.Need.ToString(), 20,
                    new Vector2(0f, -44f), new Vector2(W, 28f));
                UiKit.Bold(need);
                need.color = got ? InkTheme.TextMid : can ? InkTheme.Seal : InkTheme.TextDark;
                if (got) card.GetComponent<Image>().color = InkTheme.Hex("EDE6DA");
                int idx = i;
                var btn = card.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Claim(idx));
            }
        }

        static RewardFly.Piece[] PrizePieces(RectTransform layer, EventMilestone m, bool refunded, int chest)
        {
            if (refunded) return new[] { RewardFly.Token(layer, EventCatalog.SkinTokenRefund) };
            switch (m.Prize)
            {
                case EventPrize.Ink: return new[] { RewardFly.Ink(layer, m.Amount) };
                case EventPrize.Token: return new[] { RewardFly.Token(layer, m.Amount) };
                case EventPrize.Chest:
                    if (chest < 0) return new[] { RewardFly.Ink(layer, ChestCatalog.InkAvg((ChestTier)m.Amount)) };
                    return new[] { RewardFly.Chest(layer, (ChestTier)m.Amount, chest) };
                default: return new[] { RewardFly.Skin(layer, m.Amount) };
            }
        }

        void Claim(int i)
        {
            EventMilestone m = EventCatalog.Milestones[i];
            if (!_meta.EventClaimable(i))
            {
                AudioBus.Tap();
                string what = EventCatalog.PrizeName(m);
                InkToast.Show(transform.parent, _meta.EventClaimed(i) ? $"{what}已经领过了" : $"聚宝盆到 {m.Need} 可领{what}");
                return;
            }
            Transform card = _body != null ? _body.Find("ms" + i) : null;
            var layer = transform.parent as RectTransform;
            Vector2 from = RewardFly.Local(layer, card);
            if (!_meta.ClaimEvent(i, out bool refunded, out int chest)) return;
            AudioBus.CountTick();
            string got = refunded ? $"已有福袋炮台，折成活动币 ×{EventCatalog.SkinTokenRefund}"
                : $"领到 {EventCatalog.PrizeName(m)} {EventCatalog.PrizeCount(m)}";
            InkToast.Show(layer, got);
            RewardFly.Play(layer, from, PrizePieces(layer, m, refunded, chest));
            _changed?.Invoke();
            Refresh();
        }

        void BuildTiers()
        {
            var head = UiKit.Label(_body, "tierhead", "选一处开集", 26, new Vector2(0f, Y(616f)), new Vector2(BoardW - 40f, 34f));
            UiKit.Bold(head);
            const float RowW = 600f, RowH = 112f;
            for (int t = 0; t < EventCatalog.TierCount; t++)
            {
                EventTierDef d = EventCatalog.Tier(t);
                bool open = _meta.EventTierOpen(t);
                float y = Y(696f + t * (RowH + 14f));
                var row = UiKit.Stroke(_body, "tier" + t, new Vector2(0f, y), new Vector2(RowW, RowH), Pin.Center, 4f,
                    null, open ? InkTheme.CardFace : InkTheme.Hex("EDE6DA"), 22f);
                var name = UiKit.Label(row, "name", d.Name, 32, new Vector2(-150f, 18f), new Vector2(260f, 40f), TextAnchor.MiddleLeft);
                UiKit.Bold(name);
                var brief = UiKit.Label(row, "brief", d.Brief, 20, new Vector2(-150f, -24f), new Vector2(260f, 30f), TextAnchor.MiddleLeft);
                brief.color = InkTheme.TextMid;
                if (open)
                {
                    int tier = t;
                    var go = UiKit.Btn(row, "go", "开打", new Vector2(196f, 0f), new Vector2(170f, 78f), () => Go(tier));
                    PanelKit.Dim(go, _meta.EventPlaysLeft <= 0);
                }
                else
                {
                    var lockText = UiKit.Label(row, "lock", $"通关第 {d.GateChapter + 1} 章开放", 22,
                        new Vector2(180f, 0f), new Vector2(220f, 30f));
                    lockText.color = InkTheme.TextMid;
                }
            }
        }

        void Go(int tier)
        {
            AudioBus.Tap();
            if (_meta.EventPlaysLeft <= 0)
            {
                InkToast.Show(transform.parent, _meta.EventAdDone ? "今天的次数用完了，明天再来" : "今天的次数用完了，看广告再开 2 局");
                return;
            }
            if (_meta.EventDone)
            {
                ConfirmDone(tier);
                return;
            }
            Launch(tier);
        }

        void Launch(int tier)
        {
            Destroy(gameObject);
            _start?.Invoke(tier);
        }

        // 奖励全领完还要打：先说清楚这局什么都拿不到，点了「照样开打」才进。
        void ConfirmDone(int tier)
        {
            var dim = UiKit.Dimmer(transform);
            var board = PanelKit.Board(dim, "提示", Vector2.zero, new Vector2(520f, 420f), Pin.Center,
                () => Destroy(dim.gameObject));
            var tip = UiKit.Label(board, "tip", "聚宝盆的奖励已全部领取\n再打不会获得任何奖励", 26,
                new Vector2(0f, 20f), new Vector2(460f, 90f));
            tip.color = InkTheme.TextDark;
            UiKit.Btn(board, "cancel", "不打了", new Vector2(-110f, 56f), new Vector2(190f, 80f), () =>
            {
                AudioBus.Tap();
                Destroy(dim.gameObject);
            }, false, Pin.Bottom);
            UiKit.Btn(board, "go", "照样开打", new Vector2(110f, 56f), new Vector2(190f, 80f), () =>
            {
                AudioBus.Tap();
                Launch(tier);
            }, true, Pin.Bottom);
        }

        void BuildPlays()
        {
            int cap = EventCatalog.PlaysPerDay + (_meta.EventAdDone ? EventCatalog.AdPlays : 0);
            var plays = UiKit.Label(_body, "plays", $"今日次数 {_meta.EventPlaysLeft}/{cap}", 26,
                new Vector2(_meta.EventAdDone ? 0f : -150f, -Top + 70f), new Vector2(260f, 36f));
            UiKit.Bold(plays);
            if (_meta.EventAdDone) return;
            var ad = UiKit.Btn(_body, "ad", $"+{EventCatalog.AdPlays} 次", new Vector2(150f, -Top + 70f),
                new Vector2(220f, 80f), OnAd);
            ResultKit.AdMark(ad, 34f);
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("event_plays", () =>
            {
                if (this == null || !_meta.GrantEventAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }
    }
}

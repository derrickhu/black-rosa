using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    // 「招财进宝」活动页：聚宝盆进度、里程碑领取、庙会小地图、当天次数。
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
        bool _leaving;

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
            BuildMap();
            BuildPlays();
        }

        static float Y(float fromTop) => Top - fromTop;

        void BuildHead()
        {
            bool done = _meta.EventDone;
            var date = UiKit.Label(_body, "date",
                done ? "奖励已全部领取，再打不会获得任何奖励" : $"常驻活动 · 每天 {EventCatalog.PlaysPerDay} 次", 20,
                new Vector2(0f, Y(108f)), new Vector2(BoardW - 40f, 28f));
            date.color = done ? InkTheme.Seal : InkTheme.TextMid;

            UiKit.Icon(_body, InkSprites.Ui("skin_lucky"), new Vector2(-214f, Y(176f)), 92f);
            ResultKit.CoinAfter(_body, "hero", "聚宝盆攒满 " + EventCatalog.SkinTarget, 28,
                new Vector2(-142f, Y(158f)), InkTheme.TextDark, false);
            var sub = UiKit.Label(_body, "herosub", "福袋炮台到手：金币拾取 +10%", 20,
                new Vector2(58f, Y(192f)), new Vector2(400f, 28f), TextAnchor.MiddleLeft);
            sub.color = InkTheme.Seal;
            var rule = UiKit.Label(_body, "rule", "活动关不掉墨。赢了钱袋全存进聚宝盆；输了看广告可存回一半", 18,
                new Vector2(0f, Y(248f)), new Vector2(BoardW - 80f, 40f));
            rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            rule.color = InkTheme.TextMid;

            int max = EventCatalog.Milestones[EventCatalog.Milestones.Length - 1].Need;
            ResultKit.CoinAfter(_body, "bank", "聚宝盆 " + _meta.EventBank, 26,
                new Vector2(0f, Y(284f)), InkTheme.CoinDeep, true);
            var fill = ResultKit.Bar(_body, new Vector2(0f, Y(316f)), 560f, 22f, InkTheme.Gold);
            ResultKit.SetBar(fill, Mathf.Min(_meta.EventBank, max) / (float)max);
        }

        void BuildMilestones()
        {
            const float W = 90f, H = 104f, Gap = 6f;
            int n = EventCatalog.Milestones.Length;
            for (int i = 0; i < n; i++)
            {
                EventMilestone m = EventCatalog.Milestones[i];
                float x = (i - (n - 1) * 0.5f) * (W + Gap);
                bool can = _meta.EventClaimable(i);
                bool got = _meta.EventClaimed(i);
                bool skin = m.Prize == EventPrize.Skin;
                Color face = can ? InkTheme.Hex("FFF6E2") : skin ? InkTheme.Hex("FFE9D6") : InkTheme.CardFace;
                var card = UiKit.Stroke(_body, "ms" + i, new Vector2(x, Y(392f)), new Vector2(W, H), Pin.Center, 4f,
                    can ? InkTheme.Seal : (Color?)null, face, 16f);
                UiKit.Icon(card, InkSprites.Load(EventCatalog.PrizeIcon(m)), new Vector2(0f, 20f), 46f);
                var cnt = UiKit.Label(card, "n", EventCatalog.PrizeCount(m), 16, new Vector2(0f, -8f), new Vector2(W, 22f));
                cnt.color = InkTheme.TextMid;
                if (got || can)
                {
                    var need = UiKit.Label(card, "need", got ? "已领" : "领取", 18,
                        new Vector2(0f, -32f), new Vector2(W, 24f));
                    UiKit.Bold(need);
                    need.color = got ? InkTheme.TextMid : InkTheme.Seal;
                }
                else
                    ResultKit.CoinAfter(card, "need", m.Need.ToString(), 16, new Vector2(0f, -34f), InkTheme.TextDark, true);
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
                    if (chest == -3) return new[] { RewardFly.Ink(layer, ChestCatalog.InkAvg((ChestTier)m.Amount)) };
                    if (chest < 0) return new RewardFly.Piece[0];
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
                InkToast.Show(transform.parent, _meta.EventClaimed(i) ? $"{what}已经领过了" : $"聚宝盆攒到 {m.Need} 金币可领{what}");
                return;
            }
            Transform card = _body != null ? _body.Find("ms" + i) : null;
            var layer = transform.parent as RectTransform;
            Vector2 from = RewardFly.Local(layer, card);
            if (!_meta.ClaimEvent(i, out bool refunded, out int chest)) return;
            AudioBus.CountTick();
            string got = refunded ? $"已有福袋炮台，折成活动币 ×{EventCatalog.SkinTokenRefund}"
                : chest == -1 ? "宝箱位满了，选个位置放下"
                : chest == -3 ? $"宝箱位满了，换成 {ChestCatalog.InkAvg((ChestTier)m.Amount)} 墨"
                : $"领到 {EventCatalog.PrizeName(m)} {EventCatalog.PrizeCount(m)}";
            InkToast.Show(layer, got);
            RewardFly.Play(layer, from, PrizePieces(layer, m, refunded, chest));
            _changed?.Invoke();
            Refresh();
            if (chest == -1) ChestOverflowView.Show(layer, _meta, _changed);
        }

        static readonly string[] SpotArt = { "Ui/event_stall", "Ui/event_temple", "Ui/event_fair" };

        // 空地图铺底，三处集市是单独的图，名字写在图下面。位置按地面三个圆台。
        void BuildMap()
        {
            const float MapW = 600f, MapH = 450f;
            var map = UiKit.Art(_body, "map", "Ui/event_map", new Vector2(0f, Y(710f)), new Vector2(MapW, MapH));
            PlaceSpot(map, 0, new Vector2(-190f, -117f));
            PlaceSpot(map, 1, new Vector2(0f, 0f));
            PlaceSpot(map, 2, new Vector2(180f, 132f));
        }

        void PlaceSpot(RectTransform map, int t, Vector2 pos)
        {
            EventTierDef d = EventCatalog.Tier(t);
            bool open = _meta.EventTierOpen(t);
            bool won = _meta.EventTierWon(t);
            const float Icon = 128f;
            const float NameDrop = Icon * 0.5f + 22f;
            // 图心对准圆台，名字写在图下面。
            var hit = UiKit.Panel(map, "spot" + t, pos + new Vector2(0f, -NameDrop * 0.5f),
                new Vector2(Icon + 24f, Icon + NameDrop + 24f), new Color(0f, 0f, 0f, 0f));
            var btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int tier = t;
            btn.onClick.AddListener(() => PressGo(hit, tier));
            if (open)
            {
                var trigger = hit.gameObject.AddComponent<EventTrigger>();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener(_ => hit.localScale = new Vector3(0.92f, 0.92f, 1f));
                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener(_ => { if (!_leaving) hit.localScale = Vector3.one; });
                trigger.triggers.Add(down);
                trigger.triggers.Add(up);
            }

            Vector2 iconPos = new Vector2(0f, NameDrop * 0.5f);
            var iconGo = new GameObject("art", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(hit, false);
            var ir = iconGo.GetComponent<RectTransform>();
            ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.anchoredPosition = iconPos;
            ir.sizeDelta = new Vector2(Icon, Icon);
            var img = iconGo.GetComponent<Image>();
            img.sprite = InkSprites.Load(SpotArt[t]);
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = open ? Color.white : new Color(0.62f, 0.62f, 0.64f, 1f);
            if (open)
                UiAnim.On(this).Breathe(iconGo.transform, 0.15f * t, 0.045f, 1.15f);
            else
            {
                var badge = UiKit.Stroke(hit, "lockbg", iconPos, new Vector2(56f, 56f), Pin.Center, 3f,
                    InkTheme.Hex("FFF4E2"), new Color(0.24f, 0.17f, 0.12f, 0.82f), 28f);
                UiKit.Icon(badge, InkSprites.Ui("lock"), Vector2.zero, 36f);
            }

            var name = UiKit.Label(hit, "name", d.Name, 32, iconPos + new Vector2(0f, -Icon * 0.5f - 24f), new Vector2(140f, 42f));
            UiKit.Bold(name);
            name.color = open ? (won ? InkTheme.CoinDeep : InkTheme.TextDark) : InkTheme.Hex("5C5148");
            var edge = name.gameObject.AddComponent<Outline>();
            edge.effectColor = InkTheme.Hex("FFF8EC");
            edge.effectDistance = new Vector2(2f, -2f);

            var graphics = hit.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i].gameObject != hit.gameObject) graphics[i].raycastTarget = false;
        }

        void PressGo(RectTransform spot, int tier)
        {
            if (_leaving) return;
            if (!_meta.EventTierOpen(tier))
            {
                AudioBus.Deny();
                InkToast.Show(transform.parent, "先打下上一处");
                return;
            }
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
            StartCoroutine(PopGo(spot, tier));
        }

        IEnumerator PopGo(RectTransform spot, int tier)
        {
            _leaving = true;
            float t = 0f;
            while (t < 0.12f && spot != null)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + 0.14f * Mathf.Sin(Mathf.Clamp01(t / 0.12f) * Mathf.PI);
                spot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (this != null) Launch(tier);
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

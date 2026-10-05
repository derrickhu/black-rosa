using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class EventResultArgs
    {
        public bool Win;
        public bool Done;
        public int Banked;
        public int Purse;
        public int SalvageGold;
        public int Interest;
        public string Unlocked;
        public bool CanDouble;
        public Action Double;
        public Action Salvage;
        public Action Again;
        public Action Back;
    }

    // 招财进宝的结算，和主线那张分开：中间是这局金币，下面是离下一个奖励还差多少。
    // 赢了可以看广告把存入翻倍；输了可以看广告，把钱袋里的一半存进聚宝盆。
    public sealed class EventResultPanel : MonoBehaviour
    {
        const float BoardW = 640f;

        MetaProgress _meta;
        EventResultArgs _a;
        float _boardH;
        ResultKit.CoinLine _amount;
        ResultKit.CoinLine _gap;
        ResultKit.CoinLine _interest;
        Text _cap;
        Text _prizeName;
        Image _prizeIcon;
        Image _heroCoin;
        RectTransform _fill;
        Button _double;
        Button _salvage;

        public RectTransform Root => (RectTransform)transform;

        public static EventResultPanel Show(RectTransform layer, MetaProgress meta, EventResultArgs a)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "event_result";
            var p = dim.gameObject.AddComponent<EventResultPanel>();
            p._meta = meta;
            p._a = a;
            p.Build(dim);
            return p;
        }

        float Y(float fromTop) => _boardH * 0.5f - fromTop;

        void Build(RectTransform dim)
        {
            bool save = !_a.Win && !_a.Done && _a.SalvageGold > 0 && _a.Salvage != null;
            _boardH = _a.Done ? 700f : _a.Win ? 1000f : save ? 1140f : 840f;
            string title = _a.Done ? (_a.Win ? "守住了" : "没守住") : _a.Win ? "存入聚宝盆" : "钱袋没保住";
            var board = PanelKit.Board(dim, title, Vector2.zero, new Vector2(BoardW, _boardH), Pin.Center,
                () => _a.Back?.Invoke());

            if (_a.Done)
            {
                var tip = UiKit.Label(board, "tip", "聚宝盆的奖励已全部领取\n本局不会获得任何奖励", 24,
                    new Vector2(0f, Y(240f)), new Vector2(BoardW - 80f, 80f));
                tip.color = InkTheme.TextMid;
            }
            else if (_a.Win)
                BuildWin(board);
            else
                BuildLoss(board, save);

            if (!_a.Done) BuildGoal(board, _a.Win ? 600f : save ? 820f : 530f);

            if (_a.Win && !_a.Done && _a.CanDouble && _a.Banked > 0 && _a.Double != null)
            {
                _double = UiKit.Btn(board, "double", "存入翻倍", new Vector2(0f, 170f), new Vector2(320f, 84f),
                    () => _a.Double(), true, Pin.Bottom);
                ResultKit.AdMark(_double, 36f);
            }

            int left = _meta.EventPlaysLeft;
            var again = UiKit.Btn(board, "again", $"再来一局（剩 {left}）", new Vector2(-136f, 64f), new Vector2(248f, 84f),
                () => _a.Again?.Invoke(), _double == null && _salvage == null, Pin.Bottom);
            if (left <= 0) PanelKit.Dim(again, true);
            UiKit.Btn(board, "back", "回活动", new Vector2(136f, 64f), new Vector2(220f, 84f),
                () => _a.Back?.Invoke(), false, Pin.Bottom);
        }

        void BuildWin(RectTransform board)
        {
            ResultKit.Rays(board, new Vector2(0f, Y(188f)), 300f, new Color(1f, 0.84f, 0.42f, 0.75f));
            _heroCoin = UiKit.Icon(board, InkSprites.Ui("gold"), new Vector2(0f, Y(188f)), 120f);
            _cap = UiKit.Label(board, "cap", "本局存入", 24, new Vector2(0f, Y(268f)), new Vector2(BoardW, 32f));
            _cap.color = InkTheme.TextMid;
            _amount = ResultKit.CoinAfter(board, "amount", "+" + _a.Banked, 58,
                new Vector2(0f, Y(328f)), InkTheme.CoinDeep, true);
            float note = 396f;
            if (_a.Interest > 0)
            {
                _interest = ResultKit.CoinAfter(board, "interest", "其中利息 +" + _a.Interest, 22,
                    new Vector2(0f, Y(note)), InkTheme.TextMid, true);
                note += 36f;
            }
            if (!string.IsNullOrEmpty(_a.Unlocked))
            {
                var open = UiKit.Label(board, "open", _a.Unlocked, 22, new Vector2(0f, Y(note)), new Vector2(BoardW - 40f, 30f));
                open.color = InkTheme.CoinDeep;
                UiKit.Bold(open);
            }
        }

        void BuildLoss(RectTransform board, bool save)
        {
            _heroCoin = UiKit.Icon(board, InkSprites.Ui("gold"), new Vector2(0f, Y(176f)), 108f);
            _heroCoin.color = new Color(0.62f, 0.62f, 0.64f, 1f);
            _cap = UiKit.Label(board, "cap", "本局没存上", 24, new Vector2(0f, Y(252f)), new Vector2(BoardW, 32f));
            _cap.color = InkTheme.Seal;
            _amount = ResultKit.CoinAfter(board, "amount", _a.Purse.ToString(), 52,
                new Vector2(0f, Y(312f)), InkTheme.TextMid, true);
            if (!save) return;

            var card = UiKit.Stroke(board, "save", new Vector2(0f, Y(500f)), new Vector2(560f, 220f),
                Pin.Center, 4f, InkTheme.Seal, InkTheme.Hex("FFF3D6"), 22f);
            card.GetComponent<Image>().raycastTarget = false;
            var hint = UiKit.Label(card, "hint", "看广告可以存回", 22, new Vector2(0f, 62f), new Vector2(500f, 30f));
            hint.color = InkTheme.TextMid;
            ResultKit.CoinAfter(card, "back", _a.SalvageGold.ToString(), 40, new Vector2(0f, 12f), InkTheme.CoinDeep, true);
            _salvage = UiKit.Btn(card, "ad", "看广告存回", new Vector2(0f, -62f), new Vector2(320f, 80f),
                () => _a.Salvage(), true);
            ResultKit.AdMark(_salvage, 34f);
        }

        void BuildGoal(RectTransform board, float fromTop)
        {
            var card = UiKit.Stroke(board, "goal", new Vector2(0f, Y(fromTop)), new Vector2(580f, 200f),
                Pin.Center, 4f, null, InkTheme.Hex("FFF8EC"), 22f);
            card.GetComponent<Image>().raycastTarget = false;
            var head = UiKit.Label(card, "h", "下一个奖励", 18, new Vector2(-2f, 64f), new Vector2(360f, 26f), TextAnchor.MiddleLeft);
            head.color = InkTheme.TextMid;
            _prizeIcon = UiKit.Icon(card, InkSprites.Ui("gold"), new Vector2(-236f, 6f), 72f);
            _prizeName = UiKit.Label(card, "name", "", 28, new Vector2(-2f, 18f), new Vector2(360f, 40f), TextAnchor.MiddleLeft);
            UiKit.Bold(_prizeName);
            _gap = ResultKit.CoinAfter(card, "gap", "还差 0", 22, new Vector2(-182f, -26f), InkTheme.Seal, false);
            _fill = ResultKit.Bar(card, new Vector2(18f, -70f), 400f, 20f, InkTheme.Gold);
            RefreshGoal();
        }

        void RefreshGoal()
        {
            int bank = _meta.EventBank;
            EventMilestone[] list = EventCatalog.Milestones;
            for (int i = 0; i < list.Length; i++)
            {
                if (!_meta.EventClaimable(i)) continue;
                ShowPrize(list[i], 1f, "可以领" + EventCatalog.PrizeName(list[i]) + "了", false);
                return;
            }
            for (int i = 0; i < list.Length; i++)
            {
                if (bank >= list[i].Need) continue;
                int prev = i == 0 ? 0 : list[i - 1].Need;
                float span = Mathf.Max(1, list[i].Need - prev);
                ShowPrize(list[i], (bank - prev) / span, EventCatalog.PrizeName(list[i]), true);
                _gap.Set("还差 " + (list[i].Need - bank));
                return;
            }
            _prizeIcon.gameObject.SetActive(false);
            _prizeName.text = "奖励全部到手";
            _gap.Set("", false);
            ResultKit.SetBar(_fill, 1f);
        }

        void ShowPrize(EventMilestone m, float bar, string name, bool gap)
        {
            _prizeIcon.gameObject.SetActive(true);
            _prizeIcon.sprite = InkSprites.Load(EventCatalog.PrizeIcon(m));
            _prizeName.text = name;
            _prizeName.color = gap ? InkTheme.TextDark : InkTheme.Seal;
            if (!gap) _gap.Set("", false);
            ResultKit.SetBar(_fill, bar);
        }

        public void Doubled()
        {
            _amount.Set("+" + _a.Banked * 2);
            if (_double != null)
            {
                PanelKit.SetText(_double, "已翻倍");
                PanelKit.Dim(_double, true);
                _double.interactable = false;
            }
            AudioBus.CountTick();
            RefreshGoal();
        }

        public void Salvaged()
        {
            if (_cap != null)
            {
                _cap.text = "已存进聚宝盆";
                _cap.color = InkTheme.CoinDeep;
            }
            if (_heroCoin != null) _heroCoin.color = Color.white;
            _amount.Set(_a.SalvageGold.ToString());
            _amount.Text.color = InkTheme.CoinDeep;
            if (_salvage != null)
            {
                PanelKit.SetText(_salvage, "已存回");
                PanelKit.Dim(_salvage, true);
                _salvage.interactable = false;
            }
            AudioBus.CountTick();
            RefreshGoal();
        }
    }
}

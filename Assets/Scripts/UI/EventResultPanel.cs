using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class EventResultArgs
    {
        public bool Win;
        // 里程碑全领完了，这局什么都不存。
        public bool Done;
        public int Banked;
        public int Interest;
        public bool CanDouble;
        public Action Double;
        public Action Again;
        public Action Back;
    }

    // 招财进宝的结算：赢了报存进聚宝盆多少、离下一档奖还差多少；输了只说没存上。
    public sealed class EventResultPanel : MonoBehaviour
    {
        const float BoardW = 600f;
        const float BoardH = 720f;
        const float Top = BoardH * 0.5f;

        MetaProgress _meta;
        EventResultArgs _a;
        Text _amount;
        Text _bank;
        Text _next;
        RectTransform _fill;
        Button _double;

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

        static float Y(float fromTop) => Top - fromTop;

        void Build(RectTransform dim)
        {
            var board = PanelKit.Board(dim, EventCatalog.Title, Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center,
                () => _a.Back?.Invoke());
            if (_a.Done)
            {
                var head = UiKit.Label(board, "head", _a.Win ? "守住了" : "没守住", 40, new Vector2(0f, Y(196f)), new Vector2(BoardW, 52f));
                UiKit.Bold(head);
                head.color = _a.Win ? InkTheme.CoinDeep : InkTheme.Seal;
                var tip = UiKit.Label(board, "tip", "聚宝盆的奖励已全部领取\n本局不会获得任何奖励", 24,
                    new Vector2(0f, Y(290f)), new Vector2(BoardW - 60f, 80f));
                tip.color = InkTheme.TextMid;
            }
            else if (_a.Win)
            {
                UiKit.Icon(board, InkSprites.Ui("gold"), new Vector2(0f, Y(170f)), 104f);
                var head = UiKit.Label(board, "head", "存入聚宝盆", 28, new Vector2(0f, Y(250f)), new Vector2(BoardW, 36f));
                head.color = InkTheme.TextMid;
                _amount = UiKit.Label(board, "amount", "+" + _a.Banked, 60, new Vector2(0f, Y(312f)), new Vector2(BoardW, 76f));
                UiKit.Bold(_amount);
                _amount.color = InkTheme.CoinDeep;
                var inter = UiKit.Label(board, "interest", _a.Interest > 0 ? $"其中利息 +{_a.Interest}" : "", 22,
                    new Vector2(0f, Y(366f)), new Vector2(BoardW, 30f));
                inter.color = InkTheme.TextMid;
            }
            else
            {
                var head = UiKit.Label(board, "head", "钱袋没保住", 40, new Vector2(0f, Y(196f)), new Vector2(BoardW, 52f));
                UiKit.Bold(head);
                head.color = InkTheme.Seal;
                var tip = UiKit.Label(board, "tip", "输了不存金币\n换一处低档，或回去强化炮台", 24,
                    new Vector2(0f, Y(290f)), new Vector2(BoardW - 60f, 80f));
                tip.color = InkTheme.TextMid;
            }

            _bank = UiKit.Label(board, "bank", "", 26, new Vector2(0f, Y(420f)), new Vector2(BoardW - 40f, 34f));
            UiKit.Bold(_bank);
            _fill = ResultKit.Bar(board, new Vector2(0f, Y(458f)), 500f, 26f, InkTheme.Gold);
            _next = UiKit.Label(board, "next", "", 22, new Vector2(0f, Y(498f)), new Vector2(BoardW - 40f, 30f));
            _next.color = InkTheme.Seal;
            ShowBank();

            if (_a.Win && !_a.Done && _a.CanDouble && _a.Banked > 0 && _a.Double != null)
            {
                _double = UiKit.Btn(board, "double", "存入翻倍", new Vector2(0f, 178f), new Vector2(300f, 84f),
                    () => _a.Double(), true, Pin.Bottom);
                ResultKit.AdMark(_double, 36f);
            }

            int left = _meta.EventPlaysLeft;
            var again = UiKit.Btn(board, "again", $"再来一局（剩 {left}）", new Vector2(-136f, 56f), new Vector2(248f, 84f),
                () => _a.Again?.Invoke(), _double == null, Pin.Bottom);
            if (left <= 0) PanelKit.Dim(again, true);
            UiKit.Btn(board, "back", "回活动", new Vector2(136f, 56f), new Vector2(220f, 84f),
                () => _a.Back?.Invoke(), false, Pin.Bottom);
        }

        void ShowBank()
        {
            int bank = _meta.EventBank;
            _bank.text = $"聚宝盆 {bank}";
            EventMilestone[] list = EventCatalog.Milestones;
            int max = list[list.Length - 1].Need;
            ResultKit.SetBar(_fill, Mathf.Min(bank, max) / (float)max);
            for (int i = 0; i < list.Length; i++)
            {
                if (_meta.EventClaimable(i))
                {
                    _next.text = $"可以领{EventCatalog.PrizeName(list[i])}了，回活动页领取";
                    return;
                }
            }
            for (int i = 0; i < list.Length; i++)
            {
                if (bank >= list[i].Need) continue;
                _next.text = $"再存 {list[i].Need - bank} 金，领{EventCatalog.PrizeName(list[i])}";
                return;
            }
            _next.text = "聚宝盆已满，奖励全部到手";
        }

        // 广告看完，GameFlow 已经把同样多的金币再存一次。
        public void Doubled()
        {
            if (_amount != null) _amount.text = $"+{_a.Banked * 2}";
            if (_double != null)
            {
                PanelKit.SetText(_double, "已翻倍");
                PanelKit.Dim(_double, true);
                _double.interactable = false;
            }
            AudioBus.CountTick();
            ShowBank();
        }
    }
}

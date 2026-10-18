using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class EndlessResultArgs
    {
        public bool Win;
        public bool Daily;      // 每日残局：纪录、次数、宝箱档都按当天算
        public int Total;       // 这局一共几波
        public EndlessResult Result;
        public Action Double;
        public Action Again;
        public Action Rank;
        public Action Home;
    }

    // 无尽的结算：大字写撑到第几波，刷新纪录盖一枚「新纪录」，下面是这局入账的墨和宝箱。
    // 墨已经入账，翻倍广告再加一份同样多的。
    public sealed class EndlessResultPanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 1000f;

        MetaProgress _meta;
        EndlessResultArgs _a;
        UiAnim _anim;
        Text _ink;
        Button _double;

        public RectTransform Root => (RectTransform)transform;

        public static EndlessResultPanel Show(RectTransform layer, MetaProgress meta, EndlessResultArgs a)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "endless_result";
            var p = dim.gameObject.AddComponent<EndlessResultPanel>();
            p._meta = meta;
            p._a = a;
            p._anim = UiAnim.On(p);
            p.Build(dim);
            return p;
        }

        static float Y(float fromTop) => BoardH * 0.5f - fromTop;

        void Build(RectTransform dim)
        {
            EndlessResult r = _a.Result;
            string title = _a.Win ? "全部守住" : _a.Daily ? "残局结算" : "无尽结算";
            var board = PanelKit.Board(dim, title, Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center,
                () => _a.Home?.Invoke());

            int total = _a.Total > 0 ? _a.Total : StageCatalog.EndlessWaves;
            var cap = UiKit.Label(board, "cap", _a.Win ? $"{total} 波全部打完" : "这一局撑到", 26,
                new Vector2(0f, Y(120f)), new Vector2(BoardW, 34f));
            cap.color = InkTheme.TextMid;

            if (r.NewBest)
                ResultKit.Rays(board, new Vector2(0f, Y(210f)), 340f, new Color(1f, 0.84f, 0.42f, 0.75f));
            var big = ResultKit.Headline(board, "waves", "第 0 波", 84, new Vector2(0f, Y(210f)),
                InkTheme.Hex("F6BE3C"), InkTheme.Outline, 4f);
            _anim.CountUp(big, 0, r.Waves, 0.2f, 0.8f, "第 {0} 波", AudioBus.CountTick)
                 .Punch(big.transform, 1.0f, 0.14f, 0.3f);

            string span = _a.Daily ? "今日" : "本周";
            string bestLine = r.NewBest
                ? (r.OldBest > 0 ? $"{span}新纪录！之前最好第 {r.OldBest} 波" : span + "新纪录！")
                : $"{span}最好 第 {Mathf.Max(r.OldBest, r.Waves)} 波";
            var best = UiKit.Label(board, "best", bestLine, 26, new Vector2(0f, Y(300f)), new Vector2(BoardW, 34f));
            UiKit.Bold(best);
            best.color = r.NewBest ? InkTheme.Seal : InkTheme.TextMid;

            BuildLoot(board, r);

            if (_a.Rank != null)
                UiKit.Btn(board, "rank", "看排行榜", new Vector2(0f, Y(700f)), new Vector2(320f, 76f),
                    () => { AudioBus.Tap(); _a.Rank(); }, false);

            if (_a.Double != null)
            {
                _double = UiKit.Btn(board, "double", "墨翻倍", new Vector2(0f, 170f), new Vector2(320f, 84f),
                    () => _a.Double(), true, Pin.Bottom);
                ResultKit.AdMark(_double, 36f);
            }

            int left = _a.Daily ? _meta.DailyPlaysLeft : _meta.EndlessPlaysLeft;
            var again = UiKit.Btn(board, "again", $"再来一局（剩 {left}）", new Vector2(-136f, 64f), new Vector2(248f, 84f),
                () => _a.Again?.Invoke(), _double == null, Pin.Bottom);
            if (left <= 0) PanelKit.Dim(again, true);
            UiKit.Btn(board, "home", "回主页", new Vector2(136f, 64f), new Vector2(220f, 84f),
                () => { AudioBus.Tap(); _a.Home?.Invoke(); }, false, Pin.Bottom);
        }

        void BuildLoot(RectTransform board, EndlessResult r)
        {
            var card = UiKit.Stroke(board, "loot", new Vector2(0f, Y(470f)), new Vector2(560f, 220f),
                Pin.Center, 4f, null, InkTheme.Hex("FFF8EC"), 22f);
            card.GetComponent<Image>().raycastTarget = false;
            var head = UiKit.Label(card, "h", "本局收获", 20, new Vector2(0f, 82f), new Vector2(500f, 28f));
            head.color = InkTheme.TextMid;

            bool chest = r.Chest >= 0;
            float inkX = chest ? -130f : 0f;
            UiKit.Icon(card, InkSprites.Ui("ink"), new Vector2(inkX, 16f), 78f);
            _ink = UiKit.Label(card, "ink", "墨 +" + r.Ink, 34, new Vector2(inkX, -52f), new Vector2(260f, 44f));
            UiKit.Bold(_ink);
            _ink.color = InkTheme.TextDark;

            if (chest)
            {
                ChestDef def = ChestCatalog.Get(r.Chest);
                UiKit.Icon(card, InkSprites.Load("Ui/chest_" + def.Key), new Vector2(130f, 16f), 86f);
                string where = r.ChestSlot == -1 ? "位满了，先寄着" : r.ChestSlot == -3 ? "放不下，折成墨了" : "已放进宝箱位";
                var n = UiKit.Label(card, "chest", def.Name + " · " + where, 20, new Vector2(130f, -52f), new Vector2(280f, 28f));
                n.color = InkTheme.TextDark;
            }
            else
            {
                int next = _a.Daily ? StageCatalog.DailyBossEvery : StageCatalog.EndlessBossEvery;
                var tip = UiKit.Label(card, "tip", $"打到第 {next} 波起，结算会多给宝箱", 20,
                    new Vector2(0f, -88f), new Vector2(500f, 28f));
                tip.color = InkTheme.TextMid;
            }
        }

        public void Doubled()
        {
            if (_ink != null) _ink.text = "墨 +" + _a.Result.Ink * 2;
            if (_double != null)
            {
                PanelKit.SetText(_double, "已翻倍");
                PanelKit.Dim(_double, true);
                _double.interactable = false;
            }
            AudioBus.CountTick();
        }
    }
}

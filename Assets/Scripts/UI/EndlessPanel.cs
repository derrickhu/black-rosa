using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 无尽模式的开局页：规则、最好成绩、到档宝箱、当天次数，点「开始」才扣次数开打。
    // 看广告加次数之后整块重画。
    public sealed class EndlessPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 900f;
        const float Top = BoardH * 0.5f;

        MetaProgress _meta;
        Action _start;
        Action _changed;
        RectTransform _layer;
        RectTransform _body;

        public static void Show(RectTransform layer, MetaProgress meta, Action start, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "endless_panel";
            var panel = dim.gameObject.AddComponent<EndlessPanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            var board = PanelKit.Board(dim, "无尽模式", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, panel.Close);
            panel._body = ResultKit.Group(board, "body", Vector2.zero, new Vector2(BoardW, BoardH));
            panel.Refresh();
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        static float Y(float fromTop) => Top - fromTop;

        void Refresh()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);

            var sub = UiKit.Label(_body, "sub", $"一共 {StageCatalog.EndlessWaves} 波，越往后越硬，看你能撑到第几波", 22,
                new Vector2(0f, Y(104f)), new Vector2(BoardW - 60f, 32f));
            sub.color = InkTheme.TextMid;

            var bestCap = UiKit.Label(_body, "bestcap", "最好成绩", 24, new Vector2(0f, Y(170f)), new Vector2(300f, 32f));
            bestCap.color = InkTheme.TextMid;
            string best = _meta.EndlessBest > 0 ? $"第 {_meta.EndlessBest} 波" : "还没打过";
            var bestT = ResultKit.Headline(_body, "best", best, 56, new Vector2(0f, Y(232f)),
                InkTheme.Hex("F6BE3C"), InkTheme.Outline, 4f);
            bestT.rectTransform.sizeDelta = new Vector2(BoardW - 40f, 84f);

            BuildChests();

            var rule = UiKit.Label(_body, "rule",
                $"开局一排格子，第 10 波起每 3 波加一格 · 每 {StageCatalog.EndlessBossEvery} 波一只关底\n金币不够花，福袋的金币收益在这里翻倍 · 只能续命一次", 20,
                new Vector2(0f, Y(560f)), new Vector2(BoardW - 60f, 64f));
            rule.horizontalOverflow = HorizontalWrapMode.Wrap;
            rule.color = InkTheme.TextMid;

            BuildPlays();

            int left = _meta.EndlessPlaysLeft;
            var go = UiKit.Btn(_body, "go", "开始", new Vector2(0f, Y(800f)), new Vector2(360f, 104f), OnStart, true);
            if (left <= 0) PanelKit.Dim(go, true);
        }

        // 到第 10 / 20 / 30 / 40 波各升一档宝箱。
        void BuildChests()
        {
            var head = UiKit.Label(_body, "chesthead", "打到这一波，结算多给一只宝箱", 20,
                new Vector2(0f, Y(300f)), new Vector2(BoardW - 60f, 28f));
            head.color = InkTheme.TextMid;
            const float W = 128f, H = 160f, Gap = 10f;
            int n = (int)ChestTier.Royal + 1;
            for (int i = 0; i < n; i++)
            {
                int need = (i + 1) * StageCatalog.EndlessBossEvery;
                bool got = _meta.EndlessBest >= need;
                float x = (i - (n - 1) * 0.5f) * (W + Gap);
                var card = UiKit.Stroke(_body, "c" + i, new Vector2(x, Y(410f)), new Vector2(W, H), Pin.Center, 4f,
                    got ? InkTheme.Seal : (Color?)null, got ? InkTheme.Hex("FFF6E2") : InkTheme.CardFace, 18f);
                card.GetComponent<Image>().raycastTarget = false;
                ChestDef def = ChestCatalog.Get((ChestTier)i);
                UiKit.Icon(card, InkSprites.Load("Ui/chest_" + def.Key), new Vector2(0f, 22f), 78f);
                var name = UiKit.Label(card, "name", def.Name, 18, new Vector2(0f, -36f), new Vector2(W, 24f));
                name.color = InkTheme.TextMid;
                string label = i == n - 1 ? $"第 {need} 波起" : $"第 {need} 波";
                var at = UiKit.Label(card, "at", label, 20, new Vector2(0f, -60f), new Vector2(W, 26f));
                UiKit.Bold(at);
                at.color = got ? InkTheme.Seal : InkTheme.TextDark;
            }
        }

        void BuildPlays()
        {
            int cap = _meta.EndlessPlaysCap;
            bool ad = !_meta.EndlessAdDone;
            var plays = UiKit.Label(_body, "plays", $"今日次数 {_meta.EndlessPlaysLeft}/{cap}", 26,
                new Vector2(ad ? -130f : 0f, Y(670f)), new Vector2(260f, 36f));
            UiKit.Bold(plays);
            if (!ad) return;
            var btn = UiKit.Btn(_body, "ad", $"+{GameConstants.EndlessAdPlays} 次", new Vector2(150f, Y(670f)),
                new Vector2(220f, 80f), OnAd);
            ResultKit.AdMark(btn, 34f);
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("endless_plays", () =>
            {
                if (this == null || !_meta.GrantEndlessAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }

        void OnStart()
        {
            if (_meta.EndlessPlaysLeft <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.EndlessAdDone
                    ? "今天的次数用完了，明天再来"
                    : $"今天的次数用完了，看广告再开 {GameConstants.EndlessAdPlays} 局");
                return;
            }
            AudioBus.Tap();
            Action start = _start;
            Destroy(gameObject);
            start?.Invoke();
        }
    }
}

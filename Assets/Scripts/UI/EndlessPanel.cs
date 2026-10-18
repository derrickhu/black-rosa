using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 无尽模式的开局页：恶魔横幅、最好成绩、到档宝箱、当天次数，点「开始」才扣次数开打。
    // 加格、关底、福袋和续命不写在这里，留给玩家自己撞上。
    // 入口贴纸是紫恶魔，整页跟着走紫色。看广告加次数之后整块重画。
    public sealed class EndlessPanel : MonoBehaviour
    {
        const float BoardW = 620f;
        const float BoardH = 800f;
        const float Top = BoardH * 0.5f;

        static readonly Color Grape = InkTheme.Hex("7B3FB8");
        static readonly Color GrapeDeep = InkTheme.Hex("4E2380");
        static readonly Color Night = InkTheme.Hex("3A2352");
        static readonly Color Lilac = InkTheme.Hex("D9C6F2");
        static readonly Color LilacFace = InkTheme.Hex("F4ECFB");
        static readonly Color Gold = InkTheme.Hex("F6BE3C");

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
            var board = PanelKit.Board(dim, "无尽模式", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, panel.Close,
                "Ui/ribbon_endless", 440f, -0.02f);
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

            BuildHero();
            BuildChests();
            BuildPlays();

            int left = _meta.EndlessPlaysLeft;
            var go = UiKit.Btn(_body, "go", "开始", new Vector2(0f, Y(720f)), new Vector2(360f, 104f), OnStart, true);
            if (left <= 0) PanelKit.Dim(go, true);
            else
            {
                UiKit.PaintBtn(go, Grape, GrapeDeep, InkTheme.CardFace);
                UiAnim.On(go).Breathe(go.transform, 0.3f, 0.035f, 1.1f);
            }
        }

        // 夜紫底的成绩卡，左边一只晃着的紫恶魔，右边最好成绩和总波数。
        void BuildHero()
        {
            var card = UiKit.Stroke(_body, "hero", new Vector2(0f, Y(206f)), new Vector2(BoardW - 56f, 196f), Pin.Center, 5f,
                null, Night, 26f);
            card.GetComponent<Image>().raycastTarget = false;

            var demon = UiKit.Icon(card, InkSprites.Load("Ui/ico_act_endless"), new Vector2(-170f, 4f), 168f);
            demon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            UiAnim.On(demon).Breathe(demon.transform, 0f, 0.05f, 0.7f);

            var cap = UiKit.Label(card, "cap", "最好成绩", 22, new Vector2(80f, 58f), new Vector2(300f, 30f));
            cap.color = Lilac;
            string best = _meta.EndlessBest > 0 ? $"第 {_meta.EndlessBest} 波" : "还没打过";
            var bestT = ResultKit.Headline(card, "best", best, 60, new Vector2(80f, 2f), Gold, InkTheme.Outline, 4f);
            bestT.rectTransform.sizeDelta = new Vector2(330f, 86f);

            var pill = UiKit.Stroke(card, "total", new Vector2(80f, -60f), new Vector2(170f, 38f), Pin.Center, 3f,
                null, GrapeDeep, 19f);
            pill.GetComponent<Image>().raycastTarget = false;
            var total = UiKit.Label(pill, "t", $"共 {StageCatalog.EndlessWaves} 波", 20, Vector2.zero, new Vector2(170f, 38f));
            total.color = Lilac;
            UiKit.Bold(total);
        }

        // 到第 10 / 20 / 30 / 40 波各升一档宝箱。拿到的盖红，下一档描紫。
        void BuildChests()
        {
            var head = UiKit.Label(_body, "chesthead", "冲 波 奖 励", 22, new Vector2(0f, Y(334f)), new Vector2(300f, 30f));
            UiKit.Bold(head);
            head.color = GrapeDeep;
            for (int side = -1; side <= 1; side += 2)
            {
                var line = UiKit.Panel(_body, "rule" + side, new Vector2(side * 150f, Y(334f)), new Vector2(110f, 3f), Lilac);
                line.GetComponent<Image>().raycastTarget = false;
            }

            const float W = 128f, H = 150f, Gap = 10f;
            int n = (int)ChestTier.Royal + 1;
            int next = -1;
            for (int i = 0; i < n; i++)
                if (_meta.EndlessBest < (i + 1) * StageCatalog.EndlessBossEvery) { next = i; break; }
            for (int i = 0; i < n; i++)
            {
                int need = (i + 1) * StageCatalog.EndlessBossEvery;
                bool got = _meta.EndlessBest >= need;
                bool aim = i == next;
                float x = (i - (n - 1) * 0.5f) * (W + Gap);
                Color? line = got ? InkTheme.Seal : aim ? Grape : (Color?)null;
                Color fill = got ? InkTheme.Hex("FFF6E2") : aim ? LilacFace : InkTheme.CardFace;
                var card = UiKit.Stroke(_body, "c" + i, new Vector2(x, Y(434f)), new Vector2(W, H), Pin.Center, 4f,
                    line, fill, 18f);
                card.GetComponent<Image>().raycastTarget = false;
                ChestDef def = ChestCatalog.Get((ChestTier)i);
                var icon = UiKit.Icon(card, InkSprites.Load("Ui/chest_" + def.Key), new Vector2(0f, 16f), 84f);
                if (aim) UiAnim.On(icon).Breathe(icon.transform, 0f, 0.06f, 1.2f);
                string label = i == n - 1 ? $"{need} 波起" : $"{need} 波";
                var at = UiKit.Label(card, "at", label, 22, new Vector2(0f, -50f), new Vector2(W, 28f));
                UiKit.Bold(at);
                at.color = got ? InkTheme.Seal : aim ? Grape : InkTheme.TextDark;
            }
        }

        void BuildPlays()
        {
            int cap = _meta.EndlessPlaysCap;
            bool ad = !_meta.EndlessAdDone;
            var plays = UiKit.Label(_body, "plays", $"今日次数 {_meta.EndlessPlaysLeft}/{cap}", 26,
                new Vector2(ad ? -130f : 0f, Y(610f)), new Vector2(260f, 36f));
            UiKit.Bold(plays);
            if (!ad) return;
            var btn = UiKit.Btn(_body, "ad", $"+{GameConstants.EndlessAdPlays} 次", new Vector2(150f, Y(610f)),
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

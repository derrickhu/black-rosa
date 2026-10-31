using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 每日副本的开局页：全服同一局残局（词缀、开局留几格、禁用的字、今日最好），点「开始」才扣次数。
    // 从副本页的每日卡点进来。看广告加次数之后整块重画。
    public sealed class DailyPuzzlePanel : MonoBehaviour
    {
        const float BoardW = 640f;
        const float BoardH = 900f;
        const float CardW = BoardW - 48f;
        const float Tile = 66f;
        // 上半张只放今日词缀和开局字。成绩、印章、次数、开始都在纸面上，照无尽页排。
        const float CardTop = 122f;
        const float CardH = 228f;
        const float ScoreY = 398f;
        const float RewardHeadY = 458f;
        const float ChestY = 562f;
        const float PlaysY = 708f;
        const float GoY = 812f;

        static readonly Color Jade = DungeonPageBuilder.Jade;
        static readonly Color JadeDeep = DungeonPageBuilder.JadeDeep;
        static readonly Color JadeFace = InkTheme.Hex("EAF6EF");
        static readonly Color JadeLine = InkTheme.Hex("B7DCCB");
        static readonly Color Gold = InkTheme.Hex("F6BE3C");

        MetaProgress _meta;
        Action _start;
        Action _changed;
        RectTransform _layer;
        RectTransform _body;

        public static void Show(RectTransform layer, MetaProgress meta, Action start, Action changed)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "daily_puzzle_panel";
            var panel = dim.gameObject.AddComponent<DailyPuzzlePanel>();
            panel._meta = meta;
            panel._start = start;
            panel._changed = changed;
            panel._layer = layer;
            var board = PanelKit.Board(dim, "每日副本", Vector2.zero, new Vector2(BoardW, BoardH), Pin.Center, panel.Close,
                "Ui/ribbon_daily", 380f, 0.10f);
            panel._body = ResultKit.Group(board, "body", Vector2.zero, new Vector2(BoardW, BoardH));
            panel.Refresh();
        }

        void Close()
        {
            AudioBus.Tap();
            Destroy(gameObject);
        }

        static float Y(float fromTop) => BoardH * 0.5f - fromTop;

        void Refresh()
        {
            for (int i = _body.childCount - 1; i >= 0; i--) Destroy(_body.GetChild(i).gameObject);
            int hours = Mathf.Max(1, Mathf.CeilToInt(DayClock.SecondsToTomorrow / 3600f));
            var reset = UiKit.Label(_body, "reset", $"{hours} 小时后换新 · 全服同一局", 20, new Vector2(0f, Y(96f)),
                new Vector2(400f, 28f));
            reset.color = InkTheme.TextMid;
            BuildBoard();
            BuildScore();
            if (_meta.SealOpen) BuildReward();
            else BuildChests();
            BuildPlays();
            BuildGo();
        }

        // 今日词缀、铲子说明、今天不能用的字。
        void BuildBoard()
        {
            StageDef s = StageCatalog.DailyStage(DayClock.Today, DayClock.Week);
            var card = UiKit.Stroke(_body, "puzzle", new Vector2(0f, Y(CardTop + CardH * 0.5f)), new Vector2(CardW, CardH),
                Pin.Center, 4f, null, JadeFace, 22f);
            card.GetComponent<Image>().raycastTarget = false;
            float top = CardH * 0.5f;

            var tag = UiKit.Stroke(card, "tag", new Vector2(-CardW * 0.5f + 100f, top - 44f), new Vector2(160f, 50f),
                Pin.Center, 3f, null, Jade, 14f);
            tag.GetComponent<Image>().raycastTarget = false;
            var tn = UiKit.Label(tag, "t", "今日 · " + s.Rule.Name, 22, Vector2.zero, new Vector2(160f, 50f));
            tn.color = InkTheme.CardFace;
            UiKit.Bold(tn);
            var desc = UiKit.Label(card, "desc", s.Rule.Desc, 21, new Vector2(80f, top - 44f), new Vector2(380f, 40f),
                TextAnchor.MiddleLeft);
            desc.color = JadeDeep;

            const float MechW = 320f;
            float mechX = -CardW * 0.5f + 28f + MechW * 0.5f;
            var mech = UiKit.Label(card, "mech", "特殊机制：铲子可以挖开一格", 20,
                new Vector2(mechX, top - 100f), new Vector2(MechW, 32f), TextAnchor.MiddleLeft);
            mech.color = InkTheme.TextMid;
            UiKit.Bold(mech);
            var shovel = UiKit.Icon(card, InkArt.Shovel(160), new Vector2(-CardW * 0.5f + 76f, top - 168f), 96f);
            shovel.raycastTarget = false;

            Caption(card, "今日禁用", new Vector2(CardW * 0.5f - 200f, top - 96f));
            int b = s.Banned != null ? s.Banned.Length : 0;
            for (int i = 0; i < b; i++)
                Glyph(card, s.Banned[i], 0, new Vector2(CardW * 0.5f - 164f + i * (Tile + 18f), top - 148f), true);
        }

        void BuildScore()
        {
            int best = _meta.DailyBestToday;
            var bestT = ResultKit.Headline(_body, "best", best > 0 ? $"今日最好 第 {best} 波" : "今天还没打",
                46, new Vector2(0f, Y(ScoreY)), Gold, InkTheme.Outline, 3f);
            bestT.rectTransform.sizeDelta = new Vector2(CardW, 64f);
        }

        void RewardRule()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var line = UiKit.Panel(_body, "rule" + side, new Vector2(side * 150f, Y(RewardHeadY)), new Vector2(110f, 3f), JadeLine);
                line.GetComponent<Image>().raycastTarget = false;
            }
        }

        // 通关第五章前：10 波木箱、20 波银箱。拿到的盖红，下一档描绿。
        void BuildChests()
        {
            var head = UiKit.Label(_body, "chesthead", "守 波 奖 励", 22, new Vector2(0f, Y(RewardHeadY)), new Vector2(300f, 30f));
            UiKit.Bold(head);
            head.color = JadeDeep;
            RewardRule();

            int best = _meta.DailyBestToday;
            int[] need = { StageCatalog.DailyBossEvery, StageCatalog.DailyWaves };
            ChestTier[] tier = { ChestTier.Wood, ChestTier.Silver };
            string[] when = { $"撑过 {need[0]} 波", $"守住 {need[1]} 波" };
            int next = best >= need[1] ? -1 : best >= need[0] ? 1 : 0;
            const float W = 248f, H = 156f, Gap = 20f;
            for (int i = 0; i < 2; i++)
            {
                bool got = best >= need[i];
                bool aim = i == next;
                float x = (i - 0.5f) * (W + Gap);
                Color? line = got ? InkTheme.Seal : aim ? Jade : (Color?)null;
                Color fill = got ? InkTheme.Hex("FFF6E2") : aim ? JadeFace : InkTheme.CardFace;
                var card = UiKit.Stroke(_body, "c" + i, new Vector2(x, Y(ChestY)), new Vector2(W, H), Pin.Center, 4f,
                    line, fill, 20f);
                card.GetComponent<Image>().raycastTarget = false;
                ChestDef def = ChestCatalog.Get(tier[i]);
                var icon = UiKit.Icon(card, InkSprites.Load("Ui/chest_" + def.Key), new Vector2(0f, 22f), 92f);
                if (aim) UiAnim.On(icon).Breathe(icon.transform, 0f, 0.06f, 1.2f);
                var name = UiKit.Label(card, "name", def.Name, 24, new Vector2(0f, -28f), new Vector2(W - 16f, 32f));
                UiKit.Bold(name);
                var at = UiKit.Label(card, "at", when[i], 20, new Vector2(0f, -54f), new Vector2(W - 16f, 28f));
                Color ink = got ? InkTheme.Seal : aim ? JadeDeep : InkTheme.TextDark;
                name.color = ink;
                at.color = ink;
            }
        }

        // 通关第五章后：全部守住才给一枚。四格是品质比例，种类另外都会有。
        void BuildReward()
        {
            var head = UiKit.Label(_body, "chesthead", "结 算 奖 励", 22, new Vector2(0f, Y(RewardHeadY)), new Vector2(300f, 30f));
            UiKit.Bold(head);
            head.color = JadeDeep;
            RewardRule();

            const float W = 516f, H = 156f;
            var card = UiKit.Stroke(_body, "seal", new Vector2(0f, Y(ChestY)), new Vector2(W, H), Pin.Center, 4f,
                Jade, JadeFace, 20f);
            card.GetComponent<Image>().raycastTarget = false;
            var title = UiKit.Label(card, "title", "全部守住得一枚 · 七种类型都会有", 20,
                new Vector2(0f, 52f), new Vector2(W - 24f, 28f));
            UiKit.Bold(title);
            title.color = JadeDeep;

            SealQuality[] qs = { SealQuality.Green, SealQuality.Blue, SealQuality.Purple, SealQuality.Gold };
            string[] pct = { "60%", "28%", "10%", "2%" };
            for (int i = 0; i < qs.Length; i++)
            {
                float x = (i - 1.5f) * 112f;
                SealArt.Medal(card, "q" + i, SealType.Edge, qs[i], new Vector2(x, 4f), 56f);
                var p = UiKit.Label(card, "p" + i, pct[i], 20, new Vector2(x, -48f), new Vector2(90f, 26f));
                UiKit.Bold(p);
                p.color = SealCatalog.QualityDeep(qs[i]);
            }
        }

        void BuildPlays()
        {
            int cap = _meta.DailyPlaysCap;
            bool ad = !_meta.DailyAdDone;
            var plays = UiKit.Label(_body, "plays", $"今日次数 {_meta.DailyPlaysLeft}/{cap}", 26,
                new Vector2(ad ? -130f : 0f, Y(PlaysY)), new Vector2(260f, 36f));
            UiKit.Bold(plays);
            if (!ad) return;
            var btn = UiKit.Btn(_body, "ad", $"+{GameConstants.DailyAdPlays} 次", new Vector2(150f, Y(PlaysY)),
                new Vector2(220f, 80f), OnAd);
            ResultKit.AdMark(btn, 34f);
        }

        void BuildGo()
        {
            var go = UiKit.Btn(_body, "go", "开始", new Vector2(0f, Y(GoY)), new Vector2(400f, 96f), OnStart, true);
            if (_meta.DailyPlaysLeft <= 0) PanelKit.Dim(go, true);
            else
            {
                UiKit.PaintBtn(go, Jade, JadeDeep, InkTheme.CardFace);
                UiAnim.On(go).Breathe(go.transform, 0.3f, 0.035f, 1.1f);
            }
        }

        static void Caption(RectTransform card, string text, Vector2 pos)
        {
            var c = UiKit.Label(card, "cap", text, 20, pos + new Vector2(90f, 0f), new Vector2(180f, 28f), TextAnchor.MiddleLeft);
            c.color = InkTheme.TextMid;
            UiKit.Bold(c);
        }

        // 一枚字方块：字色描边、宣纸底，星级挂在下沿。禁用的压灰、斜一道朱线。
        static void Glyph(RectTransform card, CardId id, int star, Vector2 pos, bool banned)
        {
            Color accent = banned ? InkTheme.Hex("9A928A") : CardCatalog.Accent(id);
            var tile = UiKit.Stroke(card, "g" + (int)id, pos, new Vector2(Tile, Tile), Pin.Center, 4f, accent,
                banned ? InkTheme.Hex("E9E4DD") : InkTheme.CardFace, 14f);
            tile.GetComponent<Image>().raycastTarget = false;
            var t = UiKit.Label(tile, "t", CardCatalog.Get(id).Name, 38, new Vector2(0f, 2f), new Vector2(Tile, Tile));
            t.color = accent;
            UiKit.Bold(t);
            if (banned)
            {
                var slash = UiKit.Panel(tile, "x", Vector2.zero, new Vector2(Tile * 1.1f, 5f), InkTheme.Seal);
                slash.localRotation = Quaternion.Euler(0f, 0f, -38f);
                slash.GetComponent<Image>().raycastTarget = false;
                return;
            }
            for (int i = 0; i < star; i++)
            {
                float x = (i - (star - 1) * 0.5f) * 20f;
                UiKit.Icon(tile, InkSprites.Load("Ui/result_star_on"), new Vector2(x, -Tile * 0.5f - 2f), 22f);
            }
        }

        void OnAd()
        {
            AudioBus.Tap();
            AdStub.Reward("daily_plays", () =>
            {
                if (this == null || !_meta.GrantDailyAdPlays()) return;
                _changed?.Invoke();
                Refresh();
            });
        }

        void OnStart()
        {
            if (_meta.DailyPlaysLeft <= 0)
            {
                AudioBus.Deny();
                InkToast.Show(_layer, _meta.DailyAdDone
                    ? "今天的残局次数用完了，明天换一局"
                    : $"今天的次数用完了，看广告再开 {GameConstants.DailyAdPlays} 局");
                return;
            }
            AudioBus.Tap();
            Action start = _start;
            Destroy(gameObject);
            start?.Invoke();
        }
    }
}

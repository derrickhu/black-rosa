using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class EndlessResultArgs
    {
        public bool Win;
        public bool Daily;      // 每日残局：纪录和次数按当天算，第五章后全部守住拉一次印章转轮
        public int Total;       // 这局一共几波
        public EndlessResult Result;
        public Action Double;
        public Action Again;
        public Action Rank;
        public Action Home;
    }

    // 每日副本和无尽的结算，全屏演，不走宣纸小卡。
    // 全通关：光芒转起来，金横幅砸下，波数滚上去，新纪录盖章，彩带。
    // 没守住：灰横幅荡下来，波数滚上去，一条离下一档宝箱还差多少的进度。
    // 点屏幕跳过演出。墨已经入账，翻倍广告再加一份同样多的。
    public sealed class EndlessResultPanel : MonoBehaviour
    {
        const float BannerY = 330f;
        const float SubY = 200f;
        const float WaveY = 96f;
        const float BestY = 8f;
        const float BarY = -48f;
        const float LootY = -168f;
        const float RankY = -318f;
        const float DoubleY = -414f;
        const float AgainY = -514f;
        const float SlotY = -190f;
        const float SlotLift = 10f;

        static readonly Color Gold = InkTheme.Hex("F6BE3C");
        static readonly Color Cream = InkTheme.Hex("FFE7B8");
        static readonly Color Jade = InkTheme.Hex("8FE3C0");
        static readonly Color Lilac = InkTheme.Hex("E4D0FF");

        MetaProgress _meta;
        EndlessResultArgs _a;
        UiAnim _anim;
        RectTransform _root;
        Text _ink;
        Button _double;
        SealSlot _slot;

        public RectTransform Root => _root;

        public static EndlessResultPanel Show(RectTransform layer, MetaProgress meta, EndlessResultArgs a)
        {
            var dim = UiKit.Dimmer(layer);
            dim.name = "endless_result";
            var p = dim.gameObject.AddComponent<EndlessResultPanel>();
            p._meta = meta;
            p._a = a;
            p._root = dim;
            p._anim = UiAnim.On(p);
            ResultKit.SkipCatcher(dim, () =>
            {
                if (p._slot != null) p._slot.Skip();
                if (p._anim.Playing) p._anim.Finish();
            });
            p.Build();
            AudioBus.Sweep(dim);
            return p;
        }

        void Build()
        {
            bool win = _a.Win;
            var dim = _root.GetComponent<Image>();
            Color veil = win ? new Color(0.10f, 0.07f, 0.05f, 0.84f) : new Color(0.06f, 0.06f, 0.07f, 0.88f);
            dim.color = new Color(veil.r, veil.g, veil.b, 0f);
            _anim.Fade(dim, 0f, 0.35f, 0f, veil.a);

            var stage = ResultKit.Group(_root, "stage", Vector2.zero, new Vector2(ScreenFit.DesignW, ScreenFit.DesignH));
            if (win) PlayWin(stage);
            else PlayLose(stage);
            float buttonsAt = win ? 1.7f : 1.4f;
            if (_a.Daily && _a.Result.HasSeal) buttonsAt = BuildSlot(stage, win ? 1.05f : 0.9f);
            else BuildLoot(stage, win ? 1.45f : 1.15f);
            BuildButtons(stage, buttonsAt);
            if (win)
                _anim.At(1.55f, () =>
                {
                    UiConfetti.Burst(_root, 100);
                    AudioBus.Confetti();
                });
        }

        void PlayWin(RectTransform stage)
        {
            Color ray = _a.Daily ? new Color(0.55f, 0.95f, 0.72f, 0.85f) : new Color(0.78f, 0.62f, 1f, 0.85f);
            var rays = ResultKit.Rays(stage, new Vector2(0f, BannerY - 20f), 760f, ray);
            _anim.Fade(rays, 0f, 0.45f, 0f, 0.85f).Spin(rays.transform, 14f).Pop(rays.transform, 0f, 0.55f, 0.35f);

            var banner = ResultKit.Banner(stage, true, "全部守住", new Vector2(0f, BannerY), 620f);
            _anim.Move(banner, new Vector2(0f, 860f), new Vector2(0f, BannerY), 0.05f, 0.4f, Ease.OutBack)
                 .Punch(banner, 0.45f, 0.1f, 0.28f)
                 .At(0.42f, AudioBus.Stamp);

            int total = Total;
            var sub = UiKit.Label(stage, "sub", (_a.Daily ? "每日副本" : "无尽副本") + $" · {total} 波全部打完", 28,
                new Vector2(0f, SubY), new Vector2(620f, 40f));
            sub.color = _a.Daily ? Jade : Lilac;
            UiKit.Bold(sub);
            _anim.Fade(sub, 0.5f, 0.28f, 0f, 1f);

            EndlessResult r = _a.Result;
            var big = ResultKit.Headline(stage, "waves", "第 0 波", 84, new Vector2(r.NewBest ? -36f : 0f, WaveY), Gold, InkTheme.Hex("7A1E14"), 4f);
            _anim.Pop(big.transform, 0.48f, 0.28f, 0f)
                 .CountUp(big, 0, r.Waves, 0.55f, 0.7f, "第 {0} 波", AudioBus.CountTick)
                 .Punch(big.transform, 1.25f, 0.14f, 0.28f);
            if (r.NewBest) _anim.Shake(stage, 1.25f, 10f, 0.24f);

            BuildBest(stage, 1.15f);
            if (r.NewBest) Stamp(stage, "新纪录", new Vector2(228f, WaveY), -12f, 1.2f);
        }

        void PlayLose(RectTransform stage)
        {
            var banner = ResultKit.Banner(stage, false, "没守住", new Vector2(0f, BannerY), 580f);
            banner.pivot = new Vector2(0.5f, 1f);
            float hung = BannerY + banner.sizeDelta.y * 0.5f;
            _anim.Move(banner, new Vector2(0f, 900f), new Vector2(0f, hung), 0.12f, 0.38f, Ease.OutCubic)
                 .Rotate(banner, -18f, 0f, 0.12f, 0.85f, Swing)
                 .Punch(banner, 0.5f, 0.06f, 0.22f)
                 .At(0.5f, AudioBus.Lose);

            var sub = UiKit.Label(stage, "sub", _a.Daily ? "每日副本" : "无尽副本", 28,
                new Vector2(0f, SubY), new Vector2(620f, 40f));
            sub.color = InkTheme.Hex("D8CFC4");
            UiKit.Bold(sub);
            _anim.Fade(sub, 0.55f, 0.25f, 0f, 1f);

            EndlessResult r = _a.Result;
            var big = ResultKit.Headline(stage, "waves", "第 0 波", 84, new Vector2(0f, WaveY), Gold, InkTheme.Hex("23201E"), 4f);
            _anim.Pop(big.transform, 0.6f, 0.28f, 0f)
                 .CountUp(big, 0, r.Waves, 0.65f, 0.65f, "第 {0} 波", AudioBus.CountTick)
                 .Punch(big.transform, 1.3f, 0.1f, 0.24f);

            BuildBest(stage, 1.05f);
            BuildRemain(stage, 1.1f);
        }

        void BuildBest(RectTransform stage, float at)
        {
            EndlessResult r = _a.Result;
            string span = _a.Daily ? "今日" : "本周";
            string line = r.NewBest
                ? (r.OldBest > 0 ? $"{span}新纪录，之前最好第 {r.OldBest} 波" : span + "新纪录")
                : $"{span}最好 第 {Mathf.Max(r.OldBest, r.Waves)} 波";
            var best = UiKit.Label(stage, "best", line, 26, new Vector2(0f, BestY), new Vector2(640f, 36f));
            UiKit.Bold(best);
            best.color = r.NewBest ? InkTheme.Hex("FF8A8A") : InkTheme.Hex("CDBFA8");
            _anim.Fade(best, at, 0.25f, 0f, 1f);
        }

        // 进度对着下一只箱子。已经是最高档了，就对着全部守住。
        // 每日残局通关第五章后不再发箱子，没守住只对着全部守住。
        void BuildRemain(RectTransform stage, float at)
        {
            int total = Total;
            int waves = Mathf.Clamp(_a.Result.Waves, 0, total);
            if (_a.Daily && _meta.SealOpen)
            {
                string dailyTip = waves >= total ? $"共 {total} 波" : $"再撑 {total - waves} 波就全部守住";
                var line = UiKit.Label(stage, "remain", dailyTip, 22, new Vector2(0f, BarY), new Vector2(520f, 32f));
                line.color = Cream;
                _anim.Fade(line, at, 0.25f, 0f, 1f);
                return;
            }
            int step = _a.Daily ? StageCatalog.DailyBossEvery : StageCatalog.EndlessBossEvery;
            if (step < 1) step = total;
            int next = (waves / step + 1) * step;
            int cur = _a.Result.Chest;
            string tip;
            float goal = total;
            if (next <= total)
            {
                int nextTier = (int)TierAt(next);
                if (cur < 0)
                {
                    tip = $"再撑 {next - waves} 波，结算多给一只{ChestCatalog.Get(nextTier).Name}";
                    goal = next;
                }
                else if (nextTier > cur)
                {
                    tip = $"再撑 {next - waves} 波，下一只是{ChestCatalog.Get(nextTier).Name}";
                    goal = next;
                }
                else tip = $"再撑 {total - waves} 波就全部守住";
            }
            else tip = $"共 {total} 波";
            var host = ResultKit.Group(stage, "remain", new Vector2(0f, BarY), new Vector2(520f, 70f));
            var fill = ResultKit.Bar(host, new Vector2(0f, 16f), 420f, 26f, InkTheme.Hex("F58A34"));
            var t = UiKit.Label(host, "t", tip, 22, new Vector2(0f, -22f), new Vector2(520f, 30f));
            t.color = Cream;
            float k0 = goal > 0f ? waves / goal : 0f;
            _anim.Pop(host, at, 0.3f)
                 .Tween(at + 0.15f, 0.55f, k => ResultKit.SetBar(fill, k0 * Ease.OutCubic(k)));
        }

        void BuildLoot(RectTransform stage, float at)
        {
            EndlessResult r = _a.Result;
            var host = ResultKit.Group(stage, "loot", new Vector2(0f, LootY), new Vector2(560f, 176f));
            var card = UiKit.Stroke(host, "card", Vector2.zero, new Vector2(560f, 176f), Pin.Center, 4f,
                null, InkTheme.Hex("FFF8EC"), 22f);
            card.GetComponent<Image>().raycastTarget = false;
            var head = UiKit.Label(card, "h", "本局收获", 20, new Vector2(0f, 62f), new Vector2(500f, 28f));
            head.color = InkTheme.TextMid;

            bool chest = r.Chest >= 0;
            float inkX = chest ? -130f : 0f;
            var inkIcon = UiKit.Icon(card, InkSprites.Ui("ink"), new Vector2(inkX, 8f), 72f);
            _ink = UiKit.Label(card, "ink", "墨 +0", 32, new Vector2(inkX, -48f), new Vector2(240f, 40f));
            UiKit.Bold(_ink);
            _anim.CountUp(_ink, 0, r.Ink, at + 0.15f, 0.45f, "墨 +{0}", AudioBus.CountTick);
            _anim.Pop(inkIcon.transform, at + 0.1f, 0.28f, 0.4f);

            if (chest)
            {
                ChestDef def = ChestCatalog.Get(r.Chest);
                var icon = UiKit.Icon(card, InkSprites.Load("Ui/chest_" + def.Key), new Vector2(130f, 10f), 84f);
                string where = r.ChestSlot == -1 ? "位满了，先寄着" : r.ChestSlot == -3 ? "放不下，折成墨了" : "已放进宝箱位";
                var n = UiKit.Label(card, "chest", def.Name + " · " + where, 20, new Vector2(130f, -48f), new Vector2(280f, 30f));
                n.color = InkTheme.TextDark;
                _anim.Pop(icon.transform, at + 0.22f, 0.32f, 0.2f).At(at + 0.4f, AudioBus.ChestLand);
            }
            _anim.Pop(host, at, 0.32f).At(at + 0.12f, AudioBus.Stamp);
        }

        void BuildButtons(RectTransform stage, float at)
        {
            float lift = _a.Rank != null ? 0f : RankY - DoubleY;
            if (_slot != null) lift = SlotLift;
            if (_a.Rank != null)
            {
                var rank = ResultKit.Group(stage, "rank", new Vector2(0f, RankY), new Vector2(300f, 72f));
                UiKit.Btn(rank, "b", "看排行榜", Vector2.zero, new Vector2(300f, 72f), () => _a.Rank(), false);
                _anim.Pop(rank, at, 0.28f);
            }

            if (_a.Double != null)
            {
                var dbl = ResultKit.Group(stage, "double", new Vector2(0f, DoubleY + lift), new Vector2(320f, 84f));
                _double = UiKit.Btn(dbl, "b", "墨翻倍", Vector2.zero, new Vector2(320f, 84f), () => _a.Double());
                ResultKit.AdMark(_double, 34f);
                _anim.Pop(dbl, at + 0.08f, 0.3f);
            }

            int left = _a.Daily ? _meta.DailyPlaysLeft : _meta.EndlessPlaysLeft;
            var row = ResultKit.Group(stage, "row", new Vector2(0f, AgainY + lift), new Vector2(500f, 88f));
            var again = UiKit.Btn(row, "again", $"再来一局（剩 {left}）", new Vector2(-128f, 0f), new Vector2(248f, 84f),
                () => _a.Again?.Invoke(), _double == null);
            if (left <= 0) PanelKit.Dim(again, true);
            else _anim.Breathe(again.transform, at + 0.5f, 0.035f, 1.2f);
            UiKit.Btn(row, "home", "回主页", new Vector2(140f, 0f), new Vector2(200f, 84f), () => _a.Home?.Invoke(), false);
            _anim.Pop(row, at + 0.16f, 0.3f);
        }

        RectTransform Stamp(RectTransform parent, string text, Vector2 pos, float tilt, float at)
        {
            var g = ResultKit.Group(parent, "stamp", pos, new Vector2(128f, 54f));
            g.localRotation = Quaternion.Euler(0f, 0f, tilt);
            UiKit.Stroke(g, "bg", Vector2.zero, new Vector2(128f, 54f), Pin.Center, 4f, fill: InkTheme.Rose, radius: 14f);
            var t = UiKit.Label(g, "t", text, 26, Vector2.zero, new Vector2(128f, 54f));
            t.color = Color.white;
            UiKit.Bold(t);
            _anim.Pop(g, at, 0.24f, 2.2f).At(at + 0.18f, AudioBus.Stamp);
            return g;
        }

        int Total => _a.Total > 0 ? _a.Total : StageCatalog.EndlessWaves;

        ChestTier TierAt(int wave)
        {
            if (_a.Daily) return (ChestTier)MetaProgress.DailyChestTier(wave);
            return (ChestTier)MetaProgress.EndlessChestTier(wave);
        }

        // 每日残局的印章转轮。奖已经入账，转轮只是停到那三枚。返回按钮出场的时间。
        float BuildSlot(RectTransform stage, float at)
        {
            float delay = at + 0.45f;
            _slot = SealSlot.Build(stage, _root, new Vector2(0f, SlotY), _a.Result.Spin, delay);
            _ink = _slot.Ink;
            _anim.Pop(_slot.transform, at, 0.34f)
                 .At(at + 0.12f, AudioBus.Stamp)
                 .CountUp(_ink, 0, _a.Result.Ink, at + 0.15f, 0.45f, "墨 +{0}", AudioBus.CountTick);
            return _slot.Duration - 0.5f;
        }

        static float Swing(float k) => 1f - Mathf.Cos(k * Mathf.PI * 3.2f) * Mathf.Pow(1f - k, 2.2f);

        public void Doubled()
        {
            if (_ink != null) _ink.text = "墨 +" + _a.Result.Ink * 2;
            if (_double != null)
            {
                PanelKit.SetText(_double, "已翻倍");
                PanelKit.Dim(_double, true);
                _double.interactable = false;
            }
            AudioBus.Stamp();
        }
    }
}

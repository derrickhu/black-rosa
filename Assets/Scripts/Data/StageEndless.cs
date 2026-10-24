using System.Collections.Generic;
using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 无尽模式：一局 70 波，越打越硬，看能撑到第几波。成绩上排行榜的「无尽」页（按周清榜）。
    // 前 50 波的血量曲线不动，免得已有的波数成绩和每日残局一起漂：
    // 第 1 波 2.3 倍，第 10 波约 3.9，第 20 波约 6.3，第 30 波约 9.3，第 40 波约 12.3，
    // 第 50 波 14 倍（第十章关底 3.5 × 1.4 × 1.15 ≈ 5.6 倍，这里约是它的 2.5 倍）。
    // 第 51 波起才是第二幕加出来的尾段，血量缓升到第 70 波的 17.5 倍，约章底的 3.1 倍。
    // 薄皮杂兵另有下限。满盘面打得过第 50 波，不再是墙。
    // 只数跟着每波的总血涨，封顶 EndlessBodyCap，后期不至于卡。
    // 再打个只数折扣：前 10 波少约一成；第 11 波起再收，到第 30 波大约剩四分之三。
    // 盘面开局一排；第 10 波起每 3 波随机开一格，先第二排后第三排，第 43 波开满。
    // 第 10 波起魔气从上往下渗到第 40 波，怪的减伤跟着涨到 12%。
    // 每周换一条魔气词缀（StageRules），血、速、钱、怪量跟着词缀乘。
    // 金币掉得比主线吝啬、抽牌越抽越贵，钱永远不够花。
    // 兵种分四段解锁。第 10、20、30、40、50 波是原来的五只关底，
    // 第 60 波影姬、第 70 波钟魔，接在魔气铺满之后。
    // 每波用固定种子排，所有人打的是同一套，排行榜才公平。
    // 不进主线关卡表，也不进 check_stages 的主线曲线。
    public static partial class StageCatalog
    {
        public const int EndlessWaves = 70;
        // 血量和移速收到这一波为止，和加尾段之前的 50 波完全一样。每日残局也读这条曲线。
        const int EndlessPaceWaves = 50;
        public const int EndlessBossEvery = 10;
        public const string EndlessBackdrop = "Bg/battle_bg_endless";
        // 同布局的魔化莲池。第 10 波开始从上往下渗，第 40 波整张换掉。
        // 减伤跟这张图走，满了是 12%，中间按整数百分点一点一点加。
        public const string EndlessDarkBackdrop = "Bg/battle_bg_endless_dark";
        public const int EndlessDarkFrom = 10;
        public const int EndlessDarkTo = 40;
        const float EndlessWardTop = 0.12f;

        // 基础移速、掉墨借这一章；移速再按 EndlessSpeed 逐波加。
        const int EndlessPace = 3;
        const float EndlessHpOpen = 2.3f;
        const float EndlessHpTop = 14f;
        // 第 70 波。从第 50 波的 14 倍线性接到这里，一波大约加 0.18 倍。
        const float EndlessHpTail = 17.5f;
        // 大于 1：同样的终点，进度前段走得快，后段变平。
        const float EndlessHpEase = 1.65f;
        // 基础血低于这个数的杂兵先抬到这里再乘倍率，免得跑得快的那几种两三下就没了。
        const float EndlessThinHp = 6.5f;
        // 每波杂兵的基础血总量（按第一章那一只算），再乘上当波倍率才是实际的血。
        const float EndlessBudgetOpen = 104f;
        const float EndlessBudgetStep = 8.2f;
        // 关底波的护送只给这一成，压力在关底本人。
        const float EndlessEscort = 0.5f;
        const int EndlessBodyCap = 86;
        // 每波多长。出场摊在前面，最后留几秒给场上的怪走完。后期只数涨得比时长快，越往后越密。
        const float EndlessSpanOpen = 14f;
        const float EndlessSpanStep = 0.1f;
        // 第 50 波比第 1 波快这么多。尾段再收到 EndlessSpeedTail。
        const float EndlessSpeedTop = 1.22f;
        const float EndlessSpeedTail = 1.28f;

        // 开局只有第一排。第 EndlessCellFrom 波（从 1 数）起每 EndlessCellEvery 波开一格：
        // 先在第二排随机挑，第二排满了再随机开第三排。挑哪格每局不同，只数和时机人人一样。
        public const int EndlessCellFrom = 10;
        const int EndlessCellEvery = 3;
        public const int EndlessCellMax = GameConstants.Columns * (GameConstants.Rows - 1);
        const float EndlessTail = 5f;
        // 抽牌定价只看前这么多波，按一关的钱袋来定起价。
        const int EndlessPriceWaves = 12;
        // 金币一直紧：掉落只吃血量倍率的这个次方（主线是 0.6），后面怪再硬钱也涨得慢；
        // 抽牌价在线性之外再加一截平方项，抽得越多越贵。金币收益在无尽里按 EndlessGainBoost 倍算，
        // 装福袋炮台是缺钱时的正经出路。
        const float EndlessDropPow = 0.25f;
        const float EndlessDraftCurve = 0.9f;
        public const float EndlessGainBoost = 2f;

        static readonly EnemyId[][] EndlessTiers =
        {
            new[] { Walker, Chubby, Tall, Ball, BigHead },
            new[] { Runner, Shield, Swarm, Strafer, Belt },
            new[] { Crawler, Splitter, Mender, Elite, Sprinter },
            new[] { Bulwark, Warden, Shade, Monk },
        };
        static readonly int[] EndlessTierFrom = { 0, 5, 15, 30 };
        static readonly EnemyId[] EndlessBosses =
        {
            BossInkbag, BossTwin, BossThunder, BossMedic, BossKing, BossShade, BossBell
        };

        public static float EndlessHp(int wave)
        {
            float k = wave / (float)(EndlessPaceWaves - 1);
            if (k <= 1f)
            {
                float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(k), EndlessHpEase);
                return EndlessHpOpen * Mathf.Pow(EndlessHpTop / EndlessHpOpen, u);
            }
            return Mathf.Lerp(EndlessHpTop, EndlessHpTail, Tail(k));
        }

        // 无尽后段补强：第 30 波起抬，第 40 波 +30%，第 70 波 +40%。
        // 原曲线下没满改装也能打穿 70 波。只乘在出怪上，金币和墨的掉落曲线不动；每日残局只有 20 波，碰不到。
        public static float EndlessLateHp(int wave)
        {
            int n = wave + 1;
            if (n <= 30) return 1f;
            if (n <= 40) return Mathf.Lerp(1f, 1.3f, (n - 30) / 10f);
            return Mathf.Lerp(1.3f, 1.4f, (n - 40) / 30f);
        }

        // 第 50 波之后的进度，第 70 波正好是 1。
        static float Tail(float k) =>
            Mathf.Clamp01((k - 1f) / ((EndlessWaves - 1f) / (EndlessPaceWaves - 1f) - 1f));

        // 闯波类关卡里这一只的血：关底吃满倍率；杂兵基础血先托到 EndlessThinHp。mul 是当波倍率（含词缀）。
        public static float SurvivalHpOf(EnemyId id, float mul)
        {
            EnemyDef bare = EnemyCatalog.Base(id);
            float hp = bare.IsBoss ? bare.Hp : Mathf.Max(bare.Hp, EndlessThinHp);
            return hp * mul;
        }

        public static bool EndlessBossWave(int wave) => (wave + 1) % EndlessBossEvery == 0;

        public static float EndlessSpeed(int wave)
        {
            float k = wave / (float)(EndlessPaceWaves - 1);
            if (k <= 1f) return Mathf.Lerp(1f, EndlessSpeedTop, Mathf.Clamp01(k));
            return Mathf.Lerp(EndlessSpeedTop, EndlessSpeedTail, Tail(k));
        }

        // 打到第 wave 波（从 0 数）时第一排以外一共该开几格。
        public static int EndlessCellsAt(int wave) =>
            wave < EndlessCellFrom - 1 ? 0
                : Mathf.Min(EndlessCellMax, 1 + (wave - EndlessCellFrom + 1) / EndlessCellEvery);

        // 第 wave 波（从 0 数）魔化莲池该盖上多少。第 10 波刚渗出一点，第 40 波铺满。
        public static float EndlessDark(int wave)
        {
            float shown = wave + 1f;
            return Mathf.Clamp01((shown - (EndlessDarkFrom - 1f)) / (EndlessDarkTo - EndlessDarkFrom + 1f));
        }

        // 魔气护体：和侵染同一条进度，显示和结算都用这个整数百分比。
        public static int EndlessWardPercent(int wave)
        {
            float dark = EndlessDark(wave);
            if (dark <= 0f) return 0;
            int pct = Mathf.FloorToInt(dark * EndlessWardTop * 100f + 0.001f);
            if (pct < 1) pct = 1;
            return Mathf.Min(pct, Mathf.RoundToInt(EndlessWardTop * 100f));
        }

        public static float EndlessDrop(int wave) => Mathf.Pow(Mathf.Max(0.1f, EndlessHp(wave)), EndlessDropPow);

        // 前 10 波少约一成。第 11 波起再往下收，第 30 波起到底是四分之三。词缀的怪量倍率叠在这上面。
        static float EndlessCrowd(int wave)
        {
            const int knee = 9;
            if (wave <= knee) return 0.90f;
            float k = Mathf.Clamp01((wave - knee) / 20f);
            return Mathf.Lerp(0.86f, 0.75f, k);
        }

        public static StageDef EndlessStage(CardId[] pool, StageRule rule)
        {
            float countMul = rule != null ? rule.CountMul : 1f;
            var waves = new WaveDef[EndlessWaves];
            var bodies = new int[EndlessWaves][];
            var hp = new float[EndlessWaves];
            var gold = new float[EndlessWaves];
            var speed = new float[EndlessWaves];
            var ward = new int[EndlessWaves];
            var dark = new float[EndlessWaves];
            var cells = new int[EndlessWaves];
            for (int w = 0; w < EndlessWaves; w++)
            {
                EnemyId? boss = EndlessBossWave(w)
                    ? EndlessBosses[Mathf.Clamp((w + 1) / EndlessBossEvery - 1, 0, EndlessBosses.Length - 1)]
                    : (EnemyId?)null;
                SurvivalWave(w, boss, new System.Random(7919 + w * 131), countMul * EndlessCrowd(w), w, out waves[w], out bodies[w]);
                hp[w] = EndlessHp(w);
                gold[w] = EndlessDrop(w);
                speed[w] = EndlessSpeed(w);
                ward[w] = EndlessWardPercent(w);
                dark[w] = EndlessDark(w);
                cells[w] = EndlessCellsAt(w);
            }
            var s = new StageDef
            {
                Index = -1,
                Chapter = EndlessPace,
                Slot = 0,
                Title = "无尽",
                Name = "无尽 · " + EndlessWaves + " 波",
                Mask = G("XXXXXX"),
                Pool = pool,
                Fresh = new CardId[0],
                Waves = waves,
                Bodies = bodies,
                Hp = 1f,
                WaveHp = hp,
                WaveGold = gold,
                WaveSpeed = speed,
                WaveWard = ward,
                WaveDark = dark,
                WaveCells = cells,
                DarkShout = EndlessDarkFrom - 1,
                StaminaCost = 0,
                Endless = true,
                Backdrop = EndlessBackdrop,
            };
            ApplyRule(s, rule);
            PriceSurvival(s, EndlessPriceWaves);
            return s;
        }

        // 词缀乘到逐波表上。减伤只在已经有减伤的波次往上加，魔染例外：从第一波就带。
        static void ApplyRule(StageDef s, StageRule rule)
        {
            s.Rule = rule;
            if (rule == null) return;
            for (int w = 0; w < s.Waves.Length; w++)
            {
                s.WaveHp[w] *= rule.HpMul;
                s.WaveGold[w] *= rule.GoldMul;
                if (s.WaveSpeed != null) s.WaveSpeed[w] *= rule.SpeedMul;
                if (s.WaveWard != null) s.WaveWard[w] += rule.Ward;
            }
            s.DemonChance = Mathf.Max(s.DemonChance, rule.DemonChance);
        }

        // 闯波类的抽牌价只按前 n 波的钱袋定，再叠一截平方项；词缀的改装涨价乘在最后。
        static void PriceSurvival(StageDef s, int n)
        {
            n = Mathf.Min(n, s.Waves.Length);
            var head = new StageDef
            {
                Chapter = s.Chapter,
                Mask = s.Mask,
                Hp = s.Hp,
                Waves = Head(s.Waves, n),
                Bodies = Head(s.Bodies, n),
                WaveHp = Head(s.WaveHp, n),
                WaveGold = Head(s.WaveGold, n),
            };
            Price(head);
            float draft = s.Rule != null ? s.Rule.DraftMul : 1f;
            s.KillGold = head.KillGold;
            s.GoldPurse = head.GoldPurse;
            s.InkBudget = head.InkBudget;
            s.ChestGold = head.ChestGold;
            s.ChestInk = head.ChestInk;
            s.DraftFirst = Mathf.RoundToInt(head.DraftFirst * draft);
            s.DraftStep = head.DraftStep * draft;
            s.DraftCurve = EndlessDraftCurve * draft;
        }

        static T[] Head<T>(T[] src, int n)
        {
            var outp = new T[n];
            System.Array.Copy(src, outp, n);
            return outp;
        }

        // depth 是按无尽算的难度波号：决定兵种、预算、重兵上限和波长。shift 只用来错开出场列。
        static void SurvivalWave(int depth, EnemyId? boss, System.Random rng, float countMul, int shift,
            out WaveDef wave, out int[] bodies)
        {
            float budget = (EndlessBudgetOpen + EndlessBudgetStep * depth) * countMul;
            if (boss.HasValue) budget *= EndlessEscort;

            var roster = new List<EnemyId>();
            for (int t = 0; t < EndlessTiers.Length; t++)
                if (depth >= EndlessTierFrom[t]) roster.AddRange(EndlessTiers[t]);

            var ids = new List<EnemyId>();
            var groups = new List<int>();
            var used = new Dictionary<EnemyId, int>();
            float sum = 0f;
            int heads = 0, guard = 0;
            int cap = Mathf.RoundToInt((boss.HasValue ? EndlessBodyCap / 2 : EndlessBodyCap) * countMul);
            while (sum < budget && heads < cap && guard++ < 600)
            {
                EnemyId id = roster[rng.Next(roster.Count)];
                int limit = EndlessHeavyCap(id, depth);
                used.TryGetValue(id, out int had);
                if (limit >= 0 && had >= limit) continue;
                // 重兵抽中一半才算，不让它们和杂兵一样常见。
                if (limit >= 0 && rng.NextDouble() < 0.5) continue;
                int n = id == Swarm ? 3 : 1;
                used[id] = had + 1;
                ids.Add(id);
                groups.Add(n);
                sum += n * EnemyCatalog.Base(id).Hp;
                heads += n;
            }

            float span = EndlessSpanOpen + EndlessSpanStep * depth;
            float open = boss.HasValue ? 2.4f : 0.4f;
            float room = Mathf.Max(1f, span - EndlessTail - open);
            int cols = GameConstants.Columns;
            var specs = new List<SpawnSpec>();
            var counts = new List<int>();
            if (boss.HasValue)
            {
                EnemyId b = boss.Value;
                if (b == BossTwin)
                {
                    specs.Add(new SpawnSpec(0.3f, b, 1));
                    specs.Add(new SpawnSpec(0.3f, b, 4));
                    counts.Add(1);
                    counts.Add(1);
                }
                else
                {
                    specs.Add(new SpawnSpec(0.3f, b, 3));
                    counts.Add(1);
                }
            }
            for (int i = 0; i < ids.Count; i++)
            {
                float t = open + (ids.Count > 1 ? room * i / (ids.Count - 1f) : 0f);
                int col = (shift + SpreadOff(i % cols) + i / cols) % cols;
                specs.Add(new SpawnSpec(t, ids[i], col, groups[i]));
                counts.Add(groups[i]);
            }
            float dur = boss.HasValue ? Mathf.Max(22f, span + 8f) : span;
            wave = new WaveDef(dur, specs.ToArray());
            bodies = counts.ToArray();
        }

        // 带机制的重兵每波最多几只，-1 是不限。
        static int EndlessHeavyCap(EnemyId id, int w)
        {
            switch (id)
            {
                case Shield: return 1 + w / 8;
                case Mender: return 1 + w / 15;
                case Elite: return 1 + w / 12;
                case Bulwark: return 1 + w / 12;
                case Warden: return 1 + w / 15;
                case Shade: return 2 + w / 10;
                case Monk: return 1 + w / 20;
                default: return -1;
            }
        }
    }
}

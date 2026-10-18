using System.Collections.Generic;
using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 无尽模式：一局 50 波，越打越硬，看能撑到第几波。成绩上排行榜的「无尽」页（按周清榜）。
    // 血量每波一个倍率，前段抬得快、后段收平：第 1 波 2.3 倍，第 10 波约 3.9，
    // 第 20 波约 6.3，第 30 波约 9.3，第 40 波约 12.3，第 50 波停在 14 倍
    // （大约第八章关底的三倍）。薄皮杂兵另有下限。满盘面打得过第 50 波，不再是墙。
    // 只数跟着每波的总血涨，封顶 EndlessBodyCap，后期不至于卡。
    // 盘面开局一排；第 10 波起每 3 波随机开一格，先第二排后第三排，第 43 波开满。
    // 第 10 波起魔气从上往下渗到第 40 波，怪的减伤跟着涨到 12%。
    // 每周换一条魔气词缀（StageRules），血、速、钱、怪量跟着词缀乘。
    // 金币掉得比主线吝啬、抽牌越抽越贵，钱永远不够花。
    // 兵种分四段解锁，第 10、20、30、40、50 波各压一只关底。
    // 每波用固定种子排，所有人打的是同一套，排行榜才公平。
    // 不进 72 关的表，也不进 check_stages 的主线曲线。
    public static partial class StageCatalog
    {
        public const int EndlessWaves = 50;
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
        // 第 50 波比第 1 波快这么多。
        const float EndlessSpeedTop = 1.22f;

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
            new[] { Bulwark, Warden },
        };
        static readonly int[] EndlessTierFrom = { 0, 5, 15, 30 };
        static readonly EnemyId[] EndlessBosses = { BossInkbag, BossTwin, BossThunder, BossMedic, BossKing };

        public static float EndlessHp(int wave)
        {
            float k = Mathf.Clamp01(wave / (float)(EndlessWaves - 1));
            float u = 1f - Mathf.Pow(1f - k, EndlessHpEase);
            return EndlessHpOpen * Mathf.Pow(EndlessHpTop / EndlessHpOpen, u);
        }

        // 闯波类关卡里这一只的血：关底吃满倍率；杂兵基础血先托到 EndlessThinHp。mul 是当波倍率（含词缀）。
        public static float SurvivalHpOf(EnemyId id, float mul)
        {
            EnemyDef bare = EnemyCatalog.Base(id);
            float hp = bare.IsBoss ? bare.Hp : Mathf.Max(bare.Hp, EndlessThinHp);
            return hp * mul;
        }

        public static bool EndlessBossWave(int wave) => (wave + 1) % EndlessBossEvery == 0;

        public static float EndlessSpeed(int wave) =>
            Mathf.Lerp(1f, EndlessSpeedTop, Mathf.Clamp01(wave / (float)(EndlessWaves - 1)));

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
                SurvivalWave(w, boss, new System.Random(7919 + w * 131), countMul, w, out waves[w], out bodies[w]);
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
                default: return -1;
            }
        }
    }
}

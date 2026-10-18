using System.Collections.Generic;
using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 无尽模式：一局 50 波，越打越硬，看能撑到第几波。成绩上排行榜的「无尽」页。
    // 血量每波单独一个倍率，从第二章开头一路涨到第八章关底的 1.6 倍，大部分人到不了 50。
    // 只数跟着每波的总血涨，封顶 EndlessBodyCap，后期不至于卡。
    // 兵种分四段解锁，第 10、20、30、40、50 波各压一只关底。
    // 每波用固定种子排，所有人打的是同一套，排行榜才公平。
    // 不进 72 关的表，也不进 check_stages 的主线曲线。
    public static partial class StageCatalog
    {
        public const int EndlessWaves = 50;
        public const int EndlessBossEvery = 10;
        public const string EndlessBackdrop = "Bg/battle_bg_endless";

        // 移速、掉墨借这一章，整局不变，难度只加在血和只数上。
        const int EndlessPace = 3;
        const float EndlessHpOpen = 1.3f;
        const float EndlessHpTop = 7.2f;
        // 每波杂兵的基础血总量（按第一章那一只算），再乘上当波倍率才是实际的血。
        const float EndlessBudgetOpen = 70f;
        const float EndlessBudgetStep = 5f;
        // 关底波的护送只给这一成，压力在关底本人。
        const float EndlessEscort = 0.5f;
        const int EndlessBodyCap = 56;
        // 每波多长。出场摊在前面，最后留几秒给场上的怪走完。
        const float EndlessSpanOpen = 14f;
        const float EndlessSpanStep = 0.2f;
        const float EndlessTail = 5f;
        // 抽牌定价只看前这么多波，按一关的钱袋来定，后面的钱越打越多，正好够把格子升满。
        const int EndlessPriceWaves = 12;

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
            return EndlessHpOpen * Mathf.Pow(EndlessHpTop / EndlessHpOpen, k);
        }

        public static bool EndlessBossWave(int wave) => (wave + 1) % EndlessBossEvery == 0;

        public static StageDef EndlessStage(CardId[] pool)
        {
            var waves = new WaveDef[EndlessWaves];
            var bodies = new int[EndlessWaves][];
            var hp = new float[EndlessWaves];
            for (int w = 0; w < EndlessWaves; w++)
            {
                EndlessWave(w, out waves[w], out bodies[w]);
                hp[w] = EndlessHp(w);
            }
            var s = new StageDef
            {
                Index = -1,
                Chapter = EndlessPace,
                Slot = 0,
                Title = "无尽",
                Name = "无尽 · " + EndlessWaves + " 波",
                Mask = G("XXXXXX", "XXXXXX"),
                Pool = pool,
                Fresh = new CardId[0],
                Waves = waves,
                Bodies = bodies,
                Hp = 1f,
                WaveHp = hp,
                StaminaCost = 0,
                Endless = true
            };

            int n = Mathf.Min(EndlessPriceWaves, EndlessWaves);
            var head = new StageDef
            {
                Chapter = s.Chapter,
                Mask = s.Mask,
                Hp = s.Hp,
                Waves = Head(waves, n),
                Bodies = Head(bodies, n),
                WaveHp = Head(hp, n),
            };
            Price(head);
            s.KillGold = head.KillGold;
            s.GoldPurse = head.GoldPurse;
            s.InkBudget = head.InkBudget;
            s.ChestGold = head.ChestGold;
            s.ChestInk = head.ChestInk;
            s.DraftFirst = head.DraftFirst;
            s.DraftStep = head.DraftStep;
            return s;
        }

        static T[] Head<T>(T[] src, int n)
        {
            var outp = new T[n];
            System.Array.Copy(src, outp, n);
            return outp;
        }

        static void EndlessWave(int w, out WaveDef wave, out int[] bodies)
        {
            var rng = new System.Random(7919 + w * 131);
            bool boss = EndlessBossWave(w);
            float budget = EndlessBudgetOpen + EndlessBudgetStep * w;
            if (boss) budget *= EndlessEscort;

            var roster = new List<EnemyId>();
            for (int t = 0; t < EndlessTiers.Length; t++)
                if (w >= EndlessTierFrom[t]) roster.AddRange(EndlessTiers[t]);

            var ids = new List<EnemyId>();
            var groups = new List<int>();
            var used = new Dictionary<EnemyId, int>();
            float sum = 0f;
            int heads = 0, guard = 0;
            int cap = boss ? EndlessBodyCap / 2 : EndlessBodyCap;
            while (sum < budget && heads < cap && guard++ < 600)
            {
                EnemyId id = roster[rng.Next(roster.Count)];
                int limit = EndlessHeavyCap(id, w);
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

            float span = EndlessSpanOpen + EndlessSpanStep * w;
            float open = boss ? 2.4f : 0.4f;
            float room = Mathf.Max(1f, span - EndlessTail - open);
            int cols = GameConstants.Columns;
            var specs = new List<SpawnSpec>();
            var counts = new List<int>();
            if (boss)
            {
                EnemyId b = EndlessBosses[Mathf.Clamp((w + 1) / EndlessBossEvery - 1, 0, EndlessBosses.Length - 1)];
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
                int col = (w + SpreadOff(i % cols) + i / cols) % cols;
                specs.Add(new SpawnSpec(t, ids[i], col, groups[i]));
                counts.Add(groups[i]);
            }
            float dur = boss ? Mathf.Max(22f, span + 8f) : span;
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

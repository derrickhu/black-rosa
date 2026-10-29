using System.Collections.Generic;
using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 「招财进宝」的三档活动关。两排全开。每档自带血量、怪量和移速档位（见 EventCatalog.Tiers），
    // 不借主线某章某关的数值，后续调主线第五章以后的怪量和血量不会牵动活动。
    // 难度加在只数上，不加快怪。
    // 波数写死成 6 / 8 / 10。只数按 EventCounts 从疏到密，最后一波再用 EventPeak 抬一档。
    // 同一拍的怪散开到各列，不再排成一列小队。前两波一拍四只，之后一拍五只，
    // 倒数第二波一拍十只，最后一波一拍十五只。波和波之间不留空档。
    // 不进主线表，也不跟主线章节曲线做比值绑定。
    public static partial class StageCatalog
    {
        // 抽牌定价只吃钱袋七成，剩下三成留着存。主线是八成五。
        const float EventDraftShare = 0.7f;

        // 庙会青石街，三档共用一张。
        public const string EventBackdrop = "Bg/battle_bg_event";

        // 开局密度和终盘波峰。改这里要同步 check_stages。
        const float EventRampOpen = 0.72f;
        const int EventEaseWaves = 0;
        const float EventEase = 1f;
        // 一拍出几只。倒数第二波、最后一波用列数当倍数，出场时再散到六列。
        const int EventOpenWaves = 2;
        const int EventOpenBatch = 4;
        const int EventBatch = 5;
        const float EventBeat = 4.0f;
        const int EventLateWaves = 2;
        const int EventLateLanes = 2;
        const int EventFinaleLanes = 3;
        // 最后一波的密度。主线关底是 2，活动没有 boss，靠这一档把终盘堆满。
        const float EventPeak = 3.2f;

        public static StageDef EventStage(int tier, CardId[] pool)
        {
            EventTierDef t = EventCatalog.Tier(tier);
            var s = new StageDef
            {
                Index = -1,
                Chapter = t.SpeedChapter,
                Slot = tier,
                Title = t.Name,
                Name = EventCatalog.Title + " · " + t.Name,
                Mask = G("XXXXXX", "XXXXXX"),
                Pool = pool,
                Fresh = new CardId[0],
                Waves = EventWaves(tier),
                Hp = t.Hp,
                StaminaCost = 0,
                EventTier = tier
            };
            s.Bodies = EventCounts(s.Waves, t.Crowd);
            EventStream(s);
            Price(s, EventDraftShare);
            return s;
        }

        // 活动关自己的摊怪：整关总血按「波长 × 密度」分到每波，再按该波平均血换成只数。
        // 不读主线 ChapterBodies，也不走主线的开局补对和 ChapterPress。
        static int[][] EventCounts(WaveDef[] waves, float crowd)
        {
            int nW = waves.Length;
            var raw = new float[nW][];
            var floor = new int[nW][];
            var waveRaw = new float[nW];
            var waveHp = new float[nW];
            var waveFloor = new int[nW];
            float hpSum = 0f;
            for (int w = 0; w < nW; w++)
            {
                SpawnSpec[] list = waves[w].Spawns;
                raw[w] = new float[list.Length];
                floor[w] = new int[list.Length];
                for (int k = 0; k < list.Length; k++)
                {
                    int count = list[k].Count * EnemyCatalog.Density(list[k].Id);
                    raw[w][k] = count;
                    floor[w][k] = count >= 1 ? 1 : 0;
                    float hp = count * EnemyCatalog.Base(list[k].Id).Hp;
                    waveRaw[w] += count;
                    waveHp[w] += hp;
                    waveFloor[w] += floor[w][k];
                    hpSum += hp;
                }
            }
            int last = nW - 1;
            float pre = 0f;
            for (int w = 0; w < last; w++) pre += Mathf.Max(0.01f, waves[w].Duration);
            var weight = new float[nW];
            float wSum = 0f, cursor = 0f;
            for (int w = 0; w < nW; w++)
            {
                float dur = Mathf.Max(0.01f, waves[w].Duration);
                float lvl;
                if (w == last && nW > 1) lvl = EventPeak;
                else
                {
                    float u = pre > 0.01f ? Mathf.Clamp01((cursor + dur * 0.5f) / pre) : 0.5f;
                    lvl = EventRampOpen + (RampLate - EventRampOpen) * u;
                    if (w < EventEaseWaves) lvl *= EventEase;
                }
                weight[w] = waveRaw[w] > 0f ? dur * lvl : 0f;
                wSum += weight[w];
                cursor += dur;
            }
            float hpGoal = hpSum * crowd;
            var bodies = new float[nW];
            float bodySum = 0f;
            for (int w = 0; w < nW; w++)
            {
                if (weight[w] <= 0f || wSum <= 0f) continue;
                bodies[w] = hpGoal * weight[w] / wSum / (waveHp[w] / waveRaw[w]);
                bodySum += bodies[w];
            }
            int total = Mathf.Max(1, Mathf.RoundToInt(bodySum));
            int[] perWave = Share(total, bodies, waveFloor);
            var grid = new int[nW][];
            for (int w = 0; w < nW; w++)
                grid[w] = Share(perWave[w], raw[w], floor[w]);
            return grid;
        }

        // 活动怪只用 EventCast 这八种，全都换了「招财」的皮（evt_*.png）。
        public static readonly EnemyId[] EventCast = { Walker, Ball, Chubby, BigHead, Runner, Swarm, Shield, Elite };

        // 摊完只数之后按拍上场。出场表里的时间不再算数，只留兵种的先后。
        // 同一拍先铺满六列，再在已有的列后面排第二只。
        static void EventStream(StageDef s)
        {
            int nW = s.Waves.Length;
            int cols = GameConstants.Columns;
            var waves = new WaveDef[nW];
            var grid = new int[nW][];
            for (int w = 0; w < nW; w++)
            {
                int lanes = w == nW - 1 ? EventFinaleLanes
                    : w >= nW - EventLateWaves ? EventLateLanes : 1;
                int batch = w < EventOpenWaves ? EventOpenBatch : EventBatch;
                EnemyId[] ids = Spread(s.Waves[w].Spawns, s.Bodies[w]);
                var specs = new List<SpawnSpec>();
                var counts = new List<int>();
                int cap = batch * lanes;
                int beats = Mathf.Max(1, Mathf.CeilToInt(ids.Length / (float)Mathf.Max(1, cap)));
                // 尾巴不够再占一拍时并回去，避免单独一拍只剩一只。
                if (beats > 1 && ids.Length - (beats - 1) * cap < lanes)
                    beats--;
                int baseN = ids.Length / beats;
                int extra = ids.Length % beats;
                int cursor = 0;
                for (int beat = 0; beat < beats; beat++)
                {
                    int need = baseN + (beat < extra ? 1 : 0);
                    float t = 0.2f + beat * EventBeat;
                    int origin = (w + beat) % cols;
                    var bucket = new List<EnemyId>[cols];
                    for (int c = 0; c < cols; c++) bucket[c] = new List<EnemyId>();
                    for (int i = 0; i < need && cursor < ids.Length; i++, cursor++)
                    {
                        int col = (origin + SpreadOff(i % cols)) % cols;
                        bucket[col].Add(ids[cursor]);
                    }
                    for (int c = 0; c < cols; c++)
                    {
                        if (bucket[c].Count == 0) continue;
                        AddSquad(specs, counts, bucket[c].ToArray(), 0, bucket[c].Count, t, c);
                    }
                }
                int beatCount = beats;
                float last = beatCount == 0 ? 0f : 0.2f + (beatCount - 1) * EventBeat;
                waves[w] = new WaveDef(Mathf.Max(EventBeat, last + EventBeat - 0.2f), specs.ToArray());
                grid[w] = counts.ToArray();
            }
            s.Waves = waves;
            s.Bodies = grid;
        }

        // 隔列铺开：0、3、1、4、2、5。同一拍先占不同列，满了再回到这些列的后面。
        static int SpreadOff(int k)
        {
            int half = GameConstants.Columns / 2;
            return k % 2 == 0 ? k / 2 : half + k / 2;
        }

        // 这一列里相同的兵种并成一条。不同兵种仍是同一时刻。
        static void AddSquad(List<SpawnSpec> specs, List<int> counts, EnemyId[] ids, int from, int take, float time, int col)
        {
            int end = from + take;
            for (int i = from; i < end; i++)
            {
                int n = 1;
                EnemyId id = ids[i];
                while (i + n < end && ids[i + n] == id) n++;
                specs.Add(new SpawnSpec(time, id, col, n));
                counts.Add(n);
                i += n - 1;
            }
        }

        // 各组轮流出，避免同一波里先把一种怪排完再换下一种。
        static EnemyId[] Spread(SpawnSpec[] src, int[] counts)
        {
            int total = 0;
            for (int i = 0; i < counts.Length; i++) total += counts[i];
            var ids = new EnemyId[total];
            var left = new int[counts.Length];
            for (int i = 0; i < counts.Length; i++) left[i] = counts[i];
            int at = 0, k = 0, guard = 0;
            while (at < total && guard++ < total * 4)
            {
                if (left[k] > 0)
                {
                    left[k]--;
                    ids[at++] = src[k].Id;
                }
                k = (k + 1) % left.Length;
            }
            return ids;
        }

        // 出场表里的只数只看相对多少，真正刷几只由 RampCounts 按整关总血摊。
        static WaveDef[] EventWaves(int tier)
        {
            switch (tier)
            {
                // 小集：只出纯墨兵和快脚、墨粒。
                case 0:
                    return new[]
                    {
                        W(S(1.2f, Walker), S(6.0f, Ball), S(10.8f, Walker)),
                        W(S(0.8f, Ball), S(5.4f, Chubby), S(10.0f, Walker)),
                        W(S(0.5f, Swarm), S(3.6f, Walker, -1, 2), S(6.8f, BigHead), S(9.8f, Ball, -1, 2)),
                        W(S(0.4f, Runner, -1, 2), S(2.8f, Swarm), S(5.4f, Chubby, -1, 2), S(8.0f, Walker, -1, 2)),
                        W(S(0.3f, Swarm), S(2.2f, BigHead, -1, 2), S(4.4f, Runner, -1, 2), S(6.6f, Ball, -1, 3),
                            S(8.4f, Chubby)),
                        W(S(0.3f, Swarm, 1), S(0.3f, Swarm, 4), S(2.2f, Chubby, -1, 2), S(4.2f, Runner, -1, 3),
                            S(6.2f, BigHead, -1, 2), S(8f, Swarm, 2))
                    };
                // 庙会：盾从第三波才上场，而且先一只一只来。
                case 1:
                    return new[]
                    {
                        W(S(1.2f, Walker), S(6.0f, Ball), S(10.8f, Runner)),
                        W(S(0.8f, Ball), S(5.4f, Walker), S(10.0f, Chubby)),
                        W(S(0.6f, Shield), S(4.6f, Swarm), S(8.6f, Walker, -1, 2)),
                        W(S(0.4f, Swarm), S(3.0f, Shield, -1, 2), S(5.8f, Runner, -1, 2), S(8.6f, Chubby)),
                        W(S(0.4f, Chubby, -1, 2), S(2.8f, Shield, -1, 2), S(5.4f, BigHead, -1, 2), S(8.0f, Swarm)),
                        W(S(0.3f, Swarm, 1), S(2.4f, Shield, -1, 2), S(4.8f, Runner, -1, 2), S(7.2f, Walker, -1, 2)),
                        W(S(0.3f, Runner, -1, 2), S(2.2f, Chubby, -1, 2), S(4.2f, Shield, -1, 3), S(6.4f, BigHead, -1, 2),
                            S(8.2f, Swarm, 2)),
                        W(S(0.3f, Swarm, 1), S(0.3f, Swarm, 4), S(2.2f, Shield, -1, 3), S(4.4f, Runner, -1, 3),
                            S(6.4f, Chubby, -1, 2), S(8.2f, BigHead, -1, 2))
                    };
                // 大集：墨尊从第五波才上场。
                default:
                    return new[]
                    {
                        W(S(1.2f, Walker), S(6.0f, Ball), S(10.8f, Runner)),
                        W(S(0.8f, Shield), S(5.4f, Walker), S(10.0f, Ball)),
                        W(S(0.6f, Swarm), S(4.2f, Runner, -1, 2), S(8.0f, Shield)),
                        W(S(0.4f, Chubby, -1, 2), S(3.0f, Shield, -1, 2), S(5.8f, BigHead, -1, 2), S(8.6f, Swarm)),
                        W(S(0.4f, Swarm), S(2.8f, Elite), S(5.6f, Runner, -1, 2), S(8.4f, Walker, -1, 2)),
                        W(S(0.3f, Shield, -1, 2), S(2.6f, Elite), S(5.2f, Walker, -1, 3), S(7.8f, Swarm)),
                        W(S(0.3f, Swarm, 1), S(2.4f, Elite), S(4.8f, Chubby, -1, 2), S(7.2f, Runner, -1, 2)),
                        W(S(0.3f, Runner, -1, 3), S(2.2f, Shield, -1, 2), S(4.4f, Elite), S(6.6f, BigHead, -1, 2),
                            S(8.4f, Swarm, 2)),
                        W(S(0.3f, Swarm, 1), S(0.3f, Swarm, 4), S(2.2f, Elite, -1, 2), S(4.4f, Shield, -1, 2),
                            S(6.6f, Runner, -1, 2), S(8.6f, Chubby, -1, 2)),
                        W(S(0.3f, Swarm, 1), S(0.3f, Swarm, 4), S(2.2f, Elite, -1, 2), S(4.2f, Shield, -1, 3),
                            S(6.0f, Chubby, -1, 2), S(7.6f, BigHead, -1, 2), S(9.2f, Runner, -1, 3))
                    };
            }
        }
    }
}

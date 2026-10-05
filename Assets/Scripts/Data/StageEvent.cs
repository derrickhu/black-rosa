using UnityEngine;
using static InkLine.EnemyId;

namespace InkLine
{
    // 「招财进宝」的三档活动关。两排全开，血量和速度借主线某一关。
    // 波数写死成 6 / 8 / 10。出场表只定顺序和每组几只，真正的节奏在 EventSpread：
    // 一只一只隔开出，前几波隔得更开，每波都留一段尾，棋盘才有时间铺开升星。
    // 不进 72 关的表，也不进 check_stages 的主线曲线。
    public static partial class StageCatalog
    {
        // 抽牌定价只吃钱袋七成，剩下三成留着存。主线是八成五。
        const float EventDraftShare = 0.7f;

        // 庙会青石街，三档共用一张。
        public const string EventBackdrop = "Bg/battle_bg_event";

        public static StageDef EventStage(int tier, CardId[] pool)
        {
            EventTierDef t = EventCatalog.Tier(tier);
            var s = new StageDef
            {
                Index = -1,
                Chapter = t.Chapter,
                Slot = t.Slot,
                Title = t.Name,
                Name = EventCatalog.Title + " · " + t.Name,
                Mask = G("XXXXXX", "XXXXXX"),
                Pool = pool,
                Fresh = new CardId[0],
                Waves = EventSpread(EventWaves(tier)),
                Hp = HpOf(t.Chapter, t.Slot, false),
                StaminaCost = 0,
                EventTier = tier
            };
            s.Bodies = EventBodies(s.Waves);
            Price(s, EventDraftShare);
            return s;
        }

        // 活动怪只用 EventCast 这八种，全都换了「招财」的皮（evt_*.png）。
        public static readonly EnemyId[] EventCast = { Walker, Ball, Chubby, BigHead, Runner, Swarm, Shield, Elite };

        // 第一波隔这么久出一只，最后一波收到这么久。改这里要同步 check_stages。
        const float EventGapOpen = 2.8f;
        const float EventGapLate = 1.8f;
        const float EventWaveTail = 14f;
        const float EventWaveMin = 24f;

        // 出场表里的时间点不算数，只保留组的先后。密度乘完之后拆成一只一个时间，
        // 间隔从疏收到密，但再密也要隔开一秒以上，不能在同一帧倒一堆。
        static WaveDef[] EventSpread(WaveDef[] waves)
        {
            var outp = new WaveDef[waves.Length];
            for (int w = 0; w < waves.Length; w++)
            {
                float u = waves.Length <= 1 ? 1f : w / (float)(waves.Length - 1);
                float gap = Mathf.Lerp(EventGapOpen, EventGapLate, u);
                SpawnSpec[] src = waves[w].Spawns;
                int n = 0;
                for (int k = 0; k < src.Length; k++)
                    n += src[k].Count * EnemyCatalog.Density(src[k].Id);
                var list = new SpawnSpec[n];
                float t = 1.5f;
                int at = 0;
                for (int k = 0; k < src.Length; k++)
                {
                    int c = src[k].Count * EnemyCatalog.Density(src[k].Id);
                    for (int i = 0; i < c; i++)
                    {
                        list[at++] = new SpawnSpec(t, src[k].Id, src[k].Column, 1);
                        t += gap;
                    }
                }
                float last = n == 0 ? 0f : list[n - 1].Time;
                outp[w] = new WaveDef(Mathf.Max(EventWaveMin, last + EventWaveTail), list);
            }
            return outp;
        }

        // 只数已经在 EventSpread 里拆开，每一条就是这一下出的一只。
        static int[][] EventBodies(WaveDef[] waves)
        {
            var grid = new int[waves.Length][];
            for (int w = 0; w < waves.Length; w++)
            {
                SpawnSpec[] list = waves[w].Spawns;
                grid[w] = new int[list.Length];
                for (int k = 0; k < list.Length; k++)
                    grid[w][k] = list[k].Count;
            }
            return grid;
        }

        static WaveDef[] EventWaves(int tier)
        {
            switch (tier)
            {
                // 小集：4、6、10、16、26、42。
                case 0:
                    return new[]
                    {
                        W(S(0.5f, Walker), S(3.4f, Ball)),
                        W(S(0.4f, Walker), S(2.8f, Ball), S(5.2f, Chubby)),
                        W(S(0.4f, Swarm, -1, 2), S(2.6f, Walker), S(5f, Ball)),
                        W(S(0.3f, Swarm, -1, 2), S(2.2f, Walker, -1, 2), S(4.4f, BigHead, -1, 2), S(6.6f, Ball)),
                        W(S(0.3f, Swarm, -1, 2), S(1.8f, Runner, -1, 3), S(3.4f, Chubby, -1, 2),
                            S(5f, Walker, -1, 2), S(6.6f, BigHead, -1, 2), S(8.2f, Ball)),
                        W(S(0.3f, Swarm, -1, 4), S(1.6f, Runner, -1, 4), S(3f, Walker, -1, 4),
                            S(4.6f, BigHead, -1, 3), S(6.2f, Chubby, -1, 2), S(7.8f, Ball, -1, 2))
                    };
                // 庙会：4、6、8、12、16、22、30、42。盾从第三波才上场。
                case 1:
                    return new[]
                    {
                        W(S(0.5f, Walker), S(3.4f, Runner)),
                        W(S(0.4f, Walker), S(2.8f, Ball), S(5.2f, Chubby)),
                        W(S(0.4f, Shield), S(2.2f, Walker), S(4f, Swarm), S(5.8f, Chubby)),
                        W(S(0.3f, Shield, -1, 2), S(2f, Swarm, -1, 2), S(4.2f, Runner, -1, 2)),
                        W(S(0.3f, Swarm, -1, 2), S(1.8f, Shield, -1, 2), S(3.6f, BigHead, -1, 2), S(5.4f, Chubby, -1, 2)),
                        W(S(0.3f, Swarm, -1, 3), S(1.8f, Runner, -1, 3), S(3.4f, Shield, -1, 3), S(5.2f, Walker, -1, 2)),
                        W(S(0.3f, Swarm, -1, 4), S(1.6f, Chubby, -1, 3), S(3.2f, Shield, -1, 4),
                            S(4.8f, BigHead, -1, 2), S(6.4f, Runner, -1, 2)),
                        W(S(0.3f, Swarm, -1, 5), S(1.6f, Runner, -1, 4), S(3f, Shield, -1, 5),
                            S(4.6f, BigHead, -1, 3), S(6.2f, Chubby, -1, 3), S(7.8f, Walker))
                    };
                // 大集：6、8、10、12、16、20、24、28、36、42。墨尊从第五波才上场。
                default:
                    return new[]
                    {
                        W(S(0.4f, Walker), S(2.4f, Runner), S(4.4f, Ball)),
                        W(S(0.4f, Shield), S(2f, Walker), S(3.6f, Swarm), S(5.4f, Chubby)),
                        W(S(0.3f, Swarm), S(1.8f, Runner, -1, 2), S(3.6f, Shield), S(5.2f, Walker)),
                        W(S(0.3f, Swarm, -1, 2), S(1.8f, Shield, -1, 2), S(3.6f, BigHead), S(5.2f, Runner)),
                        W(S(0.3f, Swarm, -1, 2), S(1.6f, Elite, -1, 2), S(3.2f, Runner, -1, 2),
                            S(4.8f, Chubby), S(6.4f, Walker)),
                        W(S(0.3f, Swarm, -1, 3), S(1.6f, Elite, -1, 2), S(3.2f, Shield, -1, 3),
                            S(4.8f, Walker, -1, 2), S(6.4f, Ball)),
                        W(S(0.3f, Swarm, -1, 3), S(1.5f, Runner, -1, 2), S(3f, Elite, -1, 2),
                            S(4.4f, Shield, -1, 3), S(5.8f, Chubby, -1, 2), S(7.2f, BigHead)),
                        W(S(0.3f, Swarm, -1, 4), S(1.5f, Runner, -1, 3), S(3f, Elite, -1, 2),
                            S(4.4f, Shield, -1, 4), S(5.8f, Chubby, -1, 2)),
                        W(S(0.3f, Swarm, -1, 5), S(1.4f, Runner, -1, 4), S(2.8f, Elite, -1, 3),
                            S(4.2f, Shield, -1, 4), S(5.6f, Chubby, -1, 3)),
                        W(S(0.3f, Swarm, -1, 5), S(1.4f, Runner, -1, 4), S(2.8f, Elite, -1, 5),
                            S(4.2f, BigHead, -1, 3), S(5.6f, Shield, -1, 4), S(7f, Chubby, -1, 2))
                    };
            }
        }
    }
}

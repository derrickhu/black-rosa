using static InkLine.EnemyId;

namespace InkLine
{
    // 「招财进宝」的三档活动关。两排全开，血量、速度、波长和出怪折扣借主线某一关，
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
                Waves = Pace(EventWaves(tier), t.Chapter),
                Hp = HpOf(t.Chapter, t.Slot, false),
                StaminaCost = 0,
                EventTier = tier
            };
            s.Bodies = RampCounts(s.Waves, t.Chapter);
            Price(s, EventDraftShare);
            return s;
        }

        // 活动怪只用 EventCast 这八种，全都换了「招财」的皮（evt_*.png）。
        public static readonly EnemyId[] EventCast = { Walker, Ball, Chubby, BigHead, Runner, Swarm, Shield, Elite };

        static WaveDef[] EventWaves(int tier)
        {
            switch (tier)
            {
                case 0:
                    return new[]
                    {
                        W(S(0.3f, Walker, -1, 3), S(3f, Ball, -1, 2), S(6.5f, Chubby, -1, 3)),
                        W(S(0.3f, Runner, -1, 3), S(3.5f, Walker, -1, 4), S(7f, BigHead, -1, 3)),
                        W(S(0.3f, Swarm, -1, 3), S(3f, Chubby, -1, 3), S(6f, BigHead, -1, 3), S(8.5f, Ball, -1, 3)),
                        W(S(0.3f, Runner, -1, 3), S(3f, Walker, -1, 4), S(6f, Runner, -1, 4), S(9f, BigHead, -1, 4))
                    };
                case 1:
                    return new[]
                    {
                        W(S(0.3f, Walker, -1, 3), S(3f, Runner, -1, 2), S(6.5f, Chubby, -1, 2)),
                        W(S(0.3f, Shield, -1, 2), S(3.5f, Ball, -1, 2), S(7f, BigHead, -1, 3)),
                        W(S(0.3f, Swarm, -1, 2), S(3f, Runner, -1, 2), S(6f, BigHead, -1, 2), S(8.5f, Shield, -1, 2)),
                        W(S(0.3f, Swarm, -1, 2), S(3f, Chubby, -1, 2), S(6f, Shield, -1, 2), S(9f, Runner, -1, 3)),
                        W(S(0.3f, Chubby, -1, 3), S(3f, Shield, -1, 2), S(6f, Shield, -1, 3), S(9f, BigHead, -1, 3))
                    };
                default:
                    return new[]
                    {
                        W(S(0.3f, Walker, -1, 3), S(3f, Runner, -1, 2), S(6.5f, Chubby, -1, 2)),
                        W(S(0.3f, Shield, -1, 2), S(3.5f, Ball, -1, 2), S(7f, Shield, -1, 2)),
                        W(S(0.3f, Swarm, -1, 2), S(3f, Runner, -1, 2), S(6f, Elite, -1, 1), S(8.5f, Runner, -1, 2)),
                        W(S(0.3f, Swarm, -1, 2), S(3f, Elite, -1, 1), S(6f, Shield, -1, 2), S(9f, Runner, -1, 2)),
                        W(S(0.3f, Chubby, -1, 2), S(3f, Shield, -1, 2), S(6f, BigHead, -1, 2), S(9f, BigHead, -1, 3)),
                        W(S(0.3f, Elite, -1, 1), S(3f, Shield, -1, 2), S(6f, Runner, -1, 2), S(9f, Elite, -1, 2))
                    };
            }
        }
    }
}

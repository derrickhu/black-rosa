using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第二章 · 土路：第二排从中间两格开到满，第一批带色的兵登场。
    public static partial class StageCatalog
    {
        static void Chapter2(List<Plan> s)
        {
            // 爬子是整局第一只带颜色的兵，推不动，只能正面打穿。
            s.Add(P("二层", Row(6, 2),
                W(S(0.3f, Walker, -1, 3), S(3f, BigHead, 2), S(6f, Ball, -1, 2)),
                W(S(0.3f, Crawler, 2), S(3.5f, Crawler, 3)),
                W(S(0.3f, Chubby, 1), S(2f, Crawler, 4), S(5.5f, Tall, -1, 2)),
                W(S(0.3f, Crawler, 0), S(1.6f, Crawler, 5), S(4.5f, Swarm, 3, 4), S(8f, BigHead, 2)),
                W(S(0.3f, Ball, -1, 4), S(3f, Crawler, -1, 2), S(7.5f, Chubby, 3))
            ).Give(Track));

            // 束墨专走空列。第二排只开中间三格，外侧天然没字。
            s.Add(P("束带", Row(6, 3),
                W(S(0.3f, Crawler, 1), S(2f, Crawler, 4), S(5.5f, Ball, -1, 3)),
                W(S(0.3f, Belt, 2), S(3.5f, Belt, 3)),
                W(S(0.3f, Belt, 0), S(1.8f, Belt, 5), S(5f, BigHead, -1, 2), S(8.5f, Tall, 3)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Crawler, -1, 2), S(7f, Swarm, 2, 4)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Ball, -1, 4), S(7.5f, BigHead, 3), S(10f, Crawler, 2))
            ));

            s.Add(P("快脚", Row(6, 3),
                W(S(0.3f, Belt, -1, 2), S(3f, Crawler, 3), S(6.5f, Tall, -1, 2)),
                W(S(0.3f, Runner, 2), S(2.5f, Runner, 3), S(5f, Runner, 1)),
                W(S(0.3f, Runner, 0), S(1.5f, Runner, 5), S(4.5f, Belt, 3), S(8.5f, Chubby, 2)),
                W(S(0.3f, Crawler, -1, 2), S(3f, Swarm, 4, 5), S(7.5f, BigHead, -1, 2)),
                W(S(0.3f, Runner, -1, 3), S(3.5f, Belt, -1, 2), S(8.5f, Ball, -1, 4))
            ).Give(Stun));

            // 击退成词的同时来一堵爬子墙 —— 推得动的推，推不动的正面打。
            s.Add(P("推墙", Row(6, 4),
                W(S(0.3f, Chubby, -1, 2), S(2.5f, Tall, -1, 2), S(5.5f, Runner, -1, 2)),
                W(S(0.3f, Crawler, 1), S(0.3f, Crawler, 2), S(0.3f, Crawler, 3), S(0.3f, Crawler, 4)),
                W(S(0.3f, BigHead, -1, 2), S(3f, Chubby, 2), S(3.5f, Chubby, 3), S(7f, Belt, -1, 2)),
                W(S(0.3f, Crawler, 0, 2), S(1.5f, Crawler, 5, 2), S(5f, Runner, -1, 3), S(8.5f, Swarm, 2, 5)),
                W(S(0.3f, Chubby, -1, 3), S(3f, Crawler, -1, 3), S(7.5f, Belt, -1, 2))
            ).Give(Strike, Back));

            // 小关底：鼓面带护卫重打一次。
            s.Add(P("鼓声", Row(6, 4),
                W(S(0.3f, Runner, -1, 2), S(2.5f, Crawler, -1, 2), S(6f, Belt, 3)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Swarm, -1, 5), S(6.5f, BigHead, -1, 2)),
                W(S(0.3f, Belt, 0), S(1.2f, Belt, 5), S(4f, Runner, -1, 3), S(7.5f, Tall, -1, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Ball, -1, 4), S(7f, Chubby, 3)),
                Boss(BossDrum, S(4f, Belt, 0), S(6f, Belt, 5), S(10f, Runner, -1, 2), S(14f, Crawler, 3))
            ));

            // 残局：第二排正中封了一格，横游偏偏在这两列来回晃。
            s.Add(P("横游", Row(6, 5),
                W(S(0.3f, Runner, -1, 2), S(3f, Belt, 2), S(6.5f, Crawler, 4)),
                W(S(0.3f, Strafer, 2), S(3.5f, Strafer, 3)),
                W(S(0.3f, Strafer, 0), S(1.8f, Strafer, 5), S(5f, Runner, -1, 2), S(9f, Chubby, 3)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Crawler, -1, 2), S(8f, Swarm, 3, 5)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Runner, -1, 3), S(8.5f, BigHead, -1, 2)),
                W(S(0.3f, Belt, 1), S(1.8f, Belt, 4), S(5.5f, Strafer, 3), S(9.5f, Ball, -1, 4))
            ).Holes("XXXXXX", "XX.XXX"));

            // 疾行：整关敌人快两成，金币也多两成，考摆位速度。
            s.Add(P("奔流", Row(6, 5),
                W(S(0.3f, Runner, -1, 3), S(3f, Ball, -1, 4), S(6.5f, Tall, -1, 2)),
                W(S(0.3f, Strafer, -1, 2), S(3f, Belt, -1, 2), S(6.5f, Runner, -1, 3)),
                W(S(0.3f, Crawler, -1, 2), S(2.5f, Swarm, -1, 5), S(6f, Strafer, 3), S(9f, Runner, -1, 2)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Belt, 1), S(3.8f, Belt, 4), S(7.5f, Ball, -1, 5)),
                W(S(0.3f, Runner, 0, 2), S(1.2f, Runner, 5, 2), S(4.5f, Strafer, -1, 2), S(8.5f, BigHead, -1, 2)),
                W(S(0.3f, Tall, -1, 3), S(3f, Crawler, -1, 3), S(7f, Runner, -1, 3))
            ).Rule(StageRule.Swift));

            // 孤城：基地只剩一血，关短一些，漏一只就输。
            s.Add(P("孤城", Row(6, 6),
                W(S(0.3f, Walker, -1, 3), S(3f, Chubby, 2), S(5.5f, Tall, 4)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Runner, 2), S(5.5f, Runner, 3)),
                W(S(0.3f, Belt, -1, 2), S(3f, Crawler, -1, 2), S(7f, Swarm, 3, 5)),
                W(S(0.3f, BigHead, -1, 2), S(3f, Strafer, 1), S(4.5f, Strafer, 4), S(8f, Chubby, -1, 2))
            ).Rule(StageRule.Frail));

            s.Add(P("墨囊", Row(6, 6),
                W(S(0.3f, Strafer, -1, 2), S(3f, Runner, -1, 2), S(7f, Belt, 3)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Chubby, -1, 2), S(8f, Swarm, 2, 5)),
                W(S(0.3f, Belt, 0), S(1.5f, Belt, 5), S(4.5f, Strafer, 3), S(8.5f, Runner, -1, 3)),
                W(S(0.3f, BigHead, -1, 3), S(3.5f, Ball, -1, 5), S(8f, Crawler, -1, 2)),
                W(S(0.3f, Strafer, 1), S(1.5f, Strafer, 4), S(5f, Belt, -1, 2), S(9f, Tall, -1, 3)),
                W(S(0.3f, Runner, -1, 4), S(3.5f, Chubby, -1, 3), S(8f, Crawler, 2), S(8.6f, Crawler, 3)),
                Boss(BossInkbag, S(4f, Strafer, 0), S(6f, Strafer, 5), S(10f, Runner, -1, 3), S(14f, Belt, -1, 2), S(19f, Swarm, -1, 5))
            ));
        }
    }
}

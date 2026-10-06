using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第二章 · 土路：两排，第二排只开一小截，位置每关换。成群的墨粒和第一批带色的兵登场；
    // 炸进池之后，炸和火叠在同一列就是烈爆。
    public static partial class StageCatalog
    {
        static void Chapter2(List<Plan> s)
        {
            // 墨粒一团一团来，炸一发清一片。
            s.Add(P("墨群", G("XXXXXX", "...X.."),
                W(S(0.3f, Walker, -1, 3), S(3f, BigHead, 2), S(6.5f, Ball, -1, 2)),
                W(S(0.3f, Swarm, 2, 2), S(4f, Swarm, 3, 2)),
                W(S(0.3f, Chubby, 1), S(2.5f, Swarm, 4, 2), S(6f, Tall, -1, 2)),
                W(S(0.3f, Ball, -1, 3), S(3.5f, Swarm, -1, 2), S(7.5f, BigHead, 3))
            ).Give(Explode));

            // 爬子推不动，只能正面打穿。
            s.Add(P("爬墨", G(".XXXX.", ".XXXX."),
                W(S(0.3f, Walker, -1, 3), S(3f, BigHead, 2), S(6f, Ball, -1, 2)),
                W(S(0.3f, Crawler, 2), S(3.5f, Crawler, 3)),
                W(S(0.3f, Chubby, 1), S(2f, Crawler, 4), S(5.5f, Tall, -1, 2)),
                W(S(0.3f, Crawler, 0), S(1.6f, Crawler, 5), S(4.5f, Swarm, 3, 3), S(8f, BigHead, 2)),
                W(S(0.3f, Ball, -1, 3), S(3f, Crawler, -1, 2), S(7.5f, Chubby, 3))
            ));

            // 束墨专挑没字的列走。第二排只开两侧两格，中间那几列天然是空的。
            s.Add(P("束带", G("XXXXXX", ".X..X."),
                W(S(0.3f, Crawler, 1), S(2f, Crawler, 4), S(5.5f, Ball, -1, 3)),
                W(S(0.3f, Belt, 2), S(3.5f, Belt, 3)),
                W(S(0.3f, Belt, 0), S(1.8f, Belt, 5), S(5f, BigHead, -1, 2), S(8.5f, Tall, 3)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Crawler, -1, 2), S(7f, Swarm, 2, 3)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Ball, -1, 3), S(7.5f, BigHead, 3), S(10f, Crawler, 2))
            ).Give(Accel));

            // 中间两列整条封死，快脚只会从两边冲过来。
            s.Add(P("快脚", G("XX..XX", "XX..XX"),
                W(S(0.3f, Belt, -1, 2), S(3f, Crawler, 3), S(6.5f, Tall, -1, 2)),
                W(S(0.3f, Runner, 2), S(2.5f, Runner, 3), S(5f, Runner, 1)),
                W(S(0.3f, Runner, 0), S(1.5f, Runner, 5), S(4.5f, Belt, 3), S(8.5f, Chubby, 2)),
                W(S(0.3f, Crawler, -1, 2), S(3f, Swarm, 4, 3), S(7.5f, BigHead, -1, 2)),
                W(S(0.3f, Runner, -1, 3), S(3.5f, Belt, -1, 2), S(8.5f, Ball, -1, 3))
            ));

            // 小关底：鼓面带护卫重打一次。
            s.Add(P("鼓声", G("XXXXXX", "..XX.."),
                W(S(0.3f, Runner, -1, 2), S(2.5f, Crawler, -1, 2), S(6f, Belt, 3)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Swarm, -1, 3), S(6.5f, BigHead, -1, 2)),
                W(S(0.3f, Belt, 0), S(1.2f, Belt, 5), S(4f, Runner, -1, 3), S(7.5f, Tall, -1, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Ball, -1, 3), S(7f, Chubby, 3)),
                Boss(BossDrum, S(4f, Belt, 0), S(6f, Belt, 5), S(10f, Runner, -1, 2), S(14f, Crawler, 3))
            ));

            // 横游来回换列，追踪的子弹会自己拐过去。
            s.Add(P("横游", G("XXXXX.", ".XXX.."),
                W(S(0.3f, Runner, -1, 2), S(3f, Belt, 2), S(6.5f, Crawler, 4)),
                W(S(0.3f, Strafer, 2), S(3.5f, Strafer, 3)),
                W(S(0.3f, Strafer, 0), S(1.8f, Strafer, 5), S(5f, Runner, -1, 2), S(9f, Chubby, 3)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Crawler, -1, 2), S(8f, Swarm, 3, 3)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Runner, -1, 3), S(8.5f, BigHead, -1, 2)),
                W(S(0.3f, Belt, 1), S(1.8f, Belt, 4), S(5.5f, Strafer, 3), S(9.5f, Ball, -1, 3))
            ).Give(Track));

            s.Add(P("奔流", G(".XXXXX", "..XXX."),
                W(S(0.3f, Runner, -1, 3), S(3f, Ball, -1, 3), S(6.5f, Tall, -1, 2)),
                W(S(0.3f, Strafer, -1, 2), S(3f, Belt, -1, 2), S(6.5f, Runner, -1, 3)),
                W(S(0.3f, Crawler, -1, 2), S(2.5f, Swarm, -1, 3), S(6f, Strafer, 3), S(9f, Runner, -1, 2)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Belt, 1), S(3.8f, Belt, 4), S(7.5f, Ball, -1, 3)),
                W(S(0.3f, Runner, 0, 2), S(1.2f, Runner, 5, 2), S(4.5f, Strafer, -1, 2), S(8.5f, BigHead, -1, 2))
            ));

            // 一堵爬子墙压过来，左边三列叠得厚，右边只有一排。
            s.Add(P("推墙", G("XXXXXX", "XX...."),
                W(S(0.3f, Chubby, -1, 2), S(2.5f, Tall, -1, 2), S(5.5f, Runner, -1, 2)),
                W(S(0.3f, Crawler, 1), S(0.3f, Crawler, 2), S(0.3f, Crawler, 3), S(0.3f, Crawler, 4)),
                W(S(0.3f, BigHead, -1, 2), S(3f, Chubby, 2), S(3.5f, Chubby, 3), S(7f, Belt, -1, 2)),
                W(S(0.3f, Crawler, 0, 2), S(1.5f, Crawler, 5, 2), S(5f, Runner, -1, 3), S(8.5f, Swarm, 2, 3)),
                W(S(0.3f, Chubby, -1, 3), S(3f, Crawler, -1, 3), S(7.5f, Belt, -1, 2))
            ));

            s.Add(P("墨囊", G("XXXXXX", ".XXX.."),
                W(S(0.3f, Strafer, -1, 2), S(3f, Runner, -1, 2), S(7f, Belt, 3)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Chubby, -1, 2), S(8f, Swarm, 2, 3)),
                W(S(0.3f, Belt, 0), S(1.5f, Belt, 5), S(4.5f, Strafer, 3), S(8.5f, Runner, -1, 3)),
                W(S(0.3f, BigHead, -1, 3), S(3.5f, Ball, -1, 4), S(8f, Crawler, -1, 2)),
                W(S(0.3f, Strafer, 1), S(1.5f, Strafer, 4), S(5f, Belt, -1, 2), S(9f, Tall, -1, 3)),
                Boss(BossInkbag, S(4f, Strafer, 0), S(6f, Strafer, 5), S(10f, Runner, -1, 3), S(14f, Belt, -1, 2), S(19f, Swarm, -1, 3))
            ));
        }
    }
}

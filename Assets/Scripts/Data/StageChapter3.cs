using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第三章 · 石院：两排慢慢铺满，第三排还不开。晕和第一组成词「击退」进池，
    // 盾、裂墨、狂奔登场 —— 这一章开始，光靠一种字打不动了。
    public static partial class StageCatalog
    {
        static void Chapter3(List<Plan> s)
        {
            // 举盾的正面硬，晕住了才好打。
            s.Add(P("盾阵", G("XXXXXX", ".XXXX."),
                W(S(0.3f, Strafer, -1, 2), S(3f, Runner, -1, 2), S(7f, Belt, 3)),
                W(S(0.3f, Shield, 2), S(3f, Shield, 3)),
                W(S(0.3f, Shield, 0), S(1.8f, Shield, 5), S(5.5f, Crawler, -1, 2), S(9.5f, Runner, -1, 2)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Strafer, -1, 2), S(8.5f, Swarm, 2, 4)),
                W(S(0.3f, Shield, -1, 2), S(4f, Chubby, -1, 2), S(8.5f, Runner, -1, 3)),
                W(S(0.3f, Crawler, 1), S(1.5f, Crawler, 4), S(5f, Shield, 2), S(5.6f, Shield, 3), S(9.5f, Ball, -1, 4))
            ).Give(Stun));

            // 一大一小两种肉轮着来，重字和晕字各管一头。
            s.Add(P("肉山", G("XXXXXX", "XX..XX"),
                W(S(0.3f, Chubby, -1, 2), S(3f, Shield, 3), S(6.5f, Runner, -1, 2)),
                W(S(0.3f, BigHead, 2, 2), S(3f, Chubby, 3, 2), S(6.5f, Belt, -1, 2)),
                W(S(0.3f, Shield, 1), S(1.5f, Shield, 4), S(5f, Strafer, -1, 2), S(9f, Crawler, -1, 2)),
                W(S(0.3f, Chubby, -1, 3), S(3.5f, Swarm, -1, 4), S(8f, BigHead, -1, 2)),
                W(S(0.3f, Belt, 0), S(1.2f, Belt, 5), S(4.5f, Shield, -1, 2), S(8.5f, Runner, -1, 3)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Chubby, 2), S(4.1f, Chubby, 3), S(8f, Strafer, -1, 2))
            ));

            // 击退：一上一下摆在同一列就成词。大头和长墨排成串顶上来，推回去最解气。
            s.Add(P("击退", G(".XXXX.", "XXXXXX"),
                W(S(0.3f, Runner, -1, 3), S(3f, Chubby, 2), S(3.6f, Chubby, 3), S(7f, Belt, -1, 2)),
                W(S(0.3f, Splitter, -1, 2), S(3.5f, Crawler, 2), S(4.1f, Crawler, 3), S(8f, Swarm, -1, 4)),
                W(S(0.3f, BigHead, 2, 2), S(2.5f, Tall, 2, 2), S(5.5f, Shield, -1, 2), S(9f, Strafer, -1, 2)),
                W(S(0.3f, Chubby, -1, 3), S(3.5f, Runner, -1, 3), S(7.5f, Splitter, -1, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3f, Shield, 1), S(3.8f, Shield, 4), S(8f, Belt, -1, 2)),
                W(S(0.3f, Splitter, 2), S(1f, Splitter, 3), S(4.5f, BigHead, -1, 3), S(9f, Runner, -1, 3))
            ).Give(Strike, Back));

            s.Add(P("裂墨", G("XXXXXX", "XXX.XX"),
                W(S(0.3f, Shield, -1, 2), S(3f, Strafer, -1, 2), S(7f, Runner, -1, 2)),
                W(S(0.3f, Splitter, 2), S(3.5f, Splitter, 3)),
                W(S(0.3f, Splitter, 0), S(1.8f, Splitter, 5), S(5f, Crawler, -1, 2), S(9f, Strafer, 3)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Runner, -1, 3), S(8.5f, Chubby, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4.5f, Swarm, 2, 4), S(9f, BigHead, -1, 2)),
                W(S(0.3f, Strafer, 1), S(2f, Strafer, 4), S(5.5f, Splitter, 3), S(10f, Shield, -1, 2))
            ));

            s.Add(P("囊中", G("XXXXXX", "XXXXX."),
                W(S(0.3f, Shield, -1, 2), S(3f, Splitter, -1, 2), S(7f, Runner, -1, 2)),
                W(S(0.3f, Strafer, -1, 2), S(3f, Belt, -1, 2), S(7.5f, Swarm, 3, 4)),
                W(S(0.3f, Crawler, 1), S(1.5f, Crawler, 4), S(5f, Splitter, 3), S(9f, Chubby, -1, 2)),
                W(S(0.3f, Runner, -1, 3), S(3.5f, Shield, 2), S(4.1f, Shield, 3), S(8.5f, BigHead, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Strafer, -1, 2), S(8.5f, Ball, -1, 4)),
                Boss(BossInkbag, S(4f, Shield, 0), S(6f, Shield, 5), S(10f, Splitter, -1, 2), S(15f, Runner, -1, 3))
            ));

            s.Add(P("狂奔", G("XX.XXX", "XXXXXX"),
                W(S(0.3f, Splitter, -1, 2), S(3f, Shield, 3), S(6.5f, Runner, -1, 3)),
                W(S(0.3f, Sprinter, 2), S(3f, Sprinter, 3)),
                W(S(0.3f, Sprinter, 0), S(1.6f, Sprinter, 5), S(5f, Splitter, -1, 2), S(9.5f, Chubby, 3)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Crawler, -1, 3), S(8.5f, Swarm, 2, 4)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Runner, -1, 3), S(9.5f, Belt, -1, 2)),
                W(S(0.3f, Shield, 1), S(1.2f, Shield, 4), S(5f, Sprinter, -1, 2), S(9f, Splitter, -1, 2))
            ));

            // 成团的墨粒一团一团压下来，分和炸比单发值钱。
            s.Add(P("墨团", G("XXXXXX", "X.XX.X"),
                W(S(0.3f, Swarm, 2, 4), S(2.5f, Swarm, 3, 4), S(6f, Runner, -1, 2)),
                W(S(0.3f, Splitter, -1, 2), S(3f, Swarm, 1, 4), S(5f, Swarm, 4, 4), S(9f, Shield, 3)),
                W(S(0.3f, Chubby, -1, 2), S(2.5f, Ball, -1, 4), S(6f, Swarm, 2, 5), S(9.5f, Belt, -1, 2)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Crawler, -1, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Swarm, 0, 4), S(1.5f, Swarm, 5, 4), S(4.5f, Splitter, -1, 3), S(9f, BigHead, -1, 2))
            ));

            // 一排盾墙接一排盾墙，晕和击退轮流开路。
            s.Add(P("盾墙", G("XXXXXX", "XXXXXX"),
                W(S(0.3f, Shield, 1), S(0.3f, Shield, 2), S(0.3f, Shield, 3), S(0.3f, Shield, 4)),
                W(S(0.3f, Sprinter, -1, 2), S(3f, Splitter, -1, 2), S(7f, Runner, -1, 3)),
                W(S(0.3f, Shield, 0, 2), S(1.5f, Shield, 5, 2), S(5f, Crawler, -1, 3), S(9f, Swarm, 3, 4)),
                W(S(0.3f, Strafer, -1, 3), S(4f, Belt, -1, 2), S(8.5f, Chubby, -1, 3)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Sprinter, -1, 2), S(8f, Splitter, -1, 3))
            ));

            s.Add(P("铁桶", G("XXXXXX", "XXXXXX"),
                W(S(0.3f, Shield, -1, 2), S(3f, Sprinter, -1, 2), S(7f, Crawler, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Strafer, -1, 2), S(8.5f, Swarm, 2, 5)),
                W(S(0.3f, Crawler, 0), S(1.2f, Crawler, 5), S(4.5f, Shield, 2), S(5.1f, Shield, 3), S(9f, Runner, -1, 3)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Belt, -1, 2), S(8.5f, BigHead, -1, 3)),
                W(S(0.3f, Chubby, -1, 3), S(3.5f, Splitter, -1, 3), S(8.5f, Strafer, -1, 2)),
                W(S(0.3f, Runner, -1, 4), S(3.5f, Splitter, 1), S(4.2f, Splitter, 4), S(8.5f, Ball, -1, 5)),
                Boss(BossIron, S(5f, Crawler, 0), S(7f, Crawler, 5), S(12f, Shield, -1, 2), S(17f, Sprinter, -1, 2), S(22f, Splitter, 3))
            ));
        }
    }
}

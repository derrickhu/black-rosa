using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十章 · 古寺：聚和返进池，墨僧给同伴挂盾，越磨越难打；
    // 中段重打影姬，章底是钟魔带墨王，每 8 秒全场一层盾。
    public static partial class StageCatalog
    {
        static void Chapter10(List<Plan> s)
        {
            // 墨僧第一次露面：先拆它，身边的才不会一层层挂盾。
            s.Add(P("山门", G("XXXXXX", "XXXXXX", "XX..XX"),
                W(S(0.3f, Monk, 2), S(0.9f, Runner, -1, 5), S(5f, Elite, 3), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Monk, 4), S(5f, Bulwark, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, -1, 2), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Swarm, -1, 5), S(3.5f, Monk, 3), S(4.1f, Sprinter, -1, 4), S(8.5f, Shade, -1, 4)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ).Give(Gather));

            // 晨钟：成团进场，聚 + 炸一锅端。
            s.Add(P("晨钟", G("XXXXXX", "XXXXXX", "X.XX.X"),
                W(S(0.3f, Swarm, -1, 6), S(3.5f, Ball, -1, 5), S(7.5f, Monk, 2)),
                W(S(0.3f, Runner, -1, 5), S(3f, Monk, 3), S(3.6f, Elite, 3), S(8f, Shade, -1, 5)),
                W(S(0.3f, Crawler, -1, 4), S(4f, Belt, -1, 5), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Swarm, -1, 6), S(9.5f, Splitter, -1, 3)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(4.5f, Runner, -1, 5), S(9f, Shade, -1, 5)),
                W(S(0.3f, Ball, -1, 6), S(3.5f, Warden, -1, 2), S(8f, Sprinter, -1, 4))
            ));

            // 返：飞到顶折回来再打一遍，长廊里的怪排成一串正合适。
            s.Add(P("回廊", G("XXXXXX", "XXXXXX", ".XXXX."),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Elite, -1, 2), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Shade, -1, 5), S(8f, Monk, 2)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6.5f, Monk, -1, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Warden, -1, 2), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Monk, 1), S(0.9f, Bulwark, 1), S(4.5f, Monk, 4), S(5.1f, Bulwark, 4), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Crawler, -1, 3))
            ).Give(Rebound));

            s.Add(P("禅房", G("XXXXXX", "XX..XX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Strafer, -1, 4), S(7.5f, Elite, 3)),
                W(S(0.3f, Mender, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Splitter, -1, 3), S(10f, Monk, -1, 2)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, -1, 2), S(8f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ));

            // 中段重打影姬，旁边多了墨僧给幻影挂盾。
            s.Add(P("重影", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 2), S(7f, Shade, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Shade, -1, 5), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                Boss(BossShade, S(5f, Monk, 2), S(10f, Elite, 1), S(14f, Warden, 4), S(19f, Shade, -1, 5), S(24f, Monk, 3))
            ));

            s.Add(P("经阁", G("XXXXXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, -1, 2), S(8f, Elite, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, 1), S(4.1f, Bulwark, 4), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(4f, Splitter, -1, 3), S(8.5f, Warden, -1, 2)),
                W(S(0.3f, Shade, 0, 3), S(1f, Shade, 5, 3), S(4.5f, Monk, -1, 2), S(9f, Elite, -1, 2))
            ));

            s.Add(P("塔林", G("XXXXXX", "X.XXXX", "XXXXXX"),
                W(S(0.3f, Monk, 2), S(0.9f, Swarm, -1, 5), S(5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Shade, -1, 5), S(9f, Monk, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 3), S(8f, Warden, 3)),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 0), S(4.1f, Elite, 5), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Monk, -1, 2))
            ));

            // 罗汉阵：墨僧排成一排，前面挡着的全挂着盾。
            s.Add(P("罗汉", G("XXXXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(3.5f, Elite, -1, 2), S(7.5f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, 2), S(5.6f, Monk, 3), S(9f, Sprinter, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Monk, -1, 2), S(8f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Bulwark, -1, 2))
            ));

            // 章底：钟魔每 8 秒全场挂盾，墨王压阵。拆盾的字（破三星、穿、分）是这一关的钥匙。
            s.Add(P("钟魔", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, -1, 2), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(3f, Bulwark, -1, 3), S(9f, Shade, -1, 5)),
                W(S(0.3f, Ball, -1, 6), S(3.5f, Monk, -1, 2), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(4f, Belt, -1, 5), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Monk, 1), S(7.4f, Monk, 4), S(11.5f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Shade, -1, 5), S(9.5f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Monk, 2), S(8.5f, Warden, 3), S(12.5f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Mender, -1, 2), S(12f, Runner, -1, 5)),
                Boss(BossBell, S(8f, BossKing, 1), S(14f, Monk, 4), S(20f, Elite, -1, 2), S(27f, Shade, -1, 5))
            ));
        }
    }
}

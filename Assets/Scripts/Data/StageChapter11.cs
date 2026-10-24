using System.Collections.Generic;
using static InkLine.EnemyId;

namespace InkLine
{
    // 第十一章 · 墨窟：不发新字，字池沿用古寺。开局只留四格，其余靠铲子挖。
    // 中段重打钟魔，章底钟魔再来，墨王压阵。
    public static partial class StageCatalog
    {
        static void Chapter11(List<Plan> s)
        {
            s.Add(P("洞口", G("XX..X.", ".X...."),
                W(S(0.3f, Monk, 2), S(1.2f, Runner, -1, 4), S(5f, Elite, 1), S(9f, Shade, -1, 3)),
                W(S(0.3f, Warden, 0), S(1.8f, Swarm, -1, 4), S(6f, Monk, 3), S(10f, Bulwark, -1, 2)),
                W(S(0.3f, Elite, 2), S(1.5f, Shade, -1, 4), S(6f, Sprinter, -1, 3), S(10f, Monk, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Runner, -1, 4), S(8.5f, Warden, 2)),
                W(S(0.3f, Shade, -1, 4), S(3.5f, Monk, -1, 2), S(8f, Elite, 1)),
                W(S(0.3f, Swarm, -1, 5), S(4f, Monk, 3), S(8.5f, Shade, -1, 4))
            ).DigBoard());

            s.Add(P("矿脉", G("X.XX..", "..X..."),
                W(S(0.3f, Elite, 1), S(1.2f, Monk, 2), S(5f, Belt, -1, 4), S(9f, Shade, -1, 3)),
                W(S(0.3f, Warden, 3), S(1.8f, Bulwark, -1, 2), S(6f, Runner, -1, 4), S(10f, Monk, 0)),
                W(S(0.3f, Shade, -1, 4), S(4f, Sprinter, -1, 3), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Monk, 1), S(1.2f, Monk, 4), S(5.5f, Swarm, -1, 4), S(10f, Warden, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Shade, -1, 4), S(8.5f, Bulwark, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Elite, 3))
            ).DigBoard());

            s.Add(P("岔道", G(".XX.X.", "X....."),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, 2), S(8f, Elite, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Shade, -1, 4), S(6f, Bulwark, 4), S(10f, Runner, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 3), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Sprinter, -1, 3), S(8.5f, Warden, 2)),
                W(S(0.3f, Monk, 1), S(0.9f, Bulwark, 1), S(5f, Shade, -1, 4), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 2))
            ).DigBoard());

            s.Add(P("暗河", G("X..XX.", "...X.."),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Monk, 3), S(8f, Shade, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(6f, Elite, 2), S(10f, Runner, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 4), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Elite, 1), S(4.6f, Elite, 4), S(9f, Swarm, -1, 4)),
                W(S(0.3f, Monk, 2), S(1.2f, Shade, -1, 4), S(6f, Belt, -1, 4), S(10f, Warden, 3)),
                W(S(0.3f, Shade, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Bulwark, -1, 2))
            ).DigBoard());

            // 中段重打上一章的章底。钟魔照旧敲盾，盘面得边打边挖。
            s.Add(P("钟鸣", G("XX.X..", "....X."),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 1), S(8f, Shade, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Warden, 3), S(8.5f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Monk, -1, 2), S(8.5f, Sprinter, -1, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 4), S(6f, Swarm, -1, 4), S(10f, Monk, 2)),
                Boss(BossBell, S(6f, Monk, 2), S(12f, Elite, 4), S(18f, Shade, -1, 4), S(24f, Warden, 1))
            ).DigBoard());

            s.Add(P("石室", G(".X.XX.", "X....."),
                W(S(0.3f, Elite, 2), S(1.2f, Monk, 3), S(5.5f, Shade, -1, 4), S(10f, Runner, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.8f, Bulwark, -1, 2), S(6f, Swarm, -1, 4), S(10f, Monk, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Sprinter, -1, 3), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Monk, 0), S(0.9f, Monk, 3), S(5f, Belt, -1, 4), S(9.5f, Warden, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Shade, -1, 4), S(8.5f, Bulwark, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Elite, 1))
            ).DigBoard());

            s.Add(P("深井", G("XXX...", "..X..."),
                W(S(0.3f, Monk, 1), S(1.2f, Swarm, -1, 4), S(5.5f, Elite, 2), S(10f, Shade, -1, 3)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 4), S(6f, Bulwark, -1, 2), S(10f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Monk, -1, 2), S(8.5f, Sprinter, -1, 3)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 3), S(6f, Belt, -1, 4), S(10f, Monk, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 4), S(8.5f, Swarm, -1, 4)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Warden, 2), S(8.5f, Monk, -1, 2))
            ).DigBoard());

            s.Add(P("晶洞", G("X.X.X.", ".X...."),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, 2), S(8f, Shade, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.2f, Elite, 4), S(5.5f, Bulwark, -1, 2), S(10f, Runner, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.8f, Monk, 3), S(6.5f, Swarm, -1, 4), S(11f, Shade, -1, 4)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Shade, -1, 4), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Bulwark, 2), S(0.9f, Bulwark, 4), S(5f, Elite, -1, 2), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(4f, Warden, -1, 2), S(8.5f, Shade, -1, 4))
            ).DigBoard());

            s.Add(P("地心", G("X.X..X", ".X...."),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, -1, 2), S(8f, Shade, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Warden, 1), S(4.6f, Warden, 4), S(9f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(6f, Swarm, -1, 5), S(10.5f, Monk, 2)),
                W(S(0.3f, Belt, -1, 4), S(4f, Shade, -1, 4), S(8.5f, Bulwark, -1, 2)),
                W(S(0.3f, Warden, 2), S(1.2f, Monk, 3), S(6f, Elite, -1, 2), S(10.5f, Shade, -1, 4)),
                Boss(BossBell, S(8f, BossKing, 1), S(14f, Monk, 4), S(20f, Elite, -1, 2), S(26f, Shade, -1, 4))
            ).DigBoard());
        }
    }
}

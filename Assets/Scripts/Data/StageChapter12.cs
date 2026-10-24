using System.Collections.Generic;
using static InkLine.EnemyId;

namespace InkLine
{
    // 第十二章 · 地宫：仍不发新字。开局五格，比墨窟略松，还是得挖。
    // 中段再打一次钟魔，章底换成墨王，钟魔在旁边敲盾。
    public static partial class StageCatalog
    {
        static void Chapter12(List<Plan> s)
        {
            s.Add(P("墓道", G("XXX...", ".X.X.."),
                W(S(0.3f, Monk, 1), S(1.2f, Shade, -1, 4), S(5.5f, Elite, 3), S(10f, Runner, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.8f, Bulwark, -1, 2), S(6f, Swarm, -1, 4), S(10f, Monk, 4)),
                W(S(0.3f, Elite, 2), S(1.5f, Elite, 5), S(6f, Shade, -1, 4), S(10.5f, Sprinter, -1, 3)),
                W(S(0.3f, Belt, -1, 4), S(4f, Monk, -1, 2), S(8.5f, Warden, 2)),
                W(S(0.3f, Shade, -1, 4), S(4f, Splitter, -1, 3), S(8.5f, Elite, 1)),
                W(S(0.3f, Runner, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Bulwark, -1, 2))
            ).DigBoard());

            s.Add(P("甬道", G("XX.X..", "X..X.."),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, 2), S(8f, Shade, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(6f, Elite, 0), S(10f, Runner, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 4), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Swarm, -1, 4), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Monk, 0), S(0.9f, Monk, 3), S(5f, Belt, -1, 4), S(9.5f, Shade, -1, 4)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Warden, 2), S(8.5f, Bulwark, -1, 2))
            ).DigBoard());

            s.Add(P("耳室", G(".XXX..", "X...X."),
                W(S(0.3f, Elite, 2), S(1.2f, Monk, 3), S(5.5f, Shade, -1, 4), S(10f, Swarm, -1, 4)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Bulwark, 4), S(5f, Runner, -1, 4), S(9.5f, Warden, 2)),
                W(S(0.3f, Shade, -1, 4), S(4f, Sprinter, -1, 3), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(6f, Belt, -1, 4), S(10.5f, Shade, -1, 4)),
                W(S(0.3f, Monk, 1), S(1.2f, Warden, 4), S(6f, Swarm, -1, 4), S(10f, Elite, 2)),
                W(S(0.3f, Runner, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Bulwark, -1, 2))
            ).DigBoard());

            s.Add(P("祭台", G("X.XX..", "..X.X."),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Shade, -1, 4), S(8f, Elite, 1)),
                W(S(0.3f, Warden, 0), S(1.8f, Bulwark, -1, 2), S(6f, Runner, -1, 4), S(10f, Monk, 3)),
                W(S(0.3f, Shade, -1, 5), S(4f, Sprinter, -1, 4), S(8.5f, Warden, 2)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(6f, Swarm, -1, 5), S(10.5f, Monk, -1, 2)),
                W(S(0.3f, Belt, -1, 4), S(4f, Shade, -1, 4), S(8.5f, Bulwark, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 2))
            ).DigBoard());

            s.Add(P("再钟", G("XX..X.", "X.X..."),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 2), S(8f, Shade, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Warden, 1), S(8.5f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Monk, -1, 2), S(8.5f, Sprinter, -1, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(6f, Swarm, -1, 4), S(10.5f, Monk, 3)),
                Boss(BossBell, S(6f, Shade, -1, 4), S(12f, Monk, 2), S(18f, Elite, 4), S(24f, Warden, 1))
            ).DigBoard());

            s.Add(P("藏书", G(".XX.X.", ".X..X."),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, 1), S(8f, Shade, -1, 4)),
                W(S(0.3f, Warden, 2), S(1.5f, Bulwark, -1, 2), S(6f, Elite, 4), S(10f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Sprinter, -1, 3), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.2f, Elite, 3), S(5.5f, Belt, -1, 4), S(10f, Swarm, -1, 4)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(5f, Shade, -1, 4), S(9.5f, Warden, 2)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Splitter, -1, 3), S(8.5f, Elite, -1, 2))
            ).DigBoard());

            s.Add(P("金井", G("X..XXX", ".X...."),
                W(S(0.3f, Monk, 2), S(1.2f, Swarm, -1, 4), S(5.5f, Shade, -1, 4), S(10f, Elite, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(6f, Bulwark, -1, 2), S(10f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 4), S(4f, Monk, -1, 2), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 3), S(6f, Belt, -1, 4), S(10.5f, Monk, 0)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 4), S(8.5f, Swarm, -1, 4)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Warden, 2), S(8.5f, Monk, -1, 2))
            ).DigBoard());

            s.Add(P("偏殿", G("XXX.X.", "....X."),
                W(S(0.3f, Elite, 1), S(1.2f, Monk, 2), S(5.5f, Shade, -1, 4), S(10f, Runner, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Warden, 0), S(4.6f, Warden, 4), S(9f, Swarm, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(4f, Sprinter, -1, 4), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Belt, -1, 4), S(4f, Elite, -1, 2), S(8.5f, Shade, -1, 4)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 3), S(5f, Bulwark, -1, 2), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Warden, 2), S(1.5f, Splitter, -1, 3), S(6f, Elite, 5), S(10.5f, Shade, -1, 4))
            ).DigBoard());

            s.Add(P("墨王", G("XX.XX.", "..X..."),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, -1, 2), S(8f, Shade, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Warden, 1), S(4.6f, Warden, 4), S(9f, Runner, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(4f, Monk, -1, 2), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(6f, Swarm, -1, 5), S(10.5f, Belt, -1, 4)),
                W(S(0.3f, Monk, 2), S(1.2f, Monk, 3), S(6f, Shade, -1, 4), S(10.5f, Bulwark, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Warden, -1, 2), S(8.5f, Elite, -1, 2)),
                Boss(BossKing, S(8f, BossBell, 4), S(14f, Monk, 1), S(20f, Shade, -1, 4), S(26f, Elite, -1, 2))
            ).DigBoard());
        }
    }
}

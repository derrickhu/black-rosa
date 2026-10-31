using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十三章 · 云海：新字「影」。横掠和爬子还在场上，出怪血比地宫厚一截。
    // 中段打影姬。章底钟魔带墨王。盘面留着整列，风火轮和穿云箭摆得下。
    public static partial class StageCatalog
    {
        static void Chapter13(List<Plan> s)
        {
            // 影：打出去之后再补一发淡的。
            s.Add(P("云门", G("XXXXXX", "XXXXXX", "XX..XX"),
                W(S(0.3f, Monk, 2), S(0.9f, Runner, -1, 5), S(5f, Elite, 3), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Monk, 4), S(5f, Bulwark, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, -1, 2), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Swarm, -1, 5), S(3.5f, Monk, 3), S(4.1f, Sprinter, -1, 4), S(8.5f, Shade, -1, 4)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ).Give(Shadow));

            // 横掠换列，穿云箭和风才打得着。
            s.Add(P("风口", G("XXXXXX", "XXXXXX", "X.XX.X"),
                W(S(0.3f, Swarm, -1, 6), S(3.5f, Ball, -1, 5), S(7.5f, Monk, 2)),
                W(S(0.3f, Runner, -1, 5), S(3f, Monk, 3), S(3.6f, Elite, 3), S(8f, Shade, -1, 5)),
                W(S(0.3f, Crawler, -1, 4), S(4f, Belt, -1, 5), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Swarm, -1, 6), S(9.5f, Splitter, -1, 3)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(4.5f, Runner, -1, 5), S(9f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(1.0f, Warden, -1, 2), S(4.5f, Sprinter, -1, 2), S(7.5f, Ball, -1, 4))
            ));

            // 怪排成一串。万箭打中就散，凑上穿才一路射穿。
            s.Add(P("穿云", G("XXXXXX", "XXXXXX", ".XXXX."),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Elite, -1, 2), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Shade, -1, 5), S(8f, Monk, 2)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6.5f, Monk, -1, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Warden, -1, 2), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Monk, 1), S(0.9f, Bulwark, 1), S(4.5f, Monk, 4), S(5.1f, Bulwark, 4), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Crawler, -1, 3))
            ));

            s.Add(P("叠浪", G("XXXXXX", "XX..XX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Strafer, -1, 4), S(7.5f, Elite, 3)),
                W(S(0.3f, Mender, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Splitter, -1, 3), S(10f, Monk, -1, 2)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, -1, 2), S(8f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ));

            // 中段打影姬。墨王基础血追不上钟魔，所以这一关用影姬。
            s.Add(P("影姬", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 2), S(7f, Shade, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Shade, -1, 5), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                Boss(BossShade, S(5f, Monk, 2), S(10f, Elite, 1), S(14f, Warden, 4), S(19f, Shade, -1, 5), S(24f, Monk, 3))
            ));

            s.Add(P("回风", G("XXXXXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, -1, 2), S(8f, Elite, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, 1), S(4.1f, Bulwark, 4), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(4f, Splitter, -1, 3), S(8.5f, Warden, -1, 2)),
                W(S(0.3f, Shade, 0, 3), S(1f, Shade, 5, 3), S(4.5f, Monk, -1, 2), S(9f, Elite, -1, 2))
            ));

            s.Add(P("积云", G("XXXXXX", "X.XXXX", "XXXXXX"),
                W(S(0.3f, Monk, 2), S(0.9f, Swarm, -1, 5), S(5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Shade, -1, 5), S(9f, Monk, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 3), S(8f, Warden, 3)),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 0), S(4.1f, Elite, 5), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Monk, -1, 2))
            ));

            // 墨僧还在给同伴挂盾。破和影比纯推开更值。
            s.Add(P("断霞", G("XXXXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(3.5f, Elite, -1, 2), S(7.5f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, 2), S(5.6f, Monk, 3), S(9f, Sprinter, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Monk, -1, 2), S(8f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Bulwark, -1, 2))
            ));

            // 章底：钟魔全场挂盾，墨王压阵。
            s.Add(P("云巅", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, -1, 2), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(3f, Bulwark, -1, 3), S(9f, Shade, -1, 5)),
                W(S(0.3f, Ball, -1, 6), S(3.5f, Monk, -1, 2), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(4f, Belt, -1, 5), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Monk, 1), S(7.4f, Monk, 4), S(11.5f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Shade, -1, 5), S(9.5f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Monk, 2), S(8.5f, Warden, 3), S(12.5f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Mender, -1, 2), S(12f, Runner, -1, 5)),
                Boss(BossBell, S(1.2f, Elite, -1, 4), S(8f, BossKing, 1), S(14f, Monk, 4), S(26f, Shade, -1, 2))
            ));
        }
    }
}

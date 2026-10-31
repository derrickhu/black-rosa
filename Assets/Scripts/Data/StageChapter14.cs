using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十四章 · 冥河：新字「封」。墨僧、墨影、补墨多，挂盾和加速的怪让封有用。
    // 中段重打钟魔。章底影姬带钟魔。冰、土、聚能在这一章凑成定海针。
    public static partial class StageCatalog
    {
        static void Chapter14(List<Plan> s)
        {
            // 封：打中之后，挂盾、分裂、隐身、狂化都停一会儿。
            s.Add(P("冥口", G("XXXXXX", "XXXXXX", "XX..XX"),
                W(S(0.3f, Monk, 2), S(0.9f, Runner, -1, 5), S(5f, Elite, 3), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Monk, 4), S(5f, Bulwark, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, -1, 2), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Swarm, -1, 5), S(3.5f, Monk, 3), S(4.1f, Sprinter, -1, 4), S(8.5f, Shade, -1, 4)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ).Give(Seal));

            // 成团进场。冰、土、聚钉在一起就是定海针。
            s.Add(P("急流", G("XXXXXX", "XXXXXX", "X.XX.X"),
                W(S(0.3f, Swarm, -1, 6), S(3.5f, Ball, -1, 5), S(7.5f, Monk, 2)),
                W(S(0.3f, Runner, -1, 5), S(3f, Monk, 3), S(3.6f, Elite, 3), S(8f, Shade, -1, 5)),
                W(S(0.3f, Crawler, -1, 4), S(4f, Belt, -1, 5), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Swarm, -1, 6), S(9.5f, Splitter, -1, 3)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(4.5f, Runner, -1, 5), S(9f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(1.0f, Warden, -1, 2), S(4.5f, Sprinter, -1, 2), S(7.5f, Ball, -1, 4))
            ));

            // 补墨在给周围回血。先封住它，再慢慢磨。
            s.Add(P("沉锚", G("XXXXXX", "XXXXXX", ".XXXX."),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Elite, -1, 2), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Shade, -1, 5), S(8f, Monk, 2)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6.5f, Monk, -1, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Warden, -1, 2), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Monk, 1), S(0.9f, Bulwark, 1), S(4.5f, Monk, 4), S(5.1f, Bulwark, 4), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Crawler, -1, 3))
            ));

            s.Add(P("鬼灯", G("XXXXXX", "XX..XX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Strafer, -1, 4), S(7.5f, Elite, 3)),
                W(S(0.3f, Mender, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Splitter, -1, 3), S(10f, Monk, -1, 2)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, -1, 2), S(8f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ));

            // 中段重打上一章的钟魔。
            s.Add(P("再钟", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 2), S(7f, Shade, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Shade, -1, 5), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                Boss(BossBell, S(5f, Monk, 2), S(10f, Elite, 1), S(14f, Warden, 4), S(19f, Shade, -1, 5), S(24f, Monk, 3))
            ));

            s.Add(P("失路", G("XXXXXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Monk, -1, 2), S(8f, Elite, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, 1), S(4.1f, Bulwark, 4), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(4f, Splitter, -1, 3), S(8.5f, Warden, -1, 2)),
                W(S(0.3f, Shade, 0, 3), S(1f, Shade, 5, 3), S(4.5f, Monk, -1, 2), S(9f, Elite, -1, 2))
            ));

            s.Add(P("渡口", G("XXXXXX", "X.XXXX", "XXXXXX"),
                W(S(0.3f, Monk, 2), S(0.9f, Swarm, -1, 5), S(5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Shade, -1, 5), S(9f, Monk, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 3), S(8f, Warden, 3)),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 0), S(4.1f, Elite, 5), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Monk, -1, 2))
            ));

            // 墨僧排开挂盾。封住施法者，盾就补不上。
            s.Add(P("夜航", G("XXXXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(3.5f, Elite, -1, 2), S(7.5f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Monk, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, 2), S(5.6f, Monk, 3), S(9f, Sprinter, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Monk, -1, 2), S(8f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Bulwark, -1, 2))
            ));

            // 章底：影姬分幻影，钟魔在旁边敲盾。
            s.Add(P("冥河", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, -1, 2), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(3f, Bulwark, -1, 3), S(9f, Shade, -1, 5)),
                W(S(0.3f, Ball, -1, 6), S(3.5f, Monk, -1, 2), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(4f, Belt, -1, 5), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Monk, 1), S(7.4f, Monk, 4), S(11.5f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Shade, -1, 5), S(9.5f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Monk, 2), S(8.5f, Warden, 3), S(12.5f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Mender, -1, 2), S(12f, Runner, -1, 5)),
                Boss(BossShade, S(1.2f, Elite, -1, 4), S(8f, BossBell, 1), S(14f, Monk, 4), S(26f, Shade, -1, 2))
            ));
        }
    }
}

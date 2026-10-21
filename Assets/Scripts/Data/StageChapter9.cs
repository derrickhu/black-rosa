using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第九章 · 竹林：第二幕开场。第三排第一次能整排开满，破和千钧进池，
    // 墨影隐身专治纯范围流；中段重打墨王，章底是影姬带雷公。
    public static partial class StageCatalog
    {
        static void Chapter9(List<Plan> s)
        {
            // 墨影第一次露面：范围打不着，得用炮弹本体先打显形。
            s.Add(P("竹影", G("XXXXXX", "XXXXXX", "XX..XX"),
                W(S(0.3f, Shade, -1, 4), S(3.5f, Elite, 2), S(7f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Shade, -1, 4), S(6f, Bulwark, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Shade, -1, 5), S(9.5f, Swarm, -1, 5)),
                W(S(0.3f, Mender, -1, 2), S(3.5f, Sprinter, -1, 4), S(8f, Shade, -1, 4)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Shade, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Splitter, -1, 3), S(8.5f, Shade, -1, 5))
            ).Give(Break));

            // 一路带甲的：破防之后甲照样削，带上重才见效。
            s.Add(P("破竹", G("XXXXXX", "XXXXXX", "X.XX.X"),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Shield, -1, 3), S(7.5f, Shade, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Bulwark, 2), S(5.6f, Bulwark, 3), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Shade, -1, 5), S(10f, Belt, -1, 4)),
                W(S(0.3f, Shield, -1, 4), S(3.5f, Mender, 2), S(4.1f, Bulwark, 3), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Elite, -1, 2), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Bulwark, -1, 2))
            ));

            // 千钧：按当前血量砸，专拆整波整波的大块头。
            s.Add(P("千钧", G("XXXXXX", "XXXXXX", ".XXXX."),
                W(S(0.3f, Warden, 1), S(0.9f, Warden, 4), S(4.5f, Elite, -1, 2), S(8.5f, Shade, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Chubby, -1, 4), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6.5f, Mender, -1, 2)),
                W(S(0.3f, Warden, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Bulwark, 4), S(4.5f, Sprinter, -1, 4), S(9f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Shade, -1, 5), S(9f, Crawler, -1, 3))
            ).Give(Qian, Jun));

            // 第二排中间塌了两格，第三排却是满的：字得往上摆。
            s.Add(P("林深", G("XXXXXX", "XX..XX", "XXXXXX"),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Strafer, -1, 4), S(7.5f, Elite, 3)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Shade, -1, 4), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Sprinter, -1, 4), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Splitter, -1, 3), S(10f, Runner, -1, 5)),
                W(S(0.3f, Shade, -1, 4), S(3.5f, Bulwark, -1, 2), S(8f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Mender, -1, 2))
            ));

            // 中段重打墨王，这回盘面整个是开的。
            s.Add(P("重逢", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Elite, 2), S(7f, Warden, 3)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Mender, -1, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Shade, -1, 5), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Splitter, -1, 3), S(10f, Swarm, -1, 5)),
                Boss(BossKing, S(5f, Shade, -1, 4), S(10f, Elite, 1), S(14f, Warden, 4), S(19f, Bulwark, -1, 2), S(24f, Shade, -1, 5))
            ));

            s.Add(P("风竹", G("XXXXXX", "X.XX.X", "XXXXXX"),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Shade, -1, 5), S(8f, Elite, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, 1), S(4.1f, Bulwark, 4), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Mender, -1, 2), S(9f, Shade, -1, 4)),
                W(S(0.3f, Strafer, -1, 5), S(4f, Splitter, -1, 3), S(8.5f, Warden, -1, 2)),
                W(S(0.3f, Shade, 0, 3), S(1f, Shade, 5, 3), S(4.5f, Sprinter, -1, 4), S(9f, Elite, -1, 2))
            ));

            s.Add(P("竹海", G("XXXXXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Shade, -1, 5), S(3f, Swarm, -1, 5), S(6.5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Bulwark, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Shade, -1, 5), S(9f, Mender, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 3), S(8f, Warden, 3)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Elite, 0), S(4.1f, Elite, 5), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Mender, -1, 2), S(8.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Shade, -1, 5))
            ));

            // 夜里的竹林：一半的兵都是影子。
            s.Add(P("夜行", G("XXXXXX", "XX.XXX", "XXXXXX"),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Shade, -1, 5), S(7.5f, Elite, 2)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Shade, -1, 5), S(10f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Mender, -1, 2), S(9f, Shade, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Strafer, -1, 4), S(8f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Shade, -1, 5), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Shade, -1, 5), S(9f, Bulwark, -1, 2))
            ));

            // 章底：三排全开。影姬半血分出两只幻影，雷公在旁边护着。
            s.Add(P("影姬", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Shade, -1, 5), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(3f, Mender, -1, 2), S(9f, Shade, -1, 5)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Runner, -1, 5), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(4f, Belt, -1, 5), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Mender, 1), S(7.4f, Mender, 4), S(11.5f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Shade, -1, 5), S(9.5f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Warden, 2), S(8.5f, Warden, 3), S(12.5f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(4f, Shade, -1, 5), S(8.5f, Mender, -1, 2), S(12f, Runner, -1, 5)),
                Boss(BossShade, S(8f, BossThunder, 1), S(14f, Shade, -1, 4), S(20f, Elite, -1, 2), S(27f, Runner, -1, 4))
            ));
        }
    }
}

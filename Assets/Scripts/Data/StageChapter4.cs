using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第四章 · 沙漠：第二排逐关补齐，章底第一次两排全开，第三排还不开。雷和万箭进池，
    // 雷跟火、冰叠在同一列分别是焦雷、霰雷。医墨和壁垒登场。
    public static partial class StageCatalog
    {
        static void Chapter4(List<Plan> s)
        {
            // 医墨只奶别人。第一次登场配两只肉，否则看不出它在做什么；雷正好连着劈。
            s.Add(P("惊雷", G("XXXXXX", "XXXX.."),
                W(S(0.3f, Sprinter, -1, 2), S(3f, Shield, 3), S(6.5f, Splitter, -1, 2)),
                W(S(0.3f, Mender, 3), S(0.8f, Chubby, 2), S(1.3f, Chubby, 4)),
                W(S(0.3f, Mender, 0), S(0.9f, Shield, 0), S(4.5f, Mender, 5), S(5.1f, Shield, 5)),
                W(S(0.3f, Strafer, -1, 3), S(4f, Splitter, -1, 2), S(8.5f, Swarm, 3, 4)),
                W(S(0.3f, Mender, 2), S(2f, Crawler, -1, 2), S(6.5f, Runner, -1, 3)),
                W(S(0.3f, Shield, 1), S(1.5f, Shield, 4), S(5f, Mender, 3), S(9f, Sprinter, -1, 2))
            ).Give(Thunder));

            // 横游一字排开，雷一劈就是一串。
            s.Add(P("横雷", G("XXXXXX", ".XXXX."),
                W(S(0.3f, Mender, 3), S(0.9f, Chubby, 3), S(4f, Runner, -1, 3)),
                W(S(0.3f, Strafer, 0), S(0.3f, Strafer, 2), S(0.3f, Strafer, 4), S(3f, Strafer, 1), S(3f, Strafer, 5)),
                W(S(0.3f, Splitter, -1, 2), S(3f, Swarm, -1, 5), S(7f, Shield, -1, 2)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Sprinter, -1, 2), S(8f, Mender, 2), S(8.6f, BigHead, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Belt, -1, 3), S(8.5f, Strafer, -1, 3))
            ));

            // 万箭：墨群铺满六列，单列打法接不住。
            s.Add(P("万箭", G("XXXXXX", "XXXXX."),
                W(S(0.3f, Swarm, 0, 4), S(0.3f, Swarm, 5, 4), S(3f, Swarm, 2, 4), S(6f, Runner, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(1f, Shield, -1, 2), S(5f, Sprinter, -1, 2)),
                W(S(0.3f, Ball, -1, 5), S(2.5f, Swarm, 1, 4), S(4.5f, Swarm, 4, 4), S(8f, Splitter, -1, 2)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Crawler, -1, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Splitter, -1, 3), S(3.5f, Mender, 3), S(4f, Chubby, 3), S(8f, Ball, -1, 5))
            ).Give(Myriad, Arrow));

            // 壁垒每发减 1，多段小伤害打上去像挠痒，得把一列的字叠厚。
            s.Add(P("壁垒", G("XXXXXX", "XX..XX"),
                W(S(0.3f, Sprinter, -1, 2), S(3f, Swarm, -1, 4), S(7f, Mender, 3)),
                W(S(0.3f, Bulwark, 3)),
                W(S(0.3f, Bulwark, 1), S(2.5f, Bulwark, 4), S(6.5f, Belt, -1, 2)),
                W(S(0.3f, Crawler, -1, 3), S(4f, Splitter, -1, 2), S(9f, Swarm, 3, 4)),
                W(S(0.3f, Bulwark, 2), S(2.5f, Strafer, -1, 2), S(7.5f, Runner, -1, 3)),
                W(S(0.3f, Bulwark, 0), S(2f, Bulwark, 5), S(6.5f, Mender, 3), S(10.5f, Shield, -1, 2))
            ));

            s.Add(P("铁壁", G("XXXXXX", "XXX.XX"),
                W(S(0.3f, Bulwark, 3), S(3f, Sprinter, -1, 2), S(7f, Mender, 2)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Splitter, -1, 2), S(8f, Runner, -1, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Bulwark, 1), S(4.5f, Mender, 4), S(5.1f, Bulwark, 4)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Swarm, -1, 5), S(8f, Crawler, -1, 3)),
                Boss(BossIron, S(4f, Bulwark, 0), S(6f, Bulwark, 5), S(11f, Mender, 3), S(15f, Shield, -1, 2), S(20f, Sprinter, -1, 2))
            ));

            // 两边各缺一条，医墨躲在壁垒后面，火力得往中间收。
            s.Add(P("断路", G(".XXXX.", "XXXXXX"),
                W(S(0.3f, Mender, 3), S(0.9f, Bulwark, 3), S(5f, Runner, -1, 3)),
                W(S(0.3f, Shield, -1, 2), S(3f, Sprinter, -1, 2), S(7f, Swarm, 2, 5)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4f, Mender, 4), S(4.6f, Bulwark, 4)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Strafer, -1, 3), S(8.5f, Crawler, -1, 2)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Shield, -1, 3), S(8f, Ball, -1, 5))
            ));

            s.Add(P("沙暴", G("XXXXX.", "XXXXXX"),
                W(S(0.3f, Strafer, -1, 3), S(3f, Swarm, -1, 5), S(7f, Runner, -1, 3)),
                W(S(0.3f, Bulwark, 2), S(2f, Mender, 3), S(5.5f, Shield, -1, 2), S(9f, Splitter, -1, 2)),
                W(S(0.3f, Swarm, 0, 4), S(1.5f, Swarm, 5, 4), S(4.5f, Sprinter, -1, 3), S(8.5f, Crawler, -1, 2)),
                W(S(0.3f, Belt, -1, 3), S(3.5f, Strafer, -1, 3), S(8.5f, Bulwark, 3)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Ball, -1, 5), S(8f, Bulwark, -1, 2))
            ));

            s.Add(P("流沙", G("XXXXXX", "X.XXXX"),
                W(S(0.3f, Runner, -1, 4), S(3f, Sprinter, -1, 2), S(6.5f, Strafer, -1, 2)),
                W(S(0.3f, Belt, -1, 3), S(3f, Swarm, -1, 5), S(7f, Mender, 3)),
                W(S(0.3f, Sprinter, 0), S(1f, Sprinter, 5), S(4f, Splitter, -1, 3), S(8f, Runner, -1, 3)),
                W(S(0.3f, Bulwark, 2), S(1.5f, Bulwark, 3), S(5f, Strafer, -1, 3), S(9f, Ball, -1, 5)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Shield, -1, 3), S(8f, Sprinter, -1, 3))
            ));

            s.Add(P("双首", G("XXXXXX", "XXXXXX"),
                W(S(0.3f, Bulwark, 3), S(3f, Sprinter, -1, 2), S(7f, Strafer, -1, 2)),
                W(S(0.3f, Mender, 2), S(0.9f, Shield, 2), S(4.5f, Splitter, -1, 3), S(9f, Swarm, 3, 5)),
                W(S(0.3f, Runner, -1, 4), S(3.5f, Belt, -1, 3), S(8f, Bulwark, 1), S(8.6f, Bulwark, 4)),
                W(S(0.3f, Strafer, -1, 3), S(4f, Crawler, -1, 3), S(8.5f, Mender, 3), S(9.1f, Chubby, 3)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Splitter, -1, 3), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Bulwark, 0), S(1.5f, Bulwark, 5), S(5f, Mender, -1, 2), S(9.5f, Runner, -1, 4)),
                Boss(BossTwin, S(5f, Sprinter, 0), S(7f, Sprinter, 5), S(12f, Bulwark, 3), S(16f, Mender, 2), S(21f, Runner, -1, 3))
            ));
        }
    }
}

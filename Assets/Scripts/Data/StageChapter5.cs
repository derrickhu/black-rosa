using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第五章 · 森林：第三排第一次露头，只开一两格。金、木、连斩进池 —— 熔金、镇金、雷决都在这一章亮。
    // 墨尊登场，一只顶一队，单靠铺字已经吃力，该回去升级词条和道具了。
    public static partial class StageCatalog
    {
        static void Chapter5(List<Plan> s)
        {
            // 墨尊血厚，打死掉得也多，金字在它身上最值。
            s.Add(P("点金", G("XXXXXX", "XXXX.X"),
                W(S(0.3f, Bulwark, 3), S(3f, Sprinter, -1, 2), S(7f, Mender, 2)),
                W(S(0.3f, Elite, 3)),
                W(S(0.3f, Elite, 1), S(2.5f, Elite, 4), S(7.5f, Shield, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Crawler, -1, 3), S(9.5f, Swarm, 3, 5)),
                W(S(0.3f, Elite, 2), S(2.5f, Mender, 3), S(6.5f, Bulwark, -1, 2)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Runner, -1, 3), S(9.5f, Strafer, -1, 2))
            ).Give(Gold));

            s.Add(P("铁林", G("XXXXXX", ".XXXX.", ".X..X."),
                W(S(0.3f, Bulwark, 2), S(0.3f, Bulwark, 3), S(4f, Runner, -1, 3)),
                W(S(0.3f, Bulwark, 0), S(1f, Bulwark, 5), S(4f, Elite, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Mender, 1), S(0.9f, Bulwark, 1), S(4.5f, Mender, 4), S(5.1f, Bulwark, 4)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Splitter, -1, 3), S(8.5f, Shield, -1, 2)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Strafer, -1, 3), S(8.5f, Elite, 2))
            ));

            // 木字吸血回城，打得越久越赚。
            s.Add(P("生木", G("XXXXXX", ".XXXXX"),
                W(S(0.3f, Runner, -1, 3), S(3f, Shield, -1, 2), S(7f, Sprinter, -1, 2)),
                W(S(0.3f, Elite, 3), S(3f, Splitter, -1, 3), S(7.5f, Strafer, -1, 2)),
                W(S(0.3f, Bulwark, 1), S(1.5f, Bulwark, 4), S(5f, Mender, 3), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Belt, -1, 3), S(8f, Crawler, -1, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Runner, -1, 4))
            ).Give(Wood));

            s.Add(P("林间", G("XX.XXX", "XXXXXX"),
                W(S(0.3f, Bulwark, 2), S(1f, Bulwark, 3), S(4.5f, Swarm, -1, 5)),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Mender, 3), S(7f, Splitter, -1, 3)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Sprinter, -1, 3), S(8f, Bulwark, -1, 2)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Crawler, -1, 3), S(8.5f, Elite, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Bulwark, 1), S(4.5f, Mender, 4), S(5.1f, Bulwark, 4), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Elite, 2), S(1.5f, Elite, 3), S(5.5f, Splitter, -1, 3), S(10f, Ball, -1, 5))
            ));

            s.Add(P("双影", G("XXXXXX", "XXXXXX"),
                W(S(0.3f, Elite, 3), S(3f, Sprinter, -1, 3), S(7f, Mender, 2)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Splitter, -1, 3), S(8f, Runner, -1, 4)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Shield, -1, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Crawler, -1, 3), S(9.5f, Belt, -1, 3)),
                Boss(BossTwin, S(4f, Elite, 3), S(9f, Sprinter, 0), S(9f, Sprinter, 5), S(14f, Bulwark, -1, 2), S(19f, Runner, -1, 4))
            ));

            // 连斩：墨群一团一团压下来，斩一刀带一片。
            s.Add(P("连斩", G("XXXXXX", "XXXX..", "....XX"),
                W(S(0.3f, Swarm, 2, 5), S(2.5f, Swarm, 3, 5), S(6f, Elite, 3)),
                W(S(0.3f, Splitter, -1, 3), S(3f, Swarm, 1, 5), S(5f, Swarm, 4, 5), S(9f, Bulwark, 3)),
                W(S(0.3f, Mender, 2), S(1f, Shield, 2), S(4f, Swarm, -1, 6), S(8.5f, Sprinter, -1, 3)),
                W(S(0.3f, Swarm, 0, 5), S(1.5f, Swarm, 5, 5), S(4.5f, Splitter, -1, 3), S(9f, Elite, -1, 2)),
                W(S(0.3f, Strafer, -1, 3), S(3f, Swarm, 3, 6), S(6.5f, Runner, -1, 4), S(10f, Bulwark, -1, 2))
            ).Give(Link, Slash));

            // 墨尊身后跟着医墨，先杀谁是这一关唯一的问题。
            s.Add(P("护送", G("XXXXXX", "XX..XX", "X....X"),
                W(S(0.3f, Elite, 2), S(0.9f, Mender, 2), S(4.5f, Elite, 4), S(5.1f, Mender, 4)),
                W(S(0.3f, Runner, -1, 4), S(3f, Splitter, -1, 3), S(7.5f, Shield, -1, 3)),
                W(S(0.3f, Elite, 0), S(0.9f, Mender, 0), S(3.5f, Elite, 5), S(4.1f, Mender, 5), S(8f, Bulwark, 3)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Strafer, -1, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 1), S(4f, Bulwark, 4), S(4.6f, Mender, 4), S(8.5f, Crawler, -1, 3))
            ));

            // 两侧快脚夹击，左下角厚、右上角空。
            s.Add(P("窄巷", G(".XXXXX", "XXXXXX"),
                W(S(0.3f, Runner, 0, 3), S(1.5f, Runner, 5, 3), S(5f, Elite, 2)),
                W(S(0.3f, Bulwark, 2), S(1f, Bulwark, 3), S(4.5f, Strafer, -1, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Mender, 1), S(0.9f, Shield, 1), S(4.5f, Mender, 4), S(5.1f, Shield, 4), S(9f, Sprinter, -1, 3)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Crawler, -1, 3), S(8.5f, Elite, 3)),
                W(S(0.3f, Belt, -1, 4), S(3.5f, Runner, -1, 4), S(7.5f, Bulwark, -1, 2))
            ));

            s.Add(P("牢头", G("XXXXXX", "XXXXX.", "..X..."),
                W(S(0.3f, Elite, 3), S(3f, Bulwark, -1, 2), S(7f, Sprinter, -1, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Shield, 1), S(4.5f, Mender, 4), S(5.1f, Shield, 4), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Strafer, -1, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Bulwark, 2), S(6.1f, Bulwark, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Crawler, -1, 3), S(8.5f, Mender, -1, 2)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Elite, -1, 2), S(8f, Belt, -1, 3)),
                Boss(BossWarden, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Bulwark, -1, 2), S(17f, Sprinter, -1, 3), S(22f, Mender, 3))
            ));
        }
    }
}

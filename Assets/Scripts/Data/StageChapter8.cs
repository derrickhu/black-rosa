using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第八章 · 火山：第三排开到两到四格，始终留着缺口，三排全开留给以后的章节。惑和秒杀进池，
    // 关底成对、连战，最后是墨王带墨医。
    public static partial class StageCatalog
    {
        static void Chapter8(List<Plan> s)
        {
            // 惑让敌人互打，敌人越挤越值。
            s.Add(P("惑心", G("XXXXXX", "XXXXXX", "X....."),
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(4.5f, Runner, -1, 5), S(8.5f, Warden, 1)),
                W(S(0.3f, Warden, 4), S(1.5f, Mender, 3), S(2.1f, Bulwark, 3), S(6f, Swarm, -1, 5)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Splitter, -1, 3), S(8f, Elite, -1, 2)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Shield, -1, 3), S(8f, Warden, 2), S(8.6f, Warden, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Mender, -1, 2), S(9f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Crawler, -1, 3), S(8.5f, Runner, -1, 5))
            ).Give(Confuse));

            // 两只关底同一波上：鼓面挡一发，墨囊死了还要裂。
            s.Add(P("双鼓", G("XXXXXX", "XXXXXX", "....XX"),
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 5), S(7f, Warden, -1, 2)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Mender, 2), S(4.1f, Bulwark, 2), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Splitter, -1, 3), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Belt, -1, 4), S(9f, Crawler, -1, 3)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Runner, -1, 5), S(8.5f, Elite, -1, 2)),
                Boss(BossDrum, S(0.3f, BossInkbag, 1), S(6f, Elite, 4), S(11f, Sprinter, -1, 4), S(16f, Warden, 2), S(21f, Swarm, -1, 5))
            ));

            // 秒杀：整波整波的墨尊，血线压到一截就直接抹掉。
            s.Add(P("秒杀", G("XXXXXX", "XXXXX.", ".XX..."),
                W(S(0.3f, Elite, 1), S(0.3f, Elite, 4), S(3f, Elite, 2), S(3f, Elite, 3)),
                W(S(0.3f, Runner, -1, 5), S(3f, Swarm, -1, 5), S(6.5f, Ball, -1, 5)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6f, Mender, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Sprinter, -1, 4), S(9f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 3), S(2.5f, Elite, -1, 3), S(6.5f, Mender, 2), S(7.1f, Mender, 3)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Splitter, -1, 3), S(8f, Crawler, -1, 3))
            ).Give(Sec, Kill));

            // 第二、三排中间都塌了两格，中路只能靠第一排撑。
            s.Add(P("裂谷", G("XXXXXX", "XX..XX", "XX..XX"),
                W(S(0.3f, Runner, -1, 5), S(3f, Elite, 3), S(7f, Strafer, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Splitter, -1, 3), S(9.5f, Swarm, -1, 5)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Mender, 3), S(5.1f, Bulwark, 3), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Belt, -1, 4), S(10f, Crawler, -1, 3)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Elite, -1, 2))
            ));

            s.Add(P("医馆", G("XXXXXX", "XXXXXX", ".X..X."),
                W(S(0.3f, Elite, 2), S(0.9f, Mender, 2), S(4f, Elite, 4), S(4.6f, Mender, 4)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Runner, -1, 5), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 1), S(4f, Bulwark, 4), S(4.6f, Mender, 4), S(9f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Strafer, -1, 4), S(10f, Splitter, -1, 3)),
                Boss(BossMedic, S(4f, Elite, 1), S(6f, Elite, 4), S(11f, Warden, 3), S(16f, Bulwark, -1, 2), S(21f, Runner, -1, 5))
            ));

            s.Add(P("熔流", G("XXXXXX", "XXXXXX", "X.X..."),
                W(S(0.3f, Runner, -1, 5), S(3f, Sprinter, -1, 4), S(6.5f, Elite, 3)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Belt, -1, 4), S(9.5f, Splitter, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Mender, 2), S(4.1f, Bulwark, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Elite, -1, 2), S(10f, Crawler, -1, 3)),
                W(S(0.3f, Runner, 0, 3), S(1f, Runner, 5, 3), S(4f, Sprinter, -1, 4), S(8.5f, Shield, -1, 3))
            ));

            s.Add(P("火海", G("XXXXXX", "XXXXXX", ".XXX.."),
                W(S(0.3f, Strafer, -1, 4), S(3f, Swarm, -1, 5), S(6.5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Bulwark, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Belt, -1, 4), S(9f, Mender, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 3), S(8f, Warden, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Strafer, -1, 4), S(9.5f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Mender, -1, 2), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Runner, -1, 5))
            ));

            // 连战：铁桶、双首、牢头各占一波，一只接一只。
            s.Add(P("连战", G("XXXXXX", "XXXXX.", ".XXXX."),
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 5), S(7f, Warden, -1, 2)),
                W(S(0.3f, BossIron, 3), S(5f, Bulwark, 1), S(7f, Bulwark, 4), S(12f, Crawler, -1, 3), S(16f, Mender, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Strafer, -1, 4), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, BossTwin, 1), S(0.3f, BossTwin, 4), S(5f, Sprinter, 0), S(7f, Sprinter, 5), S(12f, Runner, -1, 5)),
                W(S(0.3f, Mender, -1, 2), S(1f, Warden, -1, 2), S(5.5f, Swarm, -1, 5), S(9.5f, Splitter, -1, 3)),
                Boss(BossWarden, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Warden, 2), S(17f, Bulwark, -1, 2), S(22f, Sprinter, -1, 4))
            ));

            // 第三排开到三格，全书最大的盘面。
            s.Add(P("墨王", G("XXXXXX", "XXXXXX", "..XXX."),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Elite, -1, 2), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(3f, Bulwark, -1, 3), S(9f, Splitter, -1, 4)),
                W(S(0.3f, Ball, -1, 6), S(3.5f, Runner, -1, 5), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Crawler, -1, 5), S(4f, Belt, -1, 5), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Mender, 1), S(7.4f, Mender, 4), S(11.5f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Shield, -1, 3), S(9.5f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Warden, 2), S(8.5f, Warden, 3), S(12.5f, Splitter, -1, 4)),
                W(S(0.3f, Elite, -1, 3), S(4f, Warden, -1, 2), S(8.5f, Mender, -1, 2), S(12f, Runner, -1, 5)),
                Boss(BossKing, S(8f, BossMedic, 1), S(14f, Warden, 4), S(20f, Elite, -1, 2), S(27f, Sprinter, -1, 5))
            ));
        }
    }
}

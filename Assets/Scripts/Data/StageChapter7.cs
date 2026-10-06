using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第七章 · 雪原：第三排开到一到四格，还留着缺口。土和风进池，夯土、寒风两个组合解开。
    // 全混编，墨尊成群，没升过级的字和道具顶不住。
    public static partial class StageCatalog
    {
        static void Chapter7(List<Plan> s)
        {
            // 土把敌人往上推。冲得越快的，推回去越值。
            s.Add(P("冻土", G("XXXXXX", "XXXXX.", ".X...."),
                W(S(0.3f, Strafer, -1, 4), S(3f, Elite, 3), S(7f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Swarm, -1, 5), S(9f, Belt, -1, 3)),
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(4.5f, Mender, -1, 2), S(8.5f, Splitter, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Bulwark, -1, 2), S(8.5f, Strafer, -1, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Warden, 1), S(4.5f, Mender, 4), S(5.4f, Warden, 4), S(9.5f, Elite, -1, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Shield, -1, 3), S(8f, Runner, -1, 5))
            ).Give(Earth));

            // 墨尊扎堆往下压，一列打不完就得靠范围。
            s.Add(P("雪群", G("XXXXXX", "XXXXXX", "X....."),
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(4.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(1.5f, Mender, 2), S(2.1f, Mender, 3), S(7f, Swarm, -1, 5)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Sprinter, -1, 4), S(8f, Elite, -1, 2)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Splitter, -1, 3), S(8.5f, Strafer, -1, 3)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8f, Belt, -1, 3)),
                W(S(0.3f, Elite, -1, 3), S(4f, Shield, -1, 3), S(8.5f, Sprinter, -1, 4))
            ));

            // 风把敌人往旁边吹。横游、束墨本来就爱换列，正好吹进火力里。
            s.Add(P("起风", G("XXXXXX", "XXXXXX", "X....X"),
                W(S(0.3f, Strafer, -1, 4), S(3f, Belt, -1, 3), S(7f, Elite, 3)),
                W(S(0.3f, Belt, 0, 2), S(1f, Belt, 5, 2), S(4f, Warden, 2), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Strafer, 1), S(0.3f, Strafer, 4), S(3f, Sprinter, -1, 4), S(7.5f, Mender, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Runner, -1, 5), S(10f, Bulwark, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Strafer, -1, 4), S(8.5f, Warden, 1), S(9.1f, Warden, 4)),
                W(S(0.3f, Mender, 2), S(0.9f, Warden, 2), S(4.5f, Strafer, -1, 4), S(9f, Sprinter, -1, 4))
            ).Give(Wind));

            s.Add(P("雪墙", G("XXXXXX", "XXXXX.", "XX...."),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Bulwark, -1, 2), S(8f, Mender, -1, 2)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Sprinter, -1, 4), S(8f, Splitter, -1, 3)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Elite, 3), S(9.5f, Swarm, -1, 5)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Mender, 3), S(5.1f, Bulwark, 3), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Shield, -1, 3), S(9.5f, Sprinter, -1, 4))
            ));

            s.Add(P("雷鸣", G("XXXXXX", ".XXXX.", ".XXX.."),
                W(S(0.3f, Elite, 3), S(3f, Sprinter, -1, 4), S(7f, Warden, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Strafer, -1, 4), S(8f, Mender, 3), S(8.6f, Bulwark, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Splitter, -1, 3), S(10f, Swarm, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Sprinter, -1, 4), S(10f, Belt, -1, 3)),
                Boss(BossThunder, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Sprinter, -1, 4), S(17f, Warden, 3), S(22f, Runner, -1, 5))
            ));

            s.Add(P("冰河", G("XXXXXX", "XXXXX.", "X.X..."),
                W(S(0.3f, Strafer, -1, 4), S(3f, Swarm, -1, 5), S(6.5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Bulwark, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Belt, -1, 3), S(9f, Mender, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 3), S(8f, Warden, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Strafer, -1, 4), S(9.5f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Mender, -1, 2), S(8.5f, Sprinter, -1, 4))
            ));

            // 怪又多又快，第二排中间缺一格，中路得靠两边补。
            s.Add(P("暴雪", G("XXXXXX", "XX.XXX", "..X.X."),
                W(S(0.3f, Runner, -1, 5), S(2.5f, Swarm, -1, 5), S(6f, Sprinter, -1, 3)),
                W(S(0.3f, Elite, 2), S(1.2f, Elite, 3), S(4.5f, Belt, -1, 3), S(8.5f, Strafer, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Runner, -1, 5), S(9f, Splitter, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Mender, 2), S(4.1f, Bulwark, 2), S(8f, Crawler, -1, 3)),
                W(S(0.3f, Elite, -1, 3), S(4f, Ball, -1, 5), S(8f, Runner, -1, 5)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Sprinter, -1, 4))
            ));

            // 左下、右上各缺一角，斜着留一条通道。
            s.Add(P("斜阵", G("XXXX..", "XXXXXX", "..XXXX"),
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 5), S(7f, Warden, 1)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Bulwark, 2), S(4.1f, Bulwark, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Mender, 0), S(0.9f, Warden, 0), S(4.5f, Mender, 5), S(5.4f, Warden, 5), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Splitter, -1, 3), S(10f, Belt, -1, 3)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Shield, -1, 3), S(8f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(1.5f, Warden, 3), S(5.5f, Sprinter, -1, 4), S(10f, Mender, -1, 2))
            ));

            s.Add(P("墨医", G("XXXXXX", "XXXXXX", "..XX.."),
                W(S(0.3f, Elite, 2), S(0.9f, Mender, 2), S(4f, Elite, 4), S(4.6f, Mender, 4)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Sprinter, -1, 4), S(8f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 1), S(4f, Bulwark, 4), S(4.6f, Mender, 4), S(9f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Strafer, -1, 4), S(10f, Splitter, -1, 3)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Belt, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Mender, -1, 2), S(4.6f, Shield, -1, 3), S(9f, Crawler, -1, 3)),
                Boss(BossMedic, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Warden, 3), S(18f, Bulwark, -1, 2), S(24f, Sprinter, -1, 4))
            ));
        }
    }
}

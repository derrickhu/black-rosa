using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第七章 · 雪原：横移和反打进池，最后两块字齐了。全混编，墨尊成群。
    public static partial class StageCatalog
    {
        static void Chapter7(List<Plan> s)
        {
            // 风把敌人往旁边吹。横游、束墨本来就爱换列，正好吹进火力里。
            s.Add(P("起风", Full,
                W(S(0.3f, Strafer, -1, 4), S(3f, Belt, -1, 3), S(7f, Elite, 3)),
                W(S(0.3f, Belt, 0, 2), S(1f, Belt, 5, 2), S(4f, Warden, 2), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Strafer, 1), S(0.3f, Strafer, 4), S(3f, Sprinter, -1, 4), S(7.5f, Mender, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Runner, -1, 5), S(10f, Bulwark, -1, 2)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Strafer, -1, 4), S(8.5f, Warden, 1), S(9.1f, Warden, 4)),
                W(S(0.3f, Belt, -1, 4), S(3.5f, Elite, -1, 2), S(8f, Crawler, -1, 4)),
                W(S(0.3f, Mender, 2), S(0.9f, Warden, 2), S(4.5f, Strafer, -1, 4), S(9f, Sprinter, -1, 4))
            ).Give(Wind));

            // 惑让敌人互打，敌人越挤越值 —— 这一关墨尊成群压上来。
            s.Add(P("迷惑", Full,
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(4.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(1.5f, Mender, 2), S(2.1f, Mender, 3), S(7f, Swarm, -1, 6)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Sprinter, -1, 4), S(8f, Elite, -1, 2)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Splitter, -1, 4), S(8.5f, Strafer, -1, 4)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(7f, Mender, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 3), S(4f, Shield, -1, 3), S(8.5f, Sprinter, -1, 4))
            ).Give(Confuse));

            s.Add(P("残卷", Full,
                W(S(0.3f, Strafer, -1, 4), S(3f, Elite, 3), S(7f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Swarm, -1, 6), S(9f, Belt, -1, 4)),
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(4.5f, Mender, -1, 2), S(8.5f, Splitter, -1, 4)),
                W(S(0.3f, Sprinter, -1, 5), S(4f, Bulwark, -1, 3), S(8.5f, Strafer, -1, 4)),
                W(S(0.3f, Mender, 1), S(0.9f, Warden, 1), S(4.5f, Mender, 4), S(5.4f, Warden, 4), S(9.5f, Elite, -1, 2)),
                W(S(0.3f, Crawler, -1, 4), S(3.5f, Shield, -1, 3), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Sprinter, -1, 4), S(10f, Swarm, -1, 6))
            ).Put(2, 0, Wind).Put(3, 0, Confuse));

            s.Add(P("禁术", Full,
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Bulwark, -1, 3), S(8f, Mender, -1, 2)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Sprinter, -1, 4), S(8f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Elite, 3), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Mender, 3), S(5.1f, Bulwark, 3), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Shield, -1, 3), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Belt, -1, 5), S(4f, Warden, -1, 2), S(8.5f, Crawler, -1, 4))
            ).Rule(StageRule.NoSpell));

            s.Add(P("雷鸣", Full,
                W(S(0.3f, Elite, 3), S(3f, Sprinter, -1, 4), S(7f, Warden, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Strafer, -1, 4), S(8f, Mender, 3), S(8.6f, Bulwark, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Splitter, -1, 4), S(10f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Sprinter, -1, 4), S(10f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 3), S(4f, Mender, -1, 2), S(8.5f, Runner, -1, 5)),
                Boss(BossThunder, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Sprinter, -1, 4), S(17f, Warden, 3), S(22f, Runner, -1, 5))
            ));

            s.Add(P("三字", Full,
                W(S(0.3f, Strafer, -1, 4), S(3f, Swarm, -1, 6), S(6.5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Bulwark, -1, 3)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Belt, -1, 4), S(9f, Mender, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 4), S(8f, Warden, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Strafer, -1, 4), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Mender, -1, 2), S(8.5f, Sprinter, -1, 5))
            ).Limit(Wind, Confuse, Split));

            // 丰年叠疾行：怪又多又快，钱也多到可以连抽。
            s.Add(P("丰疾", Full,
                W(S(0.3f, Runner, -1, 5), S(2.5f, Swarm, -1, 6), S(6f, Sprinter, -1, 3)),
                W(S(0.3f, Elite, 2), S(1.2f, Elite, 3), S(4.5f, Belt, -1, 4), S(8.5f, Strafer, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Runner, -1, 5), S(9f, Splitter, -1, 4)),
                W(S(0.3f, Sprinter, -1, 5), S(3.5f, Mender, 2), S(4.1f, Bulwark, 2), S(8f, Crawler, -1, 4)),
                W(S(0.3f, Elite, -1, 3), S(4f, Ball, -1, 6), S(8f, Runner, -1, 5)),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Sprinter, -1, 4))
            ).Rule(StageRule.Rich | StageRule.Swift));

            // 残局：左下、右上各封一块，斜着留一条通道。
            s.Add(P("斜阵", Full,
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 5), S(7f, Warden, 1)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Bulwark, 2), S(4.1f, Bulwark, 3), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 0), S(0.9f, Warden, 0), S(4.5f, Mender, 5), S(5.4f, Warden, 5), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Splitter, -1, 4), S(10f, Belt, -1, 4)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Shield, -1, 3), S(8f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(1.5f, Warden, 3), S(5.5f, Sprinter, -1, 5), S(10f, Mender, -1, 2)),
                W(S(0.3f, Elite, -1, 3), S(4f, Strafer, -1, 4), S(8.5f, Crawler, -1, 4))
            ).Holes("XXX...", "XXXXXX", "...XXX"));

            s.Add(P("墨医", Full,
                W(S(0.3f, Elite, 2), S(0.9f, Mender, 2), S(4f, Elite, 4), S(4.6f, Mender, 4)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Sprinter, -1, 4), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 1), S(4f, Bulwark, 4), S(4.6f, Mender, 4), S(9f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Strafer, -1, 4), S(10f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Belt, -1, 4)),
                W(S(0.3f, Sprinter, -1, 5), S(4f, Mender, -1, 2), S(4.6f, Shield, -1, 3), S(9f, Crawler, -1, 4)),
                W(S(0.3f, Elite, -1, 3), S(4f, Bulwark, -1, 3), S(8.5f, Runner, -1, 5)),
                Boss(BossMedic, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Warden, 3), S(18f, Bulwark, -1, 2), S(24f, Sprinter, -1, 4))
            ));
        }
    }
}

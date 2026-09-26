using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第八章 · 火山：全池混编，规则叠着用，关底成对、连战，最后是墨王带墨医。
    public static partial class StageCatalog
    {
        static void Chapter8(List<Plan> s)
        {
            // 全池 25 个字，丰年的钱让人多抽几轮把想要的凑齐。
            s.Add(P("百字", Full,
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(4.5f, Runner, -1, 5), S(8.5f, Warden, 1)),
                W(S(0.3f, Warden, 4), S(1.5f, Mender, 3), S(2.1f, Bulwark, 3), S(6f, Swarm, -1, 6)),
                W(S(0.3f, Sprinter, -1, 5), S(3.5f, Splitter, -1, 4), S(8f, Elite, -1, 2)),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Shield, -1, 3), S(8f, Warden, 2), S(8.6f, Warden, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Mender, -1, 2), S(9f, Belt, -1, 5)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Crawler, -1, 4), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Elite, -1, 3), S(10f, Sprinter, -1, 4))
            ).Rule(StageRule.Rich));

            // 两只关底同一波上：鼓面挡一发，墨囊死了还要裂。
            s.Add(P("双鼓", Full,
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 5), S(7f, Warden, -1, 2)),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Mender, 2), S(4.1f, Bulwark, 2), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Splitter, -1, 4), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Belt, -1, 5), S(9f, Crawler, -1, 4)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Runner, -1, 5), S(8.5f, Elite, -1, 2)),
                Boss(BossDrum, S(0.3f, BossInkbag, 1), S(6f, Elite, 4), S(11f, Sprinter, -1, 4), S(16f, Warden, 2), S(21f, Swarm, -1, 6))
            ));

            // 残局叠孤城：格子缺一块，基地只剩一血。
            s.Add(P("孤局", Full,
                W(S(0.3f, Runner, -1, 5), S(3f, Elite, 3), S(7f, Strafer, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Splitter, -1, 4), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Mender, 3), S(5.1f, Bulwark, 3), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Belt, -1, 5), S(10f, Crawler, -1, 4)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8.5f, Elite, -1, 2))
            ).Holes("XXXXXX", "X.XX.X", "XX..XX").Rule(StageRule.Frail));

            // 疾行叠禁术：又快又没技能，全靠盘面。
            s.Add(P("疾禁", Full,
                W(S(0.3f, Runner, -1, 5), S(3f, Sprinter, -1, 4), S(6.5f, Elite, 3)),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Warden, 2), S(4.1f, Warden, 3), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Belt, -1, 5), S(9.5f, Splitter, -1, 4)),
                W(S(0.3f, Sprinter, -1, 5), S(3.5f, Mender, 2), S(4.1f, Bulwark, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Elite, -1, 2), S(10f, Crawler, -1, 4)),
                W(S(0.3f, Runner, 0, 3), S(1f, Runner, 5, 3), S(4f, Sprinter, -1, 5), S(8.5f, Shield, -1, 3))
            ).Rule(StageRule.Swift | StageRule.NoSpell));

            s.Add(P("医馆", Full,
                W(S(0.3f, Elite, 2), S(0.9f, Mender, 2), S(4f, Elite, 4), S(4.6f, Mender, 4)),
                W(S(0.3f, Warden, -1, 2), S(3.5f, Runner, -1, 5), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 1), S(4f, Bulwark, 4), S(4.6f, Mender, 4), S(9f, Sprinter, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Strafer, -1, 5), S(10f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Belt, -1, 5)),
                Boss(BossMedic, S(4f, Elite, 1), S(6f, Elite, 4), S(11f, Warden, 3), S(16f, Bulwark, -1, 2), S(21f, Runner, -1, 5))
            ));

            // 限字：五个互不成词的单字，只能靠星级硬叠。
            s.Add(P("五字", Full,
                W(S(0.3f, Strafer, -1, 5), S(3f, Swarm, -1, 6), S(6.5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Bulwark, -1, 3)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Belt, -1, 5), S(9f, Mender, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Splitter, -1, 4), S(8f, Warden, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Strafer, -1, 4), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Mender, -1, 2), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Runner, -1, 5))
            ).Limit(Fire, Thunder, Poison, Heavy, Pierce));

            // 精英潮：整波整波的墨尊，中间夹一波喘气的杂兵。开局送一组二星秒杀。
            s.Add(P("精英潮", Full,
                W(S(0.3f, Elite, 1), S(0.3f, Elite, 4), S(3f, Elite, 2), S(3f, Elite, 3)),
                W(S(0.3f, Runner, -1, 5), S(3f, Swarm, -1, 6), S(6.5f, Ball, -1, 6)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6f, Mender, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Sprinter, -1, 5), S(9f, Belt, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(2.5f, Elite, -1, 3), S(6.5f, Mender, 2), S(7.1f, Mender, 3)),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Splitter, -1, 4), S(8f, Crawler, -1, 4)),
                W(S(0.3f, Elite, 0), S(0.3f, Elite, 5), S(2.5f, Elite, 1), S(2.5f, Elite, 4), S(5f, Elite, 2), S(5f, Elite, 3))
            ).Put(2, 0, Sec, 2).Put(2, 1, Kill, 2));

            // 连战：铁桶、双首、牢头各占一波，一只接一只。
            s.Add(P("连战", Full,
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 5), S(7f, Warden, -1, 2)),
                W(S(0.3f, BossIron, 3), S(5f, Bulwark, 1), S(7f, Bulwark, 4), S(12f, Crawler, -1, 4), S(16f, Mender, 2)),
                W(S(0.3f, Sprinter, -1, 5), S(4f, Strafer, -1, 5), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, BossTwin, 1), S(0.3f, BossTwin, 4), S(5f, Sprinter, 0), S(7f, Sprinter, 5), S(12f, Runner, -1, 5)),
                W(S(0.3f, Mender, -1, 2), S(1f, Warden, -1, 2), S(5.5f, Swarm, -1, 6), S(9.5f, Splitter, -1, 4)),
                Boss(BossWarden, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Warden, 2), S(17f, Bulwark, -1, 2), S(22f, Sprinter, -1, 5))
            ));

            s.Add(P("墨王", Full,
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

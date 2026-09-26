using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第五章 · 森林：吸血和叠层毒进池，墨尊登场。
    public static partial class StageCatalog
    {
        static void Chapter5(List<Plan> s)
        {
            s.Add(P("生木", Full,
                W(S(0.3f, Bulwark, 3), S(3f, Sprinter, -1, 2), S(7f, Mender, 2)),
                W(S(0.3f, Elite, 3)),
                W(S(0.3f, Elite, 1), S(2.5f, Elite, 4), S(7.5f, Shield, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Crawler, -1, 3), S(9.5f, Swarm, 3, 6)),
                W(S(0.3f, Elite, 2), S(2.5f, Mender, 3), S(6.5f, Bulwark, -1, 2)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Runner, -1, 3), S(9.5f, Strafer, -1, 2)),
                W(S(0.3f, Elite, 0), S(2.5f, Elite, 5), S(7.5f, Shield, 3), S(11.5f, Mender, 2))
            ).Give(Wood));

            // 毒能叠层，专克壁垒这种减伤肉。
            s.Add(P("蚀毒", Full,
                W(S(0.3f, Bulwark, 2), S(0.3f, Bulwark, 3), S(4f, Runner, -1, 3)),
                W(S(0.3f, Bulwark, 0), S(1f, Bulwark, 5), S(4f, Elite, 3), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 1), S(0.9f, Bulwark, 1), S(4.5f, Mender, 4), S(5.1f, Bulwark, 4)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Splitter, -1, 3), S(8.5f, Shield, -1, 2)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Strafer, -1, 3), S(8.5f, Elite, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Bulwark, 2), S(4.1f, Bulwark, 3), S(8.5f, Runner, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.5f, Bulwark, 4), S(5.5f, Mender, -1, 2), S(10f, Ball, -1, 6))
            ).Give(Poison));

            s.Add(P("孤城", Full,
                W(S(0.3f, Runner, -1, 3), S(3f, Shield, -1, 2), S(7f, Sprinter, -1, 2)),
                W(S(0.3f, Elite, 3), S(3f, Splitter, -1, 3), S(7.5f, Strafer, -1, 2)),
                W(S(0.3f, Bulwark, 1), S(1.5f, Bulwark, 4), S(5f, Mender, 3), S(9f, Swarm, -1, 6)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Belt, -1, 3), S(8f, Crawler, -1, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Runner, -1, 4))
            ).Rule(StageRule.Frail));

            // 残卷：中间两列各摆一张毒，一开局就能看到毒层怎么叠。
            s.Add(P("残卷", Full,
                W(S(0.3f, Bulwark, 2), S(1f, Bulwark, 3), S(4.5f, Swarm, -1, 6)),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Mender, 3), S(7f, Splitter, -1, 3)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Sprinter, -1, 3), S(8f, Bulwark, -1, 2)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Crawler, -1, 3), S(8.5f, Elite, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Bulwark, 1), S(4.5f, Mender, 4), S(5.1f, Bulwark, 4), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Elite, 2), S(1.5f, Elite, 3), S(5.5f, Splitter, -1, 3), S(10f, Ball, -1, 6)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Sprinter, -1, 3), S(8.5f, Shield, -1, 3))
            ).Put(2, 0, Poison).Put(3, 0, Poison));

            s.Add(P("双影", Full,
                W(S(0.3f, Elite, 3), S(3f, Sprinter, -1, 3), S(7f, Mender, 2)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Splitter, -1, 3), S(8f, Runner, -1, 4)),
                W(S(0.3f, Strafer, -1, 3), S(3.5f, Shield, -1, 3), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Crawler, -1, 3), S(9.5f, Belt, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(1f, Bulwark, -1, 2), S(5.5f, Sprinter, -1, 3)),
                Boss(BossTwin, S(4f, Elite, 3), S(9f, Sprinter, 0), S(9f, Sprinter, 5), S(14f, Bulwark, -1, 2), S(19f, Runner, -1, 4))
            ));

            // 护送：墨尊身后跟着医墨赶路（疾行），先杀谁是这一关唯一的问题。
            s.Add(P("护送", Full,
                W(S(0.3f, Elite, 2), S(0.9f, Mender, 2), S(4.5f, Elite, 4), S(5.1f, Mender, 4)),
                W(S(0.3f, Runner, -1, 4), S(3f, Splitter, -1, 3), S(7.5f, Shield, -1, 3)),
                W(S(0.3f, Elite, 0), S(0.9f, Mender, 0), S(3.5f, Elite, 5), S(4.1f, Mender, 5), S(8f, Bulwark, 3)),
                W(S(0.3f, Sprinter, -1, 3), S(3.5f, Strafer, -1, 3), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 1), S(4f, Bulwark, 4), S(4.6f, Mender, 4), S(8.5f, Crawler, -1, 3)),
                W(S(0.3f, Elite, 2), S(0.9f, Elite, 3), S(1.5f, Mender, 2), S(2.1f, Mender, 3), S(7f, Runner, -1, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Elite, -1, 2), S(8.5f, Mender, -1, 2))
            ).Rule(StageRule.Swift));

            s.Add(P("禁术", Full,
                W(S(0.3f, Shield, -1, 3), S(3f, Elite, 3), S(7f, Runner, -1, 4)),
                W(S(0.3f, Bulwark, 1), S(1.5f, Bulwark, 4), S(5f, Mender, -1, 2), S(9f, Splitter, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Strafer, -1, 3), S(8.5f, Crawler, -1, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Shield, -1, 3), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Mender, 3), S(5.1f, Bulwark, 3), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Elite, -1, 2), S(8.5f, Sprinter, -1, 3))
            ).Rule(StageRule.NoSpell));

            // 残局：中间被封，只剩两侧几条窄巷能放字。
            s.Add(P("窄巷", Full,
                W(S(0.3f, Runner, 0, 3), S(1.5f, Runner, 5, 3), S(5f, Elite, 2)),
                W(S(0.3f, Bulwark, 2), S(1f, Bulwark, 3), S(4.5f, Strafer, -1, 3), S(8.5f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 1), S(0.9f, Shield, 1), S(4.5f, Mender, 4), S(5.1f, Shield, 4), S(9f, Sprinter, -1, 3)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Crawler, -1, 3), S(8.5f, Elite, 3)),
                W(S(0.3f, Belt, -1, 4), S(3.5f, Runner, -1, 4), S(7.5f, Bulwark, -1, 2)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Mender, -1, 2), S(9.5f, Sprinter, -1, 3))
            ).Holes("XXXXXX", "XX..XX", "X....X"));

            s.Add(P("牢头", Full,
                W(S(0.3f, Elite, 3), S(3f, Bulwark, -1, 2), S(7f, Sprinter, -1, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Shield, 1), S(4.5f, Mender, 4), S(5.1f, Shield, 4), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Strafer, -1, 3), S(8.5f, Swarm, -1, 6)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Bulwark, 2), S(6.1f, Bulwark, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Crawler, -1, 3), S(8.5f, Mender, -1, 2)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Elite, -1, 2), S(8f, Belt, -1, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Splitter, -1, 3), S(8.5f, Runner, -1, 4)),
                Boss(BossWarden, S(5f, Elite, 1), S(7f, Elite, 4), S(12f, Bulwark, -1, 2), S(17f, Sprinter, -1, 3), S(22f, Mender, 3))
            ));
        }
    }
}

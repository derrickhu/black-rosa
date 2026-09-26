using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第六章 · 河滩：范围缓和上推进池，镇守登场 —— 纯控制流从这章开始吃瘪。
    public static partial class StageCatalog
    {
        static void Chapter6(List<Plan> s)
        {
            s.Add(P("止水", Full,
                W(S(0.3f, Elite, 3), S(3f, Sprinter, -1, 3), S(7f, Bulwark, 2)),
                W(S(0.3f, Warden, 3)),
                W(S(0.3f, Warden, 1), S(2.5f, Warden, 4), S(7.5f, Mender, 3)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Sprinter, -1, 3), S(9.5f, Swarm, 2, 6)),
                W(S(0.3f, Warden, 2), S(2.5f, Elite, 3), S(7f, Bulwark, -1, 2)),
                W(S(0.3f, Mender, 0), S(0.9f, Shield, 0), S(4.5f, Mender, 5), S(5.4f, Shield, 5), S(9.5f, Runner, -1, 4)),
                W(S(0.3f, Warden, 0), S(2.5f, Warden, 5), S(7.5f, Elite, 3), S(11.5f, Strafer, -1, 3))
            ).Give(Water));

            // 土往上推，快脚和狂奔越快，推回去越值。
            s.Add(P("厚土", Full,
                W(S(0.3f, Runner, -1, 4), S(2.5f, Sprinter, -1, 2), S(6f, Warden, 3)),
                W(S(0.3f, Sprinter, 0), S(0.9f, Sprinter, 2), S(1.5f, Sprinter, 3), S(2.1f, Sprinter, 5)),
                W(S(0.3f, Runner, 0, 3), S(1.2f, Runner, 5, 3), S(4.5f, Elite, -1, 2), S(9f, Belt, -1, 4)),
                W(S(0.3f, Warden, 2), S(2f, Mender, 3), S(5.5f, Sprinter, -1, 4), S(10f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(3.5f, Runner, -1, 5), S(8f, Sprinter, -1, 3)),
                W(S(0.3f, Strafer, -1, 4), S(4f, Splitter, -1, 3), S(8.5f, Warden, 1), S(9.1f, Warden, 4)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Elite, 2), S(4.1f, Elite, 3), S(8f, Runner, -1, 5))
            ).Give(Earth));

            s.Add(P("丰年", Full,
                W(S(0.3f, Swarm, -1, 6), S(2.5f, Runner, -1, 4), S(6f, Warden, 3)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Splitter, -1, 3), S(9f, Ball, -1, 6)),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.5f, Mender, 3), S(5.1f, Bulwark, 3), S(9.5f, Strafer, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Crawler, -1, 4), S(8.5f, Shield, -1, 3)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Elite, 3), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Splitter, -1, 4), S(3.5f, Belt, -1, 4), S(8f, Mender, -1, 2)),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Bulwark, -1, 3), S(8.5f, Runner, -1, 5))
            ).Rule(StageRule.Rich));

            s.Add(P("三字", Full,
                W(S(0.3f, Runner, -1, 4), S(3f, Swarm, -1, 6), S(6.5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(2f, Sprinter, -1, 3), S(6f, Bulwark, -1, 2)),
                W(S(0.3f, Strafer, -1, 4), S(4f, Splitter, -1, 3), S(8.5f, Mender, 3), S(9.1f, Shield, 3)),
                W(S(0.3f, Sprinter, 0, 2), S(1.2f, Sprinter, 5, 2), S(4.5f, Crawler, -1, 4), S(9f, Warden, 3)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Runner, -1, 5), S(9.5f, Ball, -1, 6)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Mender, -1, 2), S(8.5f, Sprinter, -1, 4))
            ).Limit(Water, Earth, Ice));

            s.Add(P("牢门", Full,
                W(S(0.3f, Warden, 3), S(3f, Elite, -1, 2), S(7f, Sprinter, -1, 3)),
                W(S(0.3f, Mender, 1), S(0.9f, Bulwark, 1), S(4.5f, Mender, 4), S(5.1f, Bulwark, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Runner, -1, 5), S(8.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Shield, -1, 3), S(9.5f, Strafer, -1, 3)),
                W(S(0.3f, Elite, 2), S(1.5f, Elite, 3), S(5.5f, Sprinter, -1, 4)),
                Boss(BossWarden, S(4f, Warden, 1), S(6f, Warden, 4), S(11f, Elite, 3), S(16f, Mender, -1, 2), S(21f, Sprinter, -1, 3))
            ));

            // 残局：第二、三排隔一格封一格，像棋盘，一列顶多两张字。
            s.Add(P("棋盘", Full,
                W(S(0.3f, Elite, 3), S(3f, Runner, -1, 4), S(7f, Warden, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Bulwark, 1), S(4.6f, Bulwark, 4), S(8.5f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 2), S(0.9f, Shield, 2), S(4.5f, Mender, 3), S(5.1f, Shield, 3), S(9.5f, Splitter, -1, 3)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Strafer, -1, 4), S(10f, Crawler, -1, 3)),
                W(S(0.3f, Elite, -1, 2), S(3.5f, Runner, -1, 5), S(8f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Sprinter, -1, 4), S(8.5f, Mender, -1, 2), S(9.1f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Splitter, -1, 4), S(9f, Ball, -1, 6))
            ).Holes("XXXXXX", ".X.X.X", "X.X.X."));

            s.Add(P("疾行", Full,
                W(S(0.3f, Runner, -1, 5), S(3f, Sprinter, -1, 3), S(6.5f, Strafer, -1, 3)),
                W(S(0.3f, Elite, 2), S(1.5f, Elite, 3), S(5f, Belt, -1, 4), S(9f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Runner, -1, 5), S(9f, Splitter, -1, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Mender, 3), S(4.1f, Bulwark, 3), S(8f, Crawler, -1, 4)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Elite, -1, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Sprinter, 0, 2), S(1f, Sprinter, 5, 2), S(4f, Warden, 2), S(4.6f, Warden, 3), S(9f, Ball, -1, 6))
            ).Rule(StageRule.Swift));

            s.Add(P("孤城", Full,
                W(S(0.3f, Warden, 3), S(3f, Runner, -1, 4), S(7f, Elite, 2)),
                W(S(0.3f, Sprinter, -1, 4), S(4f, Bulwark, -1, 2), S(8f, Mender, 3)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Splitter, -1, 4), S(10f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5f, Strafer, -1, 4), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Sprinter, -1, 4), S(8f, Shield, -1, 3))
            ).Rule(StageRule.Frail));

            s.Add(P("奔雷", Full,
                W(S(0.3f, Warden, 3), S(3f, Sprinter, -1, 3), S(7f, Elite, -1, 2)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Belt, -1, 4), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 1), S(0.9f, Warden, 1), S(4.5f, Mender, 4), S(5.4f, Warden, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Bulwark, 3), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Strafer, -1, 4), S(8.5f, Crawler, -1, 4)),
                W(S(0.3f, Warden, 2), S(1.5f, Warden, 3), S(5.5f, Elite, -1, 2), S(10f, Mender, -1, 2)),
                W(S(0.3f, Sprinter, -1, 5), S(3.5f, Runner, -1, 5), S(8f, Bulwark, -1, 3)),
                Boss(BossThunder, S(6f, Warden, 1), S(9f, Warden, 4), S(14f, Sprinter, -1, 4), S(20f, Elite, 3), S(25f, Runner, -1, 5))
            ));
        }
    }
}

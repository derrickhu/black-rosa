using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第三章 · 石院：第三排从中间两格开到满，两组成词字进池。
    public static partial class StageCatalog
    {
        static void Chapter3(List<Plan> s)
        {
            s.Add(P("三层", Row(6, 6, 2),
                W(S(0.3f, Strafer, -1, 2), S(3f, Runner, -1, 2), S(7f, Belt, 3)),
                W(S(0.3f, Shield, 2), S(3f, Shield, 3)),
                W(S(0.3f, Shield, 0), S(1.8f, Shield, 5), S(5.5f, Crawler, -1, 2), S(9.5f, Runner, -1, 2)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Strafer, -1, 2), S(8.5f, Swarm, 2, 5)),
                W(S(0.3f, Shield, -1, 3), S(4f, Chubby, -1, 2), S(8.5f, Runner, -1, 3)),
                W(S(0.3f, Crawler, 1), S(1.5f, Crawler, 4), S(5f, Shield, 2), S(5.6f, Shield, 3), S(9.5f, Ball, -1, 5))
            ));

            // 秒杀：一大一小两种肉，正好用来看「秒」掉的那一下。
            s.Add(P("秒杀", Row(6, 6, 2),
                W(S(0.3f, Chubby, -1, 2), S(3f, Shield, 3), S(6.5f, Runner, -1, 2)),
                W(S(0.3f, BigHead, 2, 2), S(3f, Chubby, 3, 2), S(6.5f, Belt, -1, 2)),
                W(S(0.3f, Shield, 1), S(1.5f, Shield, 4), S(5f, Strafer, -1, 2), S(9f, Crawler, -1, 2)),
                W(S(0.3f, Chubby, -1, 3), S(3.5f, Swarm, -1, 5), S(8f, BigHead, -1, 2)),
                W(S(0.3f, Belt, 0), S(1.2f, Belt, 5), S(4.5f, Shield, -1, 2), S(8.5f, Runner, -1, 3)),
                W(S(0.3f, Crawler, -1, 3), S(3.5f, Chubby, 2), S(4.1f, Chubby, 3), S(8f, Strafer, -1, 2))
            ).Give(Sec, Kill));

            s.Add(P("裂墨", Row(6, 6, 3),
                W(S(0.3f, Shield, -1, 2), S(3f, Strafer, -1, 2), S(7f, Runner, -1, 2)),
                W(S(0.3f, Splitter, 2), S(3.5f, Splitter, 3)),
                W(S(0.3f, Splitter, 0), S(1.8f, Splitter, 5), S(5f, Crawler, -1, 2), S(9f, Strafer, 3)),
                W(S(0.3f, Belt, -1, 2), S(3.5f, Runner, -1, 3), S(8.5f, Chubby, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4.5f, Swarm, 2, 5), S(9f, BigHead, -1, 2)),
                W(S(0.3f, Strafer, 1), S(2f, Strafer, 4), S(5.5f, Splitter, 3), S(10f, Shield, -1, 2))
            ));

            // 残卷：第三列一上一下已经拼好「击退」，玩家只管往两边铺。
            s.Add(P("残卷", Row(6, 6, 3),
                W(S(0.3f, Runner, -1, 3), S(3f, Chubby, 2), S(3.6f, Chubby, 3), S(7f, Belt, -1, 2)),
                W(S(0.3f, Splitter, -1, 2), S(3.5f, Crawler, 2), S(4.1f, Crawler, 3), S(8f, Swarm, -1, 5)),
                W(S(0.3f, BigHead, 2, 2), S(2.5f, Tall, 2, 2), S(5.5f, Shield, -1, 2), S(9f, Strafer, -1, 2)),
                W(S(0.3f, Chubby, -1, 3), S(3.5f, Runner, -1, 3), S(7.5f, Splitter, -1, 2)),
                W(S(0.3f, Crawler, -1, 3), S(3f, Shield, 1), S(3.8f, Shield, 4), S(8f, Belt, -1, 2)),
                W(S(0.3f, Splitter, 2), S(1f, Splitter, 3), S(4.5f, BigHead, -1, 3), S(9f, Runner, -1, 3))
            ).Put(2, 0, Strike).Put(2, 1, Back));

            s.Add(P("囊中", Row(6, 6, 4),
                W(S(0.3f, Shield, -1, 2), S(3f, Splitter, -1, 2), S(7f, Runner, -1, 2)),
                W(S(0.3f, Strafer, -1, 2), S(3f, Belt, -1, 2), S(7.5f, Swarm, 3, 5)),
                W(S(0.3f, Crawler, 1), S(1.5f, Crawler, 4), S(5f, Splitter, 3), S(9f, Chubby, -1, 2)),
                W(S(0.3f, Runner, -1, 3), S(3.5f, Shield, 2), S(4.1f, Shield, 3), S(8.5f, BigHead, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Strafer, -1, 2), S(8.5f, Ball, -1, 5)),
                Boss(BossInkbag, S(4f, Shield, 0), S(6f, Shield, 5), S(10f, Splitter, -1, 2), S(15f, Runner, -1, 3))
            ));

            // 连斩：成团的墨群一团一团压下来，斩一刀带一片。
            s.Add(P("连斩", Row(6, 6, 4),
                W(S(0.3f, Swarm, 2, 5), S(2.5f, Swarm, 3, 5), S(6f, Runner, -1, 2)),
                W(S(0.3f, Splitter, -1, 2), S(3f, Swarm, 1, 5), S(5f, Swarm, 4, 5), S(9f, Shield, 3)),
                W(S(0.3f, Chubby, -1, 2), S(2.5f, Ball, -1, 5), S(6f, Swarm, 2, 6), S(9.5f, Belt, -1, 2)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Crawler, -1, 3), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Swarm, 0, 5), S(1.5f, Swarm, 5, 5), S(4.5f, Splitter, -1, 3), S(9f, BigHead, -1, 2)),
                W(S(0.3f, Shield, -1, 2), S(3f, Swarm, 3, 6), S(6.5f, Runner, -1, 3), S(10f, Splitter, 2))
            ).Give(Link, Slash));

            s.Add(P("狂奔", Row(6, 6, 5),
                W(S(0.3f, Splitter, -1, 2), S(3f, Shield, 3), S(6.5f, Runner, -1, 3)),
                W(S(0.3f, Sprinter, 2), S(3f, Sprinter, 3)),
                W(S(0.3f, Sprinter, 0), S(1.6f, Sprinter, 5), S(5f, Splitter, -1, 2), S(9.5f, Chubby, 3)),
                W(S(0.3f, Strafer, -1, 2), S(3.5f, Crawler, -1, 3), S(8.5f, Swarm, 2, 5)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Runner, -1, 3), S(9.5f, Belt, -1, 2)),
                W(S(0.3f, Shield, 1), S(1.2f, Shield, 4), S(5f, Sprinter, -1, 2), S(9f, Splitter, -1, 2)),
                W(S(0.3f, Sprinter, 2), S(0.9f, Sprinter, 3), S(4.5f, BigHead, -1, 3), S(8.5f, Runner, -1, 3))
            ));

            // 禁术：技能按不了，一排盾墙只能靠字硬推。
            s.Add(P("禁术", Row(6, 6, 5),
                W(S(0.3f, Shield, 1), S(0.3f, Shield, 2), S(0.3f, Shield, 3), S(0.3f, Shield, 4)),
                W(S(0.3f, Sprinter, -1, 2), S(3f, Splitter, -1, 2), S(7f, Runner, -1, 3)),
                W(S(0.3f, Shield, 0, 2), S(1.5f, Shield, 5, 2), S(5f, Crawler, -1, 3), S(9f, Swarm, 3, 5)),
                W(S(0.3f, Strafer, -1, 3), S(4f, Belt, -1, 2), S(8.5f, Chubby, -1, 3)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Sprinter, -1, 2), S(8f, Splitter, -1, 3)),
                W(S(0.3f, Shield, 1), S(0.3f, Shield, 4), S(3f, Shield, 2), S(3f, Shield, 3), S(7f, Runner, -1, 4))
            ).Rule(StageRule.NoSpell));

            s.Add(P("铁桶", Row(6, 6, 6),
                W(S(0.3f, Shield, -1, 2), S(3f, Sprinter, -1, 2), S(7f, Crawler, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Strafer, -1, 2), S(8.5f, Swarm, 2, 6)),
                W(S(0.3f, Crawler, 0), S(1.2f, Crawler, 5), S(4.5f, Shield, 2), S(5.1f, Shield, 3), S(9f, Runner, -1, 3)),
                W(S(0.3f, Sprinter, -1, 3), S(4f, Belt, -1, 2), S(8.5f, BigHead, -1, 3)),
                W(S(0.3f, Chubby, -1, 3), S(3.5f, Splitter, -1, 3), S(8.5f, Strafer, -1, 2)),
                W(S(0.3f, Shield, -1, 3), S(3.5f, Crawler, -1, 3), S(8f, Sprinter, 2), S(8.6f, Sprinter, 3)),
                W(S(0.3f, Runner, -1, 4), S(3.5f, Splitter, 1), S(4.2f, Splitter, 4), S(8.5f, Ball, -1, 6)),
                Boss(BossIron, S(5f, Crawler, 0), S(7f, Crawler, 5), S(12f, Shield, -1, 2), S(17f, Sprinter, -1, 2), S(22f, Splitter, 3))
            ));
        }
    }
}

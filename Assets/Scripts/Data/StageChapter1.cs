using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第一章 · 草地：只开第一排，一关给一个字、一只纯墨兵。
    // 前两关是教学，没有关底；1-9 是章底鼓面。
    public static partial class StageCatalog
    {
        static void Chapter1(List<Plan> s)
        {
            // 只开中间两格、只有「分」一个字、两波杂兵，三十秒内通关。
            // 怪全走开放的那两列，免得「放了字却没反应」。
            s.Add(P("起笔", Row(2),
                W(S(0.3f, Walker, 2), S(2.6f, Walker, 3), S(5f, Walker, 2)),
                W(S(0.3f, Walker, 3), S(2f, Walker, 2), S(4f, Walker, 3), S(6f, Walker, 2))
            ).Give(Split).Teach(true, false));

            s.Add(P("两点", Row(3),
                W(S(0.3f, Walker, 2), S(2f, Walker, 3), S(4.5f, Walker, 1)),
                W(S(0.3f, Swarm, 2, 3), S(3.5f, Swarm, 3, 3)),
                W(S(0.3f, Walker, 1), S(1.8f, Swarm, 3, 3), S(4.5f, Walker, 2), S(6.5f, Walker, 3))
            ).Give(Fire).Teach(true, true));

            s.Add(P("团墨", Row(4),
                W(S(0.3f, Walker, 1), S(2f, Walker, 4), S(4.5f, Swarm, 2, 3)),
                W(S(0.3f, Chubby, 2), S(3.5f, Chubby, 3)),
                W(S(0.3f, Chubby, 1), S(2f, Walker, 3), S(4f, Walker, 4), S(6.5f, Swarm, 2, 3))
            ).Give(Ice));

            s.Add(P("长墨", Row(5),
                W(S(0.3f, Chubby, 2), S(2f, Walker, -1, 2), S(5f, Swarm, 4, 3)),
                W(S(0.3f, Tall, 2), S(3f, Tall, 4)),
                W(S(0.3f, Tall, 1), S(1.8f, Chubby, 3), S(4.5f, Walker, -1, 2), S(7f, Swarm, 0, 3)),
                W(S(0.3f, Tall, -1, 2), S(3f, Chubby, 2), S(5.5f, Tall, 4))
            ).Give(Heavy));

            // 丰年：怪多、钱也多，第一次让人感到「多抽几次」是划算的。
            s.Add(P("一行", Row(6),
                W(S(0.3f, Walker, -1, 3), S(3f, Tall, 0), S(5.5f, Tall, 5)),
                W(S(0.3f, Ball, 2), S(2f, Ball, 3), S(4f, Ball, 1)),
                W(S(0.3f, Chubby, 2), S(1.8f, Chubby, 3), S(4.5f, Ball, -1, 2), S(7.5f, Swarm, 5, 3)),
                W(S(0.3f, Ball, -1, 3), S(3f, Tall, -1, 2), S(6.5f, Walker, -1, 2))
            ).Give(Accel).Rule(StageRule.Rich));

            // 残卷：开局第三列已经摆着一张穿，大头排成一串正好一穿到底。
            s.Add(P("大头", Row(6),
                W(S(0.3f, Ball, -1, 3), S(3f, Chubby, 2), S(5.5f, Tall, 4)),
                W(S(0.3f, BigHead, 2), S(3f, BigHead, 2), S(5.5f, BigHead, 2)),
                W(S(0.3f, BigHead, 1), S(1.8f, BigHead, 4), S(4.5f, Swarm, 2, 4), S(7.5f, Tall, 0)),
                W(S(0.3f, Chubby, -1, 2), S(3f, Ball, -1, 3), S(6.5f, BigHead, 3), S(9f, Walker, -1, 2)),
                W(S(0.3f, BigHead, 2, 2), S(3.5f, BigHead, 3, 2), S(7f, Tall, -1, 2))
            ).Give(Pierce).Put(2, 0, Pierce));

            s.Add(P("墨潮", Row(6),
                W(S(0.3f, Swarm, 2, 4), S(3f, Swarm, 3, 4)),
                W(S(0.3f, Chubby, 1), S(1.5f, Chubby, 4), S(4f, Swarm, -1, 4), S(7f, Ball, -1, 3)),
                W(S(0.3f, Swarm, 0, 3), S(1.5f, Swarm, 5, 3), S(4f, Swarm, 2, 5), S(7.5f, BigHead, -1, 2)),
                W(S(0.3f, Tall, -1, 3), S(3f, Swarm, 3, 5), S(6f, Chubby, 2)),
                W(S(0.3f, Swarm, -1, 5), S(3f, Swarm, -1, 5), S(6f, BigHead, 2), S(6.6f, BigHead, 3))
            ).Give(Explode));

            // 限字：只抽分、火、炸三张，逼玩家把同一张升到三星。
            s.Add(P("三字", Row(6),
                W(S(0.3f, Walker, -1, 3), S(2.5f, Ball, -1, 3), S(5f, Chubby, 3)),
                W(S(0.3f, Swarm, 2, 5), S(3f, Tall, -1, 2), S(6f, BigHead, 1)),
                W(S(0.3f, Chubby, 1), S(1.2f, Chubby, 4), S(4f, Ball, -1, 4), S(7f, Swarm, 3, 5)),
                W(S(0.3f, BigHead, -1, 2), S(3f, Tall, -1, 3), S(6.5f, Walker, -1, 3)),
                W(S(0.3f, Chubby, -1, 2), S(2.5f, Swarm, -1, 5), S(6f, BigHead, -1, 2), S(8.5f, Ball, -1, 3))
            ).Limit(Split, Fire, Explode));

            s.Add(P("鼓面", Row(6),
                W(S(0.3f, Walker, -1, 3), S(2.5f, Tall, -1, 2), S(5.5f, Ball, -1, 3)),
                W(S(0.3f, Chubby, 2), S(1.2f, Chubby, 3), S(4f, Swarm, -1, 5), S(7f, BigHead, -1, 2)),
                W(S(0.3f, Ball, -1, 4), S(3f, Tall, 0), S(3.6f, Tall, 5), S(6.5f, Chubby, -1, 2)),
                W(S(0.3f, BigHead, -1, 3), S(3.5f, Swarm, 2, 5), S(7f, Walker, -1, 3)),
                W(S(0.3f, Chubby, -1, 3), S(3f, Ball, -1, 4), S(6.5f, Tall, -1, 3)),
                Boss(BossDrum, S(4f, Tall, 1), S(6f, Tall, 4), S(9.5f, Chubby, 2), S(12f, BigHead, 3), S(16f, Swarm, -1, 5))
            ));
        }
    }
}

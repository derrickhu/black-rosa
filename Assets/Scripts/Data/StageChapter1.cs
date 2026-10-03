using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第一章 · 草地：教玩法的一章。前七关只开第一排，格子从两格慢慢加到六格，
    // 位置每关都挪一挪；第八关第一次长出第二排，霜火这个隐藏组合才摆得出来。
    // 只用四种纯墨兵加一只大头，怪慢、怪少，每关都有一样新东西。
    public static partial class StageCatalog
    {
        static void Chapter1(List<Plan> s)
        {
            // 只开中间两格、只有「分」一个字。怪全走这两列，放下去马上看得到效果。
            s.Add(P("起笔", G("..XX.."),
                W(S(0.3f, Walker, 2), S(3.5f, Walker, 3), S(7f, Walker, 2)),
                W(S(0.3f, Walker, 3), S(3.5f, Walker, 2), S(7f, Walker, 3, 2))
            ).Give(Split).Teach(true, false));

            // 这一关已经装上弹弓，多一波收尾。总数只多两只，三格还压在每格 5 只里面。
            s.Add(P("落火", G(".XXX.."),
                W(S(0.3f, Walker, 1), S(3.5f, Walker, 2), S(7f, Walker, 3)),
                W(S(0.3f, Walker, 2), S(2.5f, Walker, 1), S(5f, Walker, 3), S(7.5f, Walker, 2, 2)),
                W(S(0.3f, Walker, 2), S(4f, Walker, 1), S(7.5f, Walker, 3))
            ).Give(Fire).Teach(true, true));

            // 团墨血厚走得慢，冰正好让它在火力里多待一会儿。
            s.Add(P("团墨", G(".XXXX."),
                W(S(0.3f, Walker, 2), S(3.5f, Walker, 4), S(7f, Walker, 3)),
                W(S(0.3f, Chubby, 2), S(5f, Chubby, 3)),
                W(S(0.3f, Walker, 1), S(3f, Chubby, 4), S(5.5f, Walker, 2), S(8f, Walker, 3, 2))
            ).Give(Ice));

            // 格子拆成三块。长墨更硬，重字一发顶几发。
            s.Add(P("长墨", G("X.XX.X"),
                W(S(0.3f, Walker, 0), S(2f, Walker, 5), S(6f, Chubby, 2)),
                W(S(0.3f, Tall, 3), S(5f, Tall, 2)),
                W(S(0.3f, Chubby, 0), S(2.5f, Walker, 3), S(5.5f, Tall, 5), S(8f, Walker, 2))
            ).Give(Heavy));

            s.Add(P("墨球", G(".XXXXX"),
                W(S(0.3f, Walker, -1, 2), S(4f, Chubby, 3), S(7.5f, Tall, 5)),
                W(S(0.3f, Ball, 2), S(3f, Ball, 4), S(6f, Ball, 3)),
                W(S(0.3f, Chubby, 1), S(3f, Ball, -1, 2), S(6f, Walker, 4), S(8.5f, Tall, 2, 3))
            ));

            // 大头排成一串走同一列，穿一发打一串。
            s.Add(P("大头", G("XXXXX."),
                W(S(0.3f, Ball, -1, 2), S(4f, Chubby, 2), S(7.5f, Tall, 1)),
                W(S(0.3f, BigHead, 2), S(3f, BigHead, 2), S(6f, BigHead, 2)),
                W(S(0.3f, Walker, -1, 2), S(3f, BigHead, 1), S(5.5f, Ball, 3), S(8.5f, Tall, 4, 2))
            ).Give(Pierce));

            // 第一排第一次铺满。
            s.Add(P("一行", G("XXXXXX"),
                W(S(0.3f, Walker, -1, 3), S(3.5f, Tall, 0), S(7f, Tall, 5)),
                W(S(0.3f, Chubby, 2), S(2f, Chubby, 3), S(5f, Ball, -1, 2), S(8f, BigHead, 1)),
                W(S(0.3f, Ball, -1, 2), S(3f, Tall, 4), S(6f, Walker, -1, 2), S(8.5f, BigHead, 3, 3))
            ));

            // 第二排第一次露头。火和冰叠在同一列，霜火就亮了。
            s.Add(P("叠墨", G(".XXXX.", "..XX.."),
                W(S(0.3f, Chubby, 2), S(3f, Walker, -1, 2), S(7f, Tall, 3)),
                W(S(0.3f, BigHead, 2), S(3.5f, BigHead, 3), S(7f, Ball, -1, 2)),
                W(S(0.3f, Tall, 1), S(3f, Chubby, 4), S(6f, Walker, -1, 2)),
                W(S(0.3f, Ball, -1, 2), S(4f, BigHead, 3), S(8f, Chubby, 2, 2))
            ));

            s.Add(P("鼓面", G("XXXXXX", "..X..."),
                W(S(0.3f, Walker, -1, 3), S(3.5f, Tall, -1, 2), S(7f, Ball, -1, 2)),
                W(S(0.3f, Chubby, 2), S(1.5f, Chubby, 3), S(5f, BigHead, -1, 2), S(8f, Walker, -1, 2)),
                W(S(0.3f, Ball, -1, 3), S(3f, Tall, 0), S(3.6f, Tall, 5), S(7f, Chubby, -1, 2)),
                Boss(BossDrum, S(4f, Tall, 1), S(7f, Tall, 4), S(11f, Chubby, 2), S(15f, BigHead, 3, 2))
            ));
        }
    }
}

using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十五章 · 魔城：新词「横扫」（横 + 扫）、新字「财」、新兵「鼓吏」、章底「判官」。
    // 鼓吏擂鼓催动整排铁骑齐头并进，「横扫」一刀跨列劈开整排阵线；配「重」更化作三字诀「断岳横江」！
    // 判官落朱砂劫印于阵前，滑炮击碎劫印即可反噬判官重创并爆出满地金币。
    public static partial class StageCatalog
    {
        static void Chapter15(List<Plan> s)
        {
            // 15-1 城门：给「横」「扫」，鼓吏初登场催动横列冲锋，一刀横扫整排清场。
            s.Add(P("城门", G("XXXXXX", "XXXXXX", "XX..XX"),
                W(S(0.3f, Drummer, 2), S(0.9f, Swarm, -1, 6), S(4.5f, Strafer, -1, 4), S(9f, Sprinter, -1, 4)),
                W(S(0.3f, Drummer, 1), S(1.2f, Drummer, 4), S(5.2f, Shield, -1, 5), S(9.2f, Runner, -1, 5)),
                W(S(0.3f, Bulwark, 2), S(1.2f, Bulwark, 3), S(5.5f, Drummer, 2), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Drummer, 3), S(8f, Strafer, -1, 5), S(10.5f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.4f, Warden, 4), S(5.2f, Drummer, -1, 2), S(9.2f, Shade, -1, 5)),
                W(S(0.3f, Drummer, -1, 2), S(3.8f, Elite, -1, 3), S(8f, Swarm, -1, 6), S(10.5f, Runner, -1, 5))
            ).Give(Heng, Sao).Inkwell(2, 1).Crystal(2, 2));

            // 15-2 角楼：古碑预刻二星「横」，补「扫」与「重」即可亮出断岳横江。
            s.Add(P("角楼", G("XXXXXX", "XX..XX", "XXXXXX"),
                W(S(0.3f, Strafer, -1, 5), S(3.8f, Drummer, 2), S(8.2f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, 1), S(1.1f, Bulwark, 4), S(4.8f, Drummer, 2), S(9f, Sprinter, -1, 5)),
                W(S(0.3f, Monk, 2), S(1.2f, Drummer, 3), S(5.2f, Splitter, -1, 4), S(9.2f, Shade, -1, 5)),
                W(S(0.3f, Warden, 2), S(1.5f, Elite, -1, 3), S(5.8f, Drummer, -1, 2), S(9.5f, Runner, -1, 6)),
                W(S(0.3f, Drummer, 1), S(0.9f, Drummer, 4), S(4.8f, Bulwark, -1, 2), S(9f, Swarm, -1, 6)),
                W(S(0.3f, Warden, -1, 2), S(4f, Drummer, -1, 2), S(8.5f, Elite, -1, 3))
            ).Stele(1, 1, Heng, 2).Crystal(2, 1));

            // 15-3 库房：给「财」，击倒成群杂兵迸出弹跳金元宝，边震晕边发财。
            s.Add(P("库房", G(".XXXX.", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Sprinter, -1, 5), S(3.5f, Runner, -1, 5), S(7.5f, Mender, 2)),
                W(S(0.3f, Shade, -1, 5), S(3.8f, Sprinter, -1, 5), S(8f, Drummer, 2)),
                W(S(0.3f, Mender, 2), S(1.2f, Monk, 3), S(4.8f, Runner, -1, 6), S(8.8f, Swarm, -1, 6)),
                W(S(0.3f, Drummer, 1), S(0.9f, Drummer, 4), S(4.8f, Shade, -1, 6), S(9f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.4f, Monk, 4), S(5.2f, Drummer, -1, 2), S(9.2f, Splitter, -1, 4)),
                W(S(0.3f, Bulwark, 2), S(1.2f, Bulwark, 3), S(5f, Warden, 1), S(8.5f, Warden, 4))
            ).Give(Wealth).Inkwell(3, 1).Crystal(0, 0));

            // 15-4 校场：古碑预刻二星「返」，配「分 + 瞄」试剑万剑归宗。
            s.Add(P("校场", G("X.XXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Shade, -1, 5), S(3.8f, Drummer, 2), S(8.2f, Runner, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.2f, Warden, 4), S(5f, Swarm, -1, 6), S(9f, Strafer, -1, 5)),
                W(S(0.3f, Monk, 2), S(0.9f, Drummer, 3), S(4.8f, Bulwark, -1, 2), S(9.2f, Sprinter, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.4f, Elite, 5), S(5.2f, Splitter, -1, 4), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Drummer, 1), S(0.9f, Monk, 4), S(5f, Warden, 3), S(9.2f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Drummer, -1, 2), S(8.5f, Elite, -1, 3))
            ).Stele(2, 1, Rebound, 2).Crystal(4, 2));

            // 15-5 影殿：中段重打影姬，鼓吏擂鼓护送幻影突进。
            s.Add(P("影殿", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Shade, -1, 6), S(3.5f, Drummer, 2), S(7.5f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(4.2f, Monk, 3), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Drummer, -1, 2), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Splitter, -1, 4), S(9.5f, Runner, -1, 6)),
                Boss(BossShade, S(4.5f, Drummer, 1), S(9.5f, Monk, 4), S(14f, Warden, 2), S(19f, Shade, -1, 5), S(24f, Drummer, 3))
            ).Inkwell(2, 0).Inkwell(3, 0));

            // 15-6 鼓楼：双鼓吏左右擂鼓，双灵砚居中爆发。
            s.Add(P("鼓楼", G("XXXXXX", ".XXXX.", "XXXXXX"),
                W(S(0.3f, Drummer, 0), S(0.9f, Drummer, 5), S(4.5f, Swarm, -1, 6), S(8.8f, Shield, -1, 5)),
                W(S(0.3f, Splitter, -1, 4), S(3.8f, Bulwark, 2), S(8.2f, Runner, -1, 6)),
                W(S(0.3f, Monk, 1), S(1.2f, Drummer, 4), S(5f, Shade, -1, 6), S(9.2f, Strafer, -1, 5)),
                W(S(0.3f, Warden, 2), S(1.4f, Warden, 3), S(5.2f, Mender, 2), S(9.2f, Sprinter, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.2f, Elite, 4), S(5.2f, Drummer, -1, 2), S(9.2f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Drummer, -1, 2), S(10.5f, Elite, -1, 3))
            ).Crystal(0, 1).Crystal(5, 1).Inkwell(2, 1).Inkwell(3, 1));

            // 15-7 箭道：七波攻城长阵，古碑二星「财」滚雪球攒金升满三星。
            s.Add(P("箭道", G("XXXX.X", "XXXXXX", "XX.XXX"),
                W(S(0.3f, Drummer, 2), S(0.9f, Swarm, -1, 6), S(4.5f, Strafer, -1, 5), S(8.5f, Sprinter, -1, 4)),
                W(S(0.3f, Bulwark, 1), S(1.4f, Splitter, -1, 4), S(5.8f, Runner, -1, 6)),
                W(S(0.3f, Monk, 2), S(0.9f, Drummer, 3), S(4.8f, Shade, -1, 6), S(9.2f, Warden, 4)),
                W(S(0.3f, Elite, 1), S(1.2f, Elite, 4), S(5.2f, Bulwark, -1, 2), S(9.2f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(1.4f, Warden, 5), S(5.2f, Drummer, -1, 2), S(9.2f, Shade, -1, 5)),
                W(S(0.3f, Mender, 2), S(1.2f, Monk, 3), S(5f, Splitter, -1, 5), S(9f, Sprinter, -1, 5)),
                W(S(0.3f, Drummer, -1, 2), S(3.8f, Warden, -1, 2), S(8f, Elite, -1, 3), S(10.5f, Shade, -1, 5))
            ).Stele(2, 0, Wealth, 2).Crystal(4, 0));

            // 15-8 丹墀：决战前夜，鼓吏、墨僧、镇守混编压城。
            s.Add(P("丹墀", G("XX.XXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Drummer, 1), S(0.9f, Monk, 4), S(4.5f, Swarm, -1, 6), S(10f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Drummer, -1, 2), S(10.5f, Shade, -1, 6)),
                W(S(0.3f, Bulwark, 2), S(1.1f, Mender, 3), S(5.5f, Strafer, -1, 5), S(10.5f, Runner, -1, 6)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Drummer, 2), S(6.2f, Monk, 3), S(10.5f, Sprinter, -1, 5)),
                W(S(0.3f, Shade, -1, 6), S(4.2f, Bulwark, -1, 2), S(9.5f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 4), S(4.2f, Drummer, -1, 2), S(8.8f, Elite, -1, 3)),
                W(S(0.3f, Warden, 2), S(0.9f, Drummer, 3), S(4f, Monk, -1, 2), S(7.5f, Bulwark, -1, 2))
            ).Crystal(2, 0).Inkwell(1, 1).Inkwell(4, 1));

            // 15-9 魔城：章底判官携影姬坐镇！滑炮击碎朱砂劫印反噬判官、满地爆金。
            s.Add(P("魔城", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Drummer, -1, 2), S(3.5f, Bulwark, -1, 2), S(7f, Swarm, -1, 6), S(11f, Shield, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(3.8f, Strafer, -1, 5), S(10.5f, Shade, -1, 6)),
                W(S(0.3f, Splitter, -1, 5), S(4.2f, Drummer, -1, 2), S(10.5f, Runner, -1, 6)),
                W(S(0.3f, Shade, -1, 6), S(4.5f, Mender, -1, 2), S(11f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Drummer, 1), S(7.4f, Monk, 4), S(12f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4.5f, Shade, -1, 6), S(10.5f, Splitter, -1, 4)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Drummer, 2), S(8.5f, Warden, 3), S(12.5f, Sprinter, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Drummer, -1, 2), S(8.5f, Monk, -1, 2), S(12f, Runner, -1, 6)),
                Boss(BossJudge, S(2f, Drummer, -1, 2), S(10f, BossShade, 1), S(20f, Monk, 4), S(30f, Shade, -1, 3))
            ).Inkwell(2, 1).Inkwell(3, 1).Stele(0, 1, Sao, 2));
        }
    }
}

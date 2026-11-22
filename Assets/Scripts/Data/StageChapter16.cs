using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十六章 · 墨渊（第二幕终章）：新字「护」、新兵「污墨童」、终章首领「魔尊」。
    // 污墨童亡语溅浮墨短蒙一格（连开三炮即洗净）；「护」在底线立起金盾弹回快攻漏怪。
    // 16-9 魔尊半血开启渊墨护壁削弱散弹，任一成词或秘卷大招命中即轰碎渊壁、打出 1.5 倍易伤大硬直！
    public static partial class StageCatalog
    {
        static void Chapter16(List<Plan> s)
        {
            // 16-1 渊桥：给「护」，污墨童伴快脚疾冲底线，金盾稳稳弹回并震晕。
            s.Add(P("渊桥", G("XXXXXX", "XXXXXX", ".XXXX."),
                W(S(0.3f, Imp, -1, 4), S(3.5f, Runner, -1, 5), S(7.8f, Swarm, -1, 6)),
                W(S(0.3f, Sprinter, -1, 4), S(3.8f, Imp, -1, 4), S(8.2f, Drummer, 2)),
                W(S(0.3f, Warden, 1), S(1.4f, Warden, 4), S(5.2f, Imp, -1, 4), S(9.2f, Strafer, -1, 5)),
                W(S(0.3f, Monk, 2), S(1.2f, Bulwark, 3), S(5.2f, Shade, -1, 5), S(9.5f, Imp, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.4f, Elite, 4), S(5.2f, Drummer, -1, 2), S(9.2f, Runner, -1, 6)),
                W(S(0.3f, Imp, -1, 5), S(3.8f, Warden, -1, 2), S(8.2f, Elite, -1, 3))
            ).Give(Guard).Inkwell(2, 0).Crystal(0, 2));

            // 16-2 星坛：古碑预刻二星「封」，封住污墨童亡语与墨僧护盾。
            s.Add(P("星坛", G("XX.XXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Imp, -1, 4), S(3.6f, Monk, 2), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, 1), S(1.2f, Bulwark, 4), S(5f, Imp, -1, 4), S(9f, Sprinter, -1, 5)),
                W(S(0.3f, Drummer, 2), S(1.2f, Monk, 3), S(5.2f, Splitter, -1, 4), S(9.2f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.4f, Warden, 4), S(5.2f, Imp, -1, 5), S(9.2f, Runner, -1, 6)),
                W(S(0.3f, Elite, 2), S(1.2f, Elite, 3), S(5.2f, Mender, -1, 2), S(9.2f, Imp, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(4f, Drummer, -1, 2), S(8.5f, Warden, -1, 2))
            ).Stele(2, 1, Seal, 2).Crystal(2, 0));

            // 16-3 浮碑：古碑预刻二星「千」，配「钧」重锤砸穿重甲阵列。
            s.Add(P("浮碑", G("X.XXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Bulwark, 2), S(0.9f, Bulwark, 3), S(4.5f, Imp, -1, 4), S(8.8f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.4f, Warden, 4), S(5.2f, Drummer, 2), S(9.2f, Shade, -1, 5)),
                W(S(0.3f, Monk, 1), S(1.2f, Mender, 4), S(5.2f, Elite, -1, 3), S(9.2f, Imp, -1, 4)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Bulwark, -1, 3), S(8.5f, Runner, -1, 6)),
                W(S(0.3f, Drummer, 2), S(0.9f, Monk, 3), S(5f, Warden, -1, 2), S(9.2f, Sprinter, -1, 5)),
                W(S(0.3f, Bulwark, -1, 3), S(4.2f, Imp, -1, 5), S(8.5f, Elite, -1, 3))
            ).Stele(3, 1, Qian, 2).Inkwell(2, 1));

            // 16-4 逆流：惊风、快脚与污墨童三路抢滩，「护」守底线、「横扫」清场。
            s.Add(P("逆流", G("XXX..X", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Sprinter, -1, 5), S(3.5f, Imp, -1, 4), S(7.8f, Runner, -1, 6)),
                W(S(0.3f, Drummer, 1), S(0.9f, Drummer, 4), S(4.8f, Shade, -1, 6), S(9f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 2), S(1.4f, Monk, 3), S(5.2f, Imp, -1, 5), S(9.2f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.4f, Elite, 5), S(5.2f, Splitter, -1, 4), S(9.2f, Bulwark, -1, 2)),
                W(S(0.3f, Imp, 0, 3), S(0.9f, Imp, 5, 3), S(4.8f, Drummer, -1, 2), S(9f, Warden, -1, 2)),
                W(S(0.3f, Monk, -1, 2), S(4f, Elite, -1, 3), S(8.5f, Sprinter, -1, 6))
            ).Stele(1, 0, Guard, 2).Crystal(3, 0).Crystal(4, 0));

            // 16-5 判殿：中段重打判官，击碎朱砂劫印快速削血。
            s.Add(P("判殿", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Imp, -1, 4), S(3.5f, Drummer, 2), S(7.5f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(4.2f, Monk, 3), S(8.5f, Shade, -1, 6)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5.5f, Imp, -1, 4), S(9.5f, Runner, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Drummer, -1, 2), S(9.5f, Sprinter, -1, 5)),
                Boss(BossJudge, S(4.8f, Drummer, 1), S(9.8f, Monk, 4), S(14.5f, Imp, -1, 4), S(19.5f, Warden, 2), S(24f, Shade, -1, 5))
            ).Inkwell(2, 1).Inkwell(3, 1));

            // 16-6 碎镜：双墨晶宝矿配双灵砚，破晶爆金速成四星极意。
            s.Add(P("碎镜", G("XXXXXX", ".XXXX.", "XXXXXX"),
                W(S(0.3f, Shade, -1, 6), S(3.6f, Imp, -1, 4), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, 1), S(1.2f, Bulwark, 4), S(5f, Drummer, 2), S(9f, Strafer, -1, 5)),
                W(S(0.3f, Monk, 2), S(1.2f, Mender, 3), S(5.2f, Imp, -1, 5), S(9.2f, Runner, -1, 6)),
                W(S(0.3f, Warden, 0), S(1.4f, Warden, 5), S(5.2f, Splitter, -1, 4), S(9.2f, Shade, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.4f, Elite, 4), S(5.2f, Drummer, -1, 2), S(9.2f, Imp, -1, 4)),
                W(S(0.3f, Warden, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 3))
            ).Crystal(0, 1).Crystal(5, 1).Inkwell(2, 1).Inkwell(3, 1));

            // 16-7 天阶：七波登阶，古碑预刻二星「横」，挥刀横扫千军。
            s.Add(P("天阶", G("XXXX.X", "XXXXXX", "X.XXXX"),
                W(S(0.3f, Drummer, 2), S(0.9f, Imp, -1, 4), S(4.5f, Swarm, -1, 6), S(8.5f, Bulwark, -1, 2)),
                W(S(0.3f, Warden, 1), S(1.5f, Splitter, -1, 4), S(6f, Runner, -1, 6)),
                W(S(0.3f, Elite, 1), S(1.2f, Elite, 4), S(4.8f, Shade, -1, 6), S(9.2f, Monk, 2)),
                W(S(0.3f, Bulwark, 2), S(1.2f, Bulwark, 3), S(5.2f, Imp, -1, 5), S(9.2f, Warden, 4)),
                W(S(0.3f, Drummer, -1, 2), S(3.8f, Elite, 0), S(4.4f, Elite, 5), S(9.2f, Sprinter, -1, 5)),
                W(S(0.3f, Mender, -1, 2), S(4.2f, Imp, -1, 5), S(8.5f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(9.5f, Shade, -1, 5))
            ).Stele(2, 1, Heng, 2).Crystal(4, 0));

            // 16-8 九重：渊顶门前总检阅，鼓吏、墨僧、污墨童、镇守轮番压境。
            s.Add(P("九重", G("XX.XXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Drummer, 1), S(0.9f, Monk, 4), S(4.5f, Imp, -1, 5), S(10f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Drummer, -1, 2), S(10.5f, Shade, -1, 6)),
                W(S(0.3f, Bulwark, 2), S(1.2f, Mender, 3), S(5.5f, Imp, -1, 5), S(10.5f, Runner, -1, 6)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Monk, 2), S(6.2f, Drummer, 3), S(10.5f, Splitter, -1, 4)),
                W(S(0.3f, Shade, -1, 6), S(4.2f, Imp, -1, 5), S(9.5f, Warden, -1, 2)),
                W(S(0.3f, Bulwark, -1, 3), S(4.2f, Drummer, -1, 2), S(8.8f, Elite, -1, 3)),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4f, Imp, -1, 4), S(7.5f, Bulwark, -1, 2))
            ).Crystal(2, 0).Inkwell(1, 1).Inkwell(4, 1));

            // 16-9 墨渊（第二幕压轴终章）：魔尊携判官临阵！双灵砚助阵四星极意，成词与秘卷轰碎渊墨护壁！
            s.Add(P("墨渊", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Imp, -1, 5), S(3.5f, Sprinter, -1, 5), S(7.2f, Mender, -1, 2), S(11f, Runner, -1, 6)),
                W(S(0.3f, Shade, -1, 6), S(3.8f, Imp, -1, 5), S(10.5f, Mender, -1, 2)),
                W(S(0.3f, Drummer, -1, 2), S(4f, Sprinter, -1, 6), S(10.5f, Imp, -1, 5)),
                W(S(0.3f, Monk, -1, 2), S(4.2f, Shade, -1, 6), S(11f, Runner, -1, 6)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Monk, 1), S(7.4f, Mender, 4), S(12f, Imp, -1, 5)),
                W(S(0.3f, Sprinter, -1, 5), S(4.5f, Mender, -1, 2), S(10.5f, Shade, -1, 6)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Drummer, 2), S(8.5f, Mender, 3), S(12.5f, Imp, -1, 5)),
                W(S(0.3f, Bulwark, 1), S(1.5f, Bulwark, 4), S(5.5f, Warden, 2), S(12f, Warden, 3)),
                Boss(BossAbyss, S(1.5f, Bulwark, -1, 2), S(8f, BossJudge, 1), S(16f, Warden, 4), S(24f, Drummer, -1, 2))
            ).Inkwell(2, 1).Inkwell(3, 1).Stele(0, 0, Guard, 2).Stele(5, 0, Seal, 2));
        }
    }
}

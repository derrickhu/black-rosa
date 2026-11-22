using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十四章 · 冥河：新字「封」。与云海的轻灵游击彻底拉开——冥河重装方阵由墨僧、补墨、分裂、重盾、狂化护卫携鬼卒潮结阵推进！
    // 「封」禁绝挂盾、回血、分裂与隐身；「冰+土+聚」化作定海针把整团重兵钉在河心！
    // 中段重打钟魔，章底影姬携钟魔双首领压境。
    public static partial class StageCatalog
    {
        static void Chapter14(List<Plan> s)
        {
            // 封：打中之后，挂盾、回血、分裂、隐身、狂化一并封印。
            s.Add(P("冥口", G(".XXXX.", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, 2), S(0.9f, Swarm, -1, 6), S(4.8f, Bulwark, -1, 2), S(9f, Splitter, -1, 4)),
                W(S(0.3f, Mender, 2), S(1.4f, Elite, 1), S(1.9f, Elite, 4), S(6f, Shade, -1, 5), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.2f, Monk, -1, 2), S(9.2f, Swarm, -1, 6)),
                W(S(0.3f, Splitter, -1, 4), S(3.8f, Mender, 3), S(7.5f, Shade, -1, 5), S(10f, Runner, -1, 5)),
                W(S(0.3f, Bulwark, 2), S(0.9f, Monk, 2), S(5f, Warden, 3), S(9f, Swarm, -1, 6)),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Mender, 2), S(7.5f, Elite, -1, 3), S(10f, Shade, -1, 5))
            ).Give(Seal).Inkwell(2, 1).Crystal(0, 0));

            // 成团重兵压河心。古碑预刻二星「聚」，配冰与土即可铸出定海针。
            s.Add(P("急流", G("XX.XXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Bulwark, 2), S(0.9f, Swarm, -1, 6), S(4.5f, Mender, 2), S(8.5f, Runner, -1, 6)),
                W(S(0.3f, Splitter, -1, 4), S(3.5f, Monk, 2), S(4.1f, Elite, 3), S(8.5f, Shade, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.2f, Warden, 4), S(5f, Mender, 3), S(9f, Shield, -1, 5)),
                W(S(0.3f, Elite, 2), S(1.2f, Elite, 3), S(5f, Splitter, -1, 4), S(9f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 1), S(0.9f, Monk, 4), S(4.8f, Bulwark, -1, 2), S(9f, Shade, -1, 6)),
                W(S(0.3f, Warden, -1, 2), S(3.8f, Splitter, -1, 4), S(8f, Runner, -1, 6))
            ).Stele(2, 1, Gather, 2).Crystal(2, 0));

            // 双补墨护航重盾与分裂怪：先封住补墨，再一网打尽。
            s.Add(P("沉锚", G("X.XXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Mender, 2), S(0.9f, Bulwark, 2), S(4.2f, Swarm, -1, 6), S(8.5f, Splitter, -1, 4)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(4.5f, Shade, -1, 6), S(8.8f, Warden, 3)),
                W(S(0.3f, Bulwark, 1), S(0.9f, Mender, 2), S(1.5f, Mender, 3), S(5.5f, Elite, -1, 3), S(9f, Runner, -1, 5)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Warden, -1, 2), S(8f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 1), S(0.9f, Monk, 1), S(4.5f, Warden, 4), S(5.1f, Mender, 4), S(9.5f, Shade, -1, 6)),
                W(S(0.3f, Bulwark, 2), S(0.9f, Bulwark, 3), S(4.8f, Mender, -1, 2), S(9f, Elite, -1, 3))
            ).Stele(3, 1, Ice, 2).Inkwell(2, 2));

            // 冥灯鬼影：成群墨影潜行混着狂化护卫，失魂（乱+毒）与封最能破阵。
            s.Add(P("鬼灯", G("XXX..X", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Shade, -1, 6), S(3.5f, Monk, -1, 2), S(7.2f, Swarm, -1, 6)),
                W(S(0.3f, Mender, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 6), S(9f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.4f, Warden, 4), S(5f, Shade, -1, 5), S(9f, Runner, -1, 6)),
                W(S(0.3f, Splitter, -1, 4), S(3.8f, Mender, 2), S(8f, Bulwark, -1, 3)),
                W(S(0.3f, Shade, 0, 4), S(0.9f, Shade, 5, 4), S(4.8f, Monk, -1, 2), S(9f, Elite, -1, 3)),
                W(S(0.3f, Warden, -1, 2), S(4f, Mender, -1, 2), S(8.5f, Shade, -1, 6))
            ).Stele(1, 1, Confuse, 2).Crystal(3, 0).Crystal(4, 0));

            // 中段重打钟魔：补墨与墨僧双护法拱卫钟魔，封字显奇效。
            s.Add(P("再钟", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.2f, Mender, 2), S(6.5f, Shade, -1, 6), S(9.5f, Swarm, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Splitter, -1, 4), S(8f, Runner, -1, 6)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Mender, -1, 2), S(9.5f, Shade, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(9.5f, Swarm, -1, 6)),
                Boss(BossBell, S(5f, Mender, 2), S(10f, Monk, 1), S(14f, Warden, 4), S(19f, Shade, -1, 5), S(24f, Monk, 3))
            ).Inkwell(2, 0).Inkwell(3, 0));

            s.Add(P("失路", G("XXXXXX", ".XXXX.", "XXXXXX"),
                W(S(0.3f, Splitter, -1, 4), S(3.2f, Swarm, -1, 6), S(7.5f, Warden, 3)),
                W(S(0.3f, Mender, 2), S(1.2f, Bulwark, 2), S(4.8f, Monk, 3), S(9f, Shade, -1, 6)),
                W(S(0.3f, Shade, -1, 6), S(3.5f, Bulwark, 1), S(4.1f, Bulwark, 4), S(8.5f, Runner, -1, 6)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Mender, -1, 2), S(9f, Shade, -1, 5)),
                W(S(0.3f, Warden, 0), S(1.2f, Warden, 5), S(5f, Monk, -1, 2), S(9f, Splitter, -1, 4)),
                W(S(0.3f, Mender, 2), S(0.9f, Monk, 3), S(4.8f, Bulwark, -1, 2), S(9f, Elite, -1, 3))
            ).Crystal(0, 1).Crystal(5, 1).Inkwell(2, 1).Inkwell(3, 1));

            s.Add(P("渡口", G("XXXX.X", "XXXXXX", "X.XXXX"),
                W(S(0.3f, Monk, 2), S(0.9f, Mender, 3), S(4.2f, Swarm, -1, 6), S(8f, Bulwark, -1, 2)),
                W(S(0.3f, Warden, 1), S(1.5f, Splitter, -1, 4), S(6f, Runner, -1, 6)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Shade, -1, 6), S(9f, Mender, 2)),
                W(S(0.3f, Bulwark, 2), S(1.2f, Bulwark, 3), S(5f, Splitter, -1, 4), S(9f, Warden, 4)),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 0), S(4.1f, Elite, 5), S(9f, Shade, -1, 6)),
                W(S(0.3f, Mender, -1, 2), S(4f, Swarm, -1, 6), S(8.5f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Bulwark, -1, 2), S(9.5f, Shade, -1, 5))
            ).Stele(2, 0, Seal, 2).Crystal(4, 0));

            // 墨僧与补墨双排互保。封住施法者，重阵不攻自破。
            s.Add(P("夜航", G("XX.XXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, 1), S(0.9f, Mender, 4), S(4.5f, Swarm, -1, 6), S(10.5f, Splitter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(11f, Shade, -1, 6)),
                W(S(0.3f, Bulwark, 2), S(0.9f, Mender, 2), S(5.5f, Shade, -1, 6), S(10.5f, Runner, -1, 6)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Monk, 2), S(6.1f, Mender, 3), S(10.5f, Splitter, -1, 4)),
                W(S(0.3f, Shade, -1, 6), S(4f, Monk, -1, 2), S(9.5f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 4), S(4f, Mender, -1, 2), S(8.5f, Elite, -1, 3)),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(3.5f, Shade, -1, 5), S(6.5f, Bulwark, -1, 2))
            ).Crystal(2, 0).Inkwell(1, 1).Inkwell(4, 1));

            // 章底：影姬分幻影，钟魔敲金盾，双灵砚+古碑二星封镇守中宫。
            s.Add(P("冥河", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Bulwark, -1, 2), S(7f, Swarm, -1, 6), S(11f, Shield, -1, 4)),
                W(S(0.3f, Mender, -1, 2), S(3.5f, Bulwark, -1, 3), S(10.5f, Shade, -1, 6)),
                W(S(0.3f, Splitter, -1, 5), S(4f, Monk, -1, 2), S(10.5f, Runner, -1, 6)),
                W(S(0.3f, Shade, -1, 6), S(4.5f, Mender, -1, 2), S(11f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Monk, 1), S(7.4f, Mender, 4), S(12f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4.5f, Shade, -1, 6), S(10.5f, Splitter, -1, 4)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Monk, 2), S(8.5f, Warden, 3), S(12.5f, Shade, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Mender, -1, 2), S(12f, Runner, -1, 6)),
                Boss(BossShade, S(1.2f, Elite, -1, 3), S(7f, BossBell, 1), S(11f, Monk, 4), S(15f, Shade, -1, 2))
            ).Inkwell(2, 1).Inkwell(3, 1).Stele(0, 1, Seal, 2));
        }
    }
}

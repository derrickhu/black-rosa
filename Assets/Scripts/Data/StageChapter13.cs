using System.Collections.Generic;
using static InkLine.EnemyId;
using static InkLine.CardId;

namespace InkLine
{
    // 第十三章 · 云海：新字「影」。主打横掠、疾步、爬子与云隙灵砚，盘面留整列供「风火轮」「穿云箭」大展拳脚。
    // 中段打影姬，章底钟魔带墨王。
    public static partial class StageCatalog
    {
        static void Chapter13(List<Plan> s)
        {
            // 影：这一列每 4 发，再补一发淡影弹。灵砚位直接让新字体验高一星威力。
            s.Add(P("云门", G("XXXXXX", "XXXXXX", "XX..XX"),
                W(S(0.3f, Strafer, -1, 4), S(1.2f, Runner, -1, 5), S(5.2f, Elite, 3), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Monk, 4), S(5f, Sprinter, -1, 3), S(9f, Shade, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Strafer, -1, 4), S(9.5f, Runner, -1, 5)),
                W(S(0.3f, Swarm, -1, 5), S(3.5f, Monk, 3), S(4.1f, Sprinter, -1, 4), S(8.5f, Shade, -1, 4)),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Crawler, -1, 3)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Runner, -1, 5))
            ).Give(Shadow).Inkwell(1, 1).Inkwell(4, 1));

            // 横掠换列，古碑预刻二星「风」，凑上火与弹就是风火轮。
            s.Add(P("风口", G("XXXXXX", "XXXXXX", "X.XX.X"),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Ball, -1, 5), S(7.5f, Monk, 2)),
                W(S(0.3f, Runner, -1, 5), S(3f, Monk, 3), S(3.6f, Elite, 3), S(8f, Shade, -1, 5)),
                W(S(0.3f, Crawler, -1, 4), S(4f, Belt, -1, 5), S(8.5f, Strafer, -1, 4)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Swarm, -1, 6), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(4.5f, Runner, -1, 5), S(9f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(1.0f, Warden, -1, 2), S(4.5f, Sprinter, -1, 3), S(7.5f, Strafer, -1, 4))
            ).Stele(2, 1, Wind, 2).Crystal(1, 2));

            // 怪排成一串。古碑预刻二星「穿」，配万箭就是穿云箭一路贯通。
            s.Add(P("穿云", G("XXXXXX", "XXXXXX", ".XXXX."),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Elite, -1, 2), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Shade, -1, 5), S(8f, Monk, 2)),
                W(S(0.3f, Elite, 0), S(0.9f, Elite, 2), S(1.5f, Elite, 3), S(2.1f, Elite, 5), S(6.5f, Monk, -1, 2)),
                W(S(0.3f, Sprinter, -1, 5), S(4f, Warden, -1, 2), S(8.5f, Crawler, -1, 4)),
                W(S(0.3f, Monk, 1), S(0.9f, Bulwark, 1), S(4.5f, Monk, 4), S(5.1f, Bulwark, 4), S(9.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 2), S(0.9f, Warden, 3), S(4.5f, Elite, -1, 2), S(9f, Crawler, -1, 3))
            ).Stele(3, 1, Pierce, 2).Inkwell(2, 1));

            s.Add(P("叠浪", G("XXXXXX", "XX..XX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(2.8f, Strafer, -1, 5), S(6.5f, Runner, -1, 4), S(9f, Elite, 3)),
                W(S(0.3f, Mender, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Swarm, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Monk, -1, 2), S(9.5f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 0), S(1.5f, Warden, 5), S(5.5f, Splitter, -1, 3), S(10f, Monk, -1, 2)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, -1, 2), S(8f, Belt, -1, 4)),
                W(S(0.3f, Elite, -1, 2), S(4f, Strafer, -1, 4), S(8.5f, Runner, -1, 5))
            ).Crystal(2, 1).Crystal(3, 1).Inkwell(1, 0).Inkwell(4, 0));

            // 中段打影姬。双灵砚夹在中路，方便轰真身清分身。
            s.Add(P("影姬", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Strafer, -1, 4), S(3.5f, Elite, 2), S(7f, Shade, -1, 5)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8f, Runner, -1, 5)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5.5f, Shade, -1, 5), S(10f, Sprinter, -1, 4)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Monk, -1, 2), S(10f, Swarm, -1, 5)),
                Boss(BossShade, S(5f, Monk, 2), S(10f, Elite, 1), S(14f, Warden, 4), S(19f, Shade, -1, 5), S(24f, Monk, 3))
            ).Inkwell(2, 1).Inkwell(3, 1));

            s.Add(P("回风", G("XXXXXX", "XXXXXX", "XXX.XX"),
                W(S(0.3f, Strafer, -1, 5), S(3.5f, Monk, -1, 2), S(8f, Elite, 3)),
                W(S(0.3f, Sprinter, -1, 4), S(3.5f, Warden, 2), S(4.1f, Monk, 3), S(8.5f, Swarm, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Bulwark, 1), S(4.1f, Bulwark, 4), S(8.5f, Runner, -1, 5)),
                W(S(0.3f, Elite, 1), S(1.5f, Elite, 4), S(5f, Strafer, -1, 4), S(9f, Shade, -1, 4)),
                W(S(0.3f, Monk, -1, 2), S(4f, Crawler, -1, 4), S(8.5f, Warden, -1, 2)),
                W(S(0.3f, Shade, 0, 3), S(1f, Shade, 5, 3), S(4.5f, Monk, -1, 2), S(9f, Elite, -1, 2))
            ).Stele(1, 1, Rebound, 2).Crystal(3, 2));

            s.Add(P("积云", G("XXXXXX", "X.XXXX", "XXXXXX"),
                W(S(0.3f, Strafer, -1, 4), S(1.2f, Swarm, -1, 5), S(5f, Elite, 3)),
                W(S(0.3f, Warden, 2), S(1.5f, Sprinter, -1, 4), S(6f, Monk, -1, 2)),
                W(S(0.3f, Elite, 1), S(0.9f, Elite, 4), S(4.5f, Shade, -1, 5), S(9f, Strafer, -1, 4)),
                W(S(0.3f, Runner, -1, 5), S(3.5f, Crawler, -1, 4), S(8f, Warden, 3)),
                W(S(0.3f, Monk, -1, 2), S(3.5f, Elite, 0), S(4.1f, Elite, 5), S(9.5f, Belt, -1, 4)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Monk, -1, 2), S(8.5f, Shade, -1, 5)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.5f, Elite, -1, 3), S(10f, Monk, -1, 2))
            ).Crystal(1, 1).Inkwell(3, 1));

            // 墨僧与疾步穿插。古碑预刻二星「影」，多段弹幕破盾追击。
            s.Add(P("断霞", G("XXXXXX", "XXXXXX", "XXXX.X"),
                W(S(0.3f, Monk, 1), S(0.9f, Monk, 4), S(3.5f, Elite, -1, 2), S(7.2f, Runner, -1, 6)),
                W(S(0.3f, Warden, 1), S(1.5f, Warden, 4), S(5.2f, Monk, -1, 2), S(9.2f, Swarm, -1, 6)),
                W(S(0.3f, Bulwark, -1, 2), S(4f, Shade, -1, 5), S(8.5f, Strafer, -1, 4)),
                W(S(0.3f, Elite, 0), S(1.5f, Elite, 5), S(5f, Monk, 2), S(5.6f, Monk, 3), S(9f, Sprinter, -1, 4)),
                W(S(0.3f, Shade, -1, 5), S(3.5f, Crawler, -1, 4), S(8f, Warden, -1, 2)),
                W(S(0.3f, Splitter, -1, 3), S(4f, Monk, -1, 2), S(8.5f, Elite, -1, 2)),
                W(S(0.3f, Warden, 2), S(0.9f, Monk, 3), S(4.5f, Shade, -1, 5), S(9f, Bulwark, -1, 2))
            ).Stele(2, 2, Shadow, 2).Crystal(4, 2).Inkwell(2, 0));

            // 章底：钟魔全场挂盾，墨王压阵。
            s.Add(P("云巅", G("XXXXXX", "XXXXXX", "XXXXXX"),
                W(S(0.3f, Monk, -1, 2), S(3.2f, Elite, -1, 3), S(6.5f, Runner, -1, 5), S(9.5f, Shield, -1, 3)),
                W(S(0.3f, Mender, -1, 2), S(3f, Bulwark, -1, 3), S(6.5f, Elite, -1, 2), S(9f, Shade, -1, 5)),
                W(S(0.3f, Ball, -1, 6), S(3.5f, Monk, -1, 2), S(8.5f, Sprinter, -1, 5)),
                W(S(0.3f, Shade, -1, 5), S(4f, Belt, -1, 5), S(9.5f, Swarm, -1, 6)),
                W(S(0.3f, Warden, 0), S(2f, Warden, 5), S(6.5f, Monk, 1), S(7.4f, Monk, 4), S(11.5f, Elite, 3)),
                W(S(0.3f, Bulwark, -1, 3), S(4f, Shade, -1, 5), S(9.5f, Strafer, -1, 5)),
                W(S(0.3f, Elite, 1), S(2f, Elite, 4), S(6.5f, Monk, 2), S(8.5f, Warden, 3), S(12.5f, Shade, -1, 5)),
                W(S(0.3f, Elite, -1, 3), S(4f, Sprinter, -1, 4), S(8.5f, Monk, -1, 2), S(12f, Runner, -1, 5)),
                Boss(BossBell, S(1.2f, Elite, -1, 3), S(8f, BossKing, 1), S(14f, Monk, 4), S(20f, Shade, -1, 2))
            ).Inkwell(1, 1).Inkwell(4, 1));
        }
    }
}

// 由 docs/prompt/runtime/cdn_build.py 生成，别手改。
using System.Collections.Generic;

namespace InkLine
{
    public static class CdnManifest
    {
        public const string BaseUrl = "https://726f-rosa-env-d7grf78r5dbd37323-1414200063.tcb.qcloud.la/black-rosa/StreamingAssets/";

        // 逻辑名 -> (云上相对路径, CdnArt 下的源文件)
        public static readonly Dictionary<string, (string url, string src)> Files =
            new Dictionary<string, (string, string)>
            {
                { "Audio/bgm_battle", ("Audio/bgm_battle_110e580f6542fd5eb9abc5d7320fc8fb.mp3", "Audio/bgm_battle.mp3") },
                { "Bg/battle_bg_1", ("Bg/battle_bg_1_184971bb69c3716cdd2ecd200a5fac00.jpg", "Bg/battle_bg_1.png") },
                { "Bg/battle_bg_2", ("Bg/battle_bg_2_e100de608c3b86b76a8f28686e8f9e80.jpg", "Bg/battle_bg_2.png") },
                { "Bg/battle_bg_3", ("Bg/battle_bg_3_a8bda397b8e0c6f198b886c1b462a54a.jpg", "Bg/battle_bg_3.png") },
                { "Bg/battle_bg_4", ("Bg/battle_bg_4_dae7ca5c1fcc006db91f250d72049b4d.jpg", "Bg/battle_bg_4.png") },
                { "Bg/battle_bg_5", ("Bg/battle_bg_5_622b052f41e75bbc9c8dc26d943e911e.jpg", "Bg/battle_bg_5.png") },
                { "Bg/battle_bg_6", ("Bg/battle_bg_6_a578ffc59bcc4c6b50aca3b0d48fc27d.jpg", "Bg/battle_bg_6.png") },
                { "Bg/battle_bg_7", ("Bg/battle_bg_7_6c0cb190bc7b8a9b08ec57aff855d305.jpg", "Bg/battle_bg_7.png") },
                { "Bg/battle_bg_8", ("Bg/battle_bg_8_b8a48b8a0f08e6faf79581586933af4d.jpg", "Bg/battle_bg_8.png") },
                { "Ui/chapter_1", ("Ui/chapter_1_adfd2e87979423feb14ed7ce2c08ff65.png", "Ui/chapter_1.png") },
                { "Ui/chapter_2", ("Ui/chapter_2_78aa3b9400f20e3fd798b6dd2b532b40.png", "Ui/chapter_2.png") },
                { "Ui/chapter_3", ("Ui/chapter_3_a5e31c71a5508125e8e26cc07aba8dc2.png", "Ui/chapter_3.png") },
                { "Ui/chapter_4", ("Ui/chapter_4_56baea2300f255b5f6f62db054ab2998.png", "Ui/chapter_4.png") },
                { "Ui/chapter_5", ("Ui/chapter_5_079322e7d2b4fa6982d2e17c26804c75.png", "Ui/chapter_5.png") },
                { "Ui/chapter_6", ("Ui/chapter_6_1720d0fee98e82cecb068c4005f5676f.png", "Ui/chapter_6.png") },
                { "Ui/chapter_7", ("Ui/chapter_7_fc264cb96a746d2b9e325fcb835aea4e.png", "Ui/chapter_7.png") },
                { "Ui/chapter_8", ("Ui/chapter_8_ba704b49e18d5b1a6b3f44684ac6021a.png", "Ui/chapter_8.png") },
            };
    }
}

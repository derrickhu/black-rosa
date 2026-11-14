using UnityEditor;
using UnityEngine;

public sealed class InkArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (path.IndexOf("/Resources/Art/", System.StringComparison.Ordinal) < 0) return;
        bool vfx = path.IndexOf("/Resources/Art/Vfx/", System.StringComparison.Ordinal) >= 0;
        // 界面图标只当贴图画，没人在 CPU 上读它。isReadable 会额外留一份内存副本，
        // 这批 16 张白留 1MB 没意义，所以和 Vfx 一样关掉。
        bool ui = path.IndexOf("/Resources/Art/Ui/", System.StringComparison.Ordinal) >= 0;
        // 战斗背景包里只留 256 缩略图，高清版走 CdnAssets，同样没人读像素。
        bool bg = path.IndexOf("/Resources/Art/Bg/", System.StringComparison.Ordinal) >= 0;
        bool cpu = !vfx && !ui && !bg;
        // 平涂特效（§4.0）是硬描边图，和界面图标一样经不起块压缩：
        // 深色外沿正是它在宣纸底上立得住的原因，崩出脏点就白画了。
        // 靠命名认：弹体渐变 `<元素>_shot_NN`、命中 `hitv_*`、状态层 `burn_body_/ice_crust_/dot_*`。
        // `_shot_` 里那两条下划线是有意的 —— 它要排除旧的柔光单图 `shot_ice.png`。
        bool flatVfx = vfx && (path.Contains("_shot_")
                               || path.Contains("/hitv_")
                               || path.Contains("/burn_body_")
                               || path.Contains("/ice_crust_")
                               || path.Contains("/dot_"));
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.spritePixelsPerUnit = 128;
        // 字图要在 CPU 上被 InkArt.OnCard 套宣纸卡框，必须可读且不压缩。
        // Vfx 只当贴图用，黑底已由 docs/prompt/runtime/vfx_crush.py 离线压掉，
        // 所以可以关掉可读、开压缩。
        // 界面图标是硬描边小图，块压缩会在描边上崩出脏点，所以不压缩、只关可读。
        importer.textureCompression = vfx && !flatVfx
            ? TextureImporterCompression.Compressed
            : TextureImporterCompression.Uncompressed;
        importer.isReadable = cpu;
        // 屏幕上最大的用法是 160px 的抽卡字面，256 已经是两倍超采样。
        // 章节图、战斗背景这类大图的高清版在 CdnArt/，这里只是先顶上的缩略图。
        bool panel = path.Contains("/panel_") || path.EndsWith("/tab_dock.png") || path.EndsWith("/tab_plaque.png");
        // 展台在炮台页上铺到 640 宽，256 会被拉糊。按两倍超采样留 2048。
        bool stage = path.EndsWith("/skin_stage.png");
        // 炮台页上半底铺满 720×660，按两倍宽出。
        bool terrace = path.EndsWith("/turret_terrace.png") || path.EndsWith("/turret_upper.png")
                       || path.EndsWith("/skin_stripe.png");
        bool skinHex = path.Contains("/skin_hex_") || path.EndsWith("/tag_exclusive.png") || path.EndsWith("/stat_pill.png");
        // 首页整屏底是 720×1280。默认 256 会把它压糊，1024 也会把长边缩掉。
        bool homeBg = path.EndsWith("/home_bg.png");
        // 首页其余零件按设计稿两倍留，草地最长边不到 1024。
        bool home = path.Contains("/Ui/home_");
        // 道具卡、方形物品框、名称条、尖角按钮按两倍导出。默认 256 会把描边压糊。
        bool itemKit = path.Contains("/Ui/item_") || path.Contains("/Ui/btn_hex") || path.Contains("/Ui/nameplate_");
        // 首页弹窗、七日签到、字卡详情、每日/无尽副本弹窗、结算托盘整套切图按设计稿两倍导出（2px = 1 设计单位）。
        bool modalBig = path.EndsWith("/checkin_card_day7.png") || path.Contains("/Ui/glyph_stage_")
                        || path.Contains("/Ui/dungeon_banner_");
        bool modalKit = path.Contains("/Ui/modal_") || path.Contains("/Ui/checkin_")
                        || path.Contains("/Ui/starter_") || path.EndsWith("/codex_card_locked.png")
                        || path.Contains("/Ui/glyph_") || path.EndsWith("/victory_loot_tray.png")
                        || path.Contains("/Ui/dungeon_shell_") || path.Contains("/Ui/dungeon_crest_")
                        || path.Contains("/Ui/dungeon_banner_") || path.Contains("/Ui/dungeon_pill_")
                        || path.Contains("/Ui/dungeon_intel_") || path.Contains("/Ui/dungeon_axis_")
                        || path.Contains("/Ui/dungeon_ban_");
        // 玩法底板是 1280×480 的整张图，弧线不能九宫格拉，也不能压到 256。
        bool modePlate = path.Contains("/mode_plate");
        // 图鉴底板铺满整屏、星级条拉到 570 宽，256 会糊。
        bool codexBig = path.EndsWith("/codex_board.png") || path.Contains("/codex_row");
        // 图鉴卡按原比例整张贴，不走九宫格；格子 190 宽、详情 196 宽，留两倍超采样。
        bool codexCard = path.Contains("/codex_face") || path.EndsWith("/codex_back.png")
                         || path.Contains("/Ui/loot_") || path.Contains("/Ui/study_card_");
        int size = stage || homeBg || modePlate || terrace || modalBig ? 2048
            : (panel || codexBig || home || itemKit || skinHex || modalKit ? 1024 : codexCard ? 512 : 256);
        importer.maxTextureSize = size;
        if (panel) importer.spritePixelsPerUnit = 100;
        else if (modalKit) importer.spritePixelsPerUnit = 200;
        // 面板要给 Sprite.Create 做九宫格兜底，得留 CPU 副本。
        if (panel) importer.isReadable = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 1;
        settings.readable = cpu || panel;
        importer.SetTextureSettings(settings);
        // SetTextureSettings 会把 border 冲掉，九宫格必须写在它后面。
        if (path.EndsWith("/tab_dock.png"))
            importer.spriteBorder = new Vector4(88f, 70f, 88f, 70f);
        else if (path.Contains("/panel_board") || path.Contains("/panel_spell"))
            importer.spriteBorder = Vector4.zero;
        else if (path.EndsWith("/panel_item_shelf.png"))
            importer.spriteBorder = new Vector4(110f, 70f, 110f, 70f);
        else if (path.Contains("/panel_item_card") || path.EndsWith("/panel_item_plaque.png"))
            importer.spriteBorder = Vector4.zero;
        else if (path.Contains("/panel_card"))
            importer.spriteBorder = new Vector4(18f, 18f, 18f, 18f);
        else if (path.Contains("/panel_skin"))
            importer.spriteBorder = new Vector4(30f, 34f, 30f, 30f);
        else if (path.Contains("/panel_strip"))
            importer.spriteBorder = new Vector4(28f, 10f, 28f, 10f);
        else if (path.Contains("/panel_row"))
            importer.spriteBorder = new Vector4(44f, 8f, 44f, 8f);
        else if (path.Contains("/panel_price"))
            importer.spriteBorder = new Vector4(22f, 8f, 22f, 8f);
        else if (path.EndsWith("/codex_board.png"))
            importer.spriteBorder = new Vector4(80f, 80f, 80f, 80f);
        else if (path.Contains("/codex_row"))
            importer.spriteBorder = new Vector4(110f, 100f, 110f, 100f);
        else if (path.EndsWith("/loot_fill.png"))
            importer.spriteBorder = new Vector4(16f, 0f, 16f, 0f);
        else if (path.EndsWith("/codex_box.png"))
            importer.spriteBorder = new Vector4(48f, 48f, 48f, 48f);
        // 图鉴底只圆上沿。44 像素边对应设计稿上 22 的圆角（这张图 1 像素 = 0.5 设计单位）。
        if (path.EndsWith("/item_panel.png"))
        {
            importer.spritePixelsPerUnit = 2f;
            importer.spriteBorder = new Vector4(44f, 8f, 44f, 44f);
        }
        else if (path.EndsWith("/modal_panel_body.png"))
            importer.spriteBorder = new Vector4(72f, 72f, 72f, 72f);
        else if (path.EndsWith("/modal_head_yellow.png") || path.EndsWith("/modal_head_blue.png"))
            importer.spriteBorder = new Vector4(64f, 0f, 64f, 0f);
        else if (path.EndsWith("/modal_info_strip.png"))
            importer.spriteBorder = new Vector4(56f, 0f, 56f, 0f);
        else if (path.EndsWith("/modal_sub_cream.png") || path.EndsWith("/modal_sub_warm.png")
                 || path.EndsWith("/modal_sub_bright.png") || path.EndsWith("/modal_sub_dim.png")
                 || path.EndsWith("/modal_rank_row_me.png"))
            importer.spriteBorder = new Vector4(44f, 44f, 44f, 44f);
        else if (path.EndsWith("/modal_sub_dark.png") || path.EndsWith("/modal_sub_charcoal.png")
                 || path.Contains("/modal_rank_row_"))
            importer.spriteBorder = new Vector4(40f, 40f, 40f, 40f);
        else if (path.EndsWith("/modal_sub_tip.png") || path.EndsWith("/modal_tab_on.png"))
            importer.spriteBorder = new Vector4(30f, 0f, 30f, 0f);
        else if (path.EndsWith("/modal_sub_honey.png"))
            importer.spriteBorder = new Vector4(44f, 44f, 44f, 44f);
        else if (path.Contains("/modal_tag_"))
            importer.spriteBorder = new Vector4(22f, 0f, 22f, 0f);
        else if (path.EndsWith("/modal_prog_track.png"))
            importer.spriteBorder = new Vector4(34f, 0f, 34f, 0f);
        else if (path.EndsWith("/modal_prog_fill.png"))
            importer.spriteBorder = new Vector4(26f, 0f, 26f, 0f);
        else if (path.Contains("/modal_divider"))
            importer.spriteBorder = new Vector4(8f, 0f, 8f, 0f);
        else if (path.EndsWith("/modal_seal_count.png"))
            importer.spriteBorder = new Vector4(16f, 16f, 16f, 16f);
        else if (path.EndsWith("/victory_loot_tray.png"))
            importer.spriteBorder = new Vector4(52f, 52f, 52f, 52f);
        else if (path.Contains("/dungeon_shell_"))
            importer.spriteBorder = new Vector4(84f, 108f, 84f, 84f);
        else if (path.Contains("/dungeon_intel_"))
            importer.spriteBorder = new Vector4(40f, 40f, 40f, 40f);
        else if (path.Contains("/dungeon_pill_"))
            importer.spriteBorder = new Vector4(24f, 0f, 24f, 0f);
        else if (path.EndsWith("/dungeon_axis_track.png"))
            importer.spriteBorder = new Vector4(22f, 0f, 22f, 0f);
        else if (path.Contains("/dungeon_axis_fill_"))
            importer.spriteBorder = new Vector4(14f, 0f, 14f, 0f);
        else if (path.Contains("/glyph_tag_"))
            importer.spriteBorder = new Vector4(32f, 0f, 32f, 0f);
        else if (path.EndsWith("/glyph_slot_cap.png"))
            importer.spriteBorder = new Vector4(24f, 0f, 24f, 0f);
        else if (path.EndsWith("/glyph_bonus_bar.png"))
            importer.spriteBorder = new Vector4(40f, 0f, 40f, 0f);
        else if (path.EndsWith("/glyph_lv_track.png"))
            importer.spriteBorder = new Vector4(28f, 0f, 28f, 0f);
        // 写在 SetTextureSettings 后面。只改 maxTextureSize 时，微信目标仍用 meta 里的 256，草地会被压糊。
        if (home || homeBg || stage || modePlate || itemKit || terrace || skinHex || modalKit || codexCard)
            ApplyUiSize(importer, size);
    }

    static void ApplyUiSize(TextureImporter importer, int size)
    {
        var def = importer.GetDefaultPlatformTextureSettings();
        def.maxTextureSize = size;
        def.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(def);
        foreach (string platform in new[] { "Standalone", "WebGL", "WeixinMiniGame" })
        {
            var plat = importer.GetPlatformTextureSettings(platform);
            plat.overridden = true;
            plat.maxTextureSize = size;
            plat.textureCompression = TextureImporterCompression.Uncompressed;
            plat.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(plat);
        }
    }
}

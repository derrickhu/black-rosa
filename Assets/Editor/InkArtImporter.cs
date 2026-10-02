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
        // 靠命名认：弹体渐变 `<元素>_shot_NN`、状态层 `burn_body_/ice_crust_/dot_*`。
        // `_shot_` 里那两条下划线是有意的 —— 它要排除旧的柔光单图 `shot_ice.png`。
        bool flatVfx = vfx && (path.Contains("_shot_")
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
        importer.maxTextureSize = stage ? 2048 : (panel ? 1024 : 256);
        if (panel) importer.spritePixelsPerUnit = 100;
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
    }
}

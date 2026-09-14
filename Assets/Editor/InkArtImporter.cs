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
        bool cpu = !vfx && !ui;
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
        importer.textureCompression = vfx
            ? TextureImporterCompression.Compressed
            : TextureImporterCompression.Uncompressed;
        importer.isReadable = cpu;
        // 屏幕上最大的用法是 160px 的抽卡字面，256 已经是两倍超采样。
        // 首页字标要铺到 560 宽，压到 256 会糊，单独放到 1024。
        bool logo = path.EndsWith("/Resources/Art/Ui/logo.png", System.StringComparison.Ordinal);
        importer.maxTextureSize = logo ? 1024 : 256;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 8;
        settings.readable = cpu;
        importer.SetTextureSettings(settings);
    }
}

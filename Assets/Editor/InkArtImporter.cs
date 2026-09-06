using UnityEditor;
using UnityEngine;

public sealed class InkArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string path = assetPath.Replace('\\', '/');
        if (path.IndexOf("/Resources/Art/", System.StringComparison.Ordinal) < 0) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 128;
        importer.isReadable = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteExtrude = 8;
        importer.SetTextureSettings(settings);
    }
}

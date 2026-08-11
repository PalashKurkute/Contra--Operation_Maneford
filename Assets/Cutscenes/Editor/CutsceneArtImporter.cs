using UnityEditor;

// Pixel art dropped anywhere under Assets/Cutscenes imports as
// crisp sprites at the forest pack's scale (16 pixels per unit),
// so new cutscene art matches the sets without manual setup.
// Only the first import is touched; later Inspector changes stick.
public class CutsceneArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Cutscenes/") ||
            !assetImporter.importSettingsMissing)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}

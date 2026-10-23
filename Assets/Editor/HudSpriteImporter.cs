using UnityEditor;
using UnityEngine;

// Import rule for the generated HUD sprites (Tools/gen_hud_sprites.py writes them
// into Sprites/UI/HUD/Generated): single sprites at the canvas's reference PPU,
// point-filtered and uncompressed so every mockup pixel lands 1:1 on screen.
class HudSpriteImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Sprites/UI/HUD/Generated/";

    // Frames that may be stretched (Image.Type.Sliced) get their border width as a
    // 9-slice border, so the edges stay exactly 2px at any size.
    static readonly string[] SlicedFrames = { "subtab_selected", "subtab_unselected" };
    const int FrameBorder = 2;

    // bump to reimport everything in Folder after changing the rules below
    public override uint GetVersion() => 2;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;   // = the Screen Space Canvas's reference PPU
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        settings.spriteBorder = System.Array.IndexOf(SlicedFrames, name) >= 0 ? Vector4.one * FrameBorder : Vector4.zero;
        ti.SetTextureSettings(settings);
    }
}

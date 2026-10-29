using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Builds the grass ground tileset from Sprites/Tiles/Grass/grass tileset.png, a sheet of
// 32x32 pieces cut from big tile.aseprite on the ground grid (32x16 diamond tops):
// the front corner (both side faces), edges with the SE or SW side face, and the
// plain fill top. Slices the sheet, makes a Tile per piece, an Isometric Rule Tile
// that picks the piece from its neighbours -- a side face wherever the cell in front
// (down-left x-1, down-right y-1) isn't grass -- and a palette with all of them.
//
// Tools/Tiles/Build Grass Tileset. Safe to re-run after editing the sheet: the sprites
// keep their IDs, so tiles already painted stay linked. Adding a variant: draw it in
// the next free cell, add its name to Pieces (prefix grass_se_ / grass_sw_ /
// grass_fill_), re-run.
static class GrassTilesetBuilder
{
    const string Folder = "Assets/Sprites/Tiles/Grass";
    const string SheetPath = Folder + "/grass tileset.png";
    const string TileFolder = Folder + "/Tiles";
    const string RuleTilePath = Folder + "/Grass Rule Tile.asset";
    const string PaletteName = "grass-palette";
    const string ReferenceSheet = "Assets/Sprites/Tiles/isometric tileset.png";   // import settings to match

    const int Cell = 32;
    const int Columns = 4;

    // The piece's 32x16 top fills the cell's top half, so its centre -- which goes on the
    // grid cell's centre (the tilemap's tile anchor) -- is 8px below the cell's top.
    static readonly Vector2 Pivot = new(0.5f, 0.75f);

    // the sheet, left to right, top to bottom
    static readonly string[] Pieces = { "grass_corner", "grass_se_0", "grass_se_1", "grass_sw_0", "grass_fill_0" };

    static readonly Vector3Int DownLeft = new(-1, 0, 0);    // the cell in front of the SW edge
    static readonly Vector3Int DownRight = new(0, -1, 0);   // the cell in front of the SE edge

    [MenuItem("Tools/Tiles/Build Grass Tileset")]
    static void Build()
    {
        if (!Slice()) return;

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>()
            .ToDictionary(s => s.name);
        if (Pieces.Any(p => !sprites.ContainsKey(p)))
        {
            Debug.LogError($"[GrassTilesetBuilder] {SheetPath} is missing sprites after slicing.");
            return;
        }

        var tiles = Pieces.Select(p => MakeTile(p, sprites[p])).ToList();
        IsometricRuleTile rule = MakeRuleTile(sprites);
        MakePalette(rule, tiles);

        AssetDatabase.SaveAssets();
        Debug.Log($"[GrassTilesetBuilder] Built {Pieces.Length} grass tiles, the Grass Rule Tile and {PaletteName}.");
    }

    // ------------------------------------------------------------------ sprites

    static bool Slice()
    {
        var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        var reference = AssetImporter.GetAtPath(ReferenceSheet) as TextureImporter;
        if (importer == null || reference == null)
        {
            Debug.LogError($"[GrassTilesetBuilder] Missing {SheetPath} or {ReferenceSheet}.");
            return false;
        }

        // same import as the ground tileset: sprites at 32 PPU, point-filtered, uncompressed
        var settings = new TextureImporterSettings();
        reference.ReadTextureSettings(settings);
        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = reference.spritePixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out _, out int height);

        // keep each sprite's ID across re-runs, so painted tiles stay linked
        Dictionary<string, GUID> ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < Pieces.Length; i++)
        {
            int col = i % Columns, row = i / Columns;
            rects.Add(new SpriteRect
            {
                name = Pieces[i],
                rect = new Rect(col * Cell, height - (row + 1) * Cell, Cell, Cell),
                alignment = SpriteAlignment.Custom,
                pivot = Pivot,
                spriteID = ids.TryGetValue(Pieces[i], out GUID id) ? id : GUID.Generate(),
            });
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
            .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        return true;
    }

    // ------------------------------------------------------------------ tiles

    static Tile MakeTile(string name, Sprite sprite)
    {
        Tile tile = LoadOrCreate<Tile>($"{TileFolder}/{name}.asset");
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.Sprite;   // as the ground tileset's tiles
        tile.flags = TileFlags.LockColor;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // First matching rule wins: the corner where both front cells are open, then each
    // edge, then the fill (random among its variants).
    static IsometricRuleTile MakeRuleTile(Dictionary<string, Sprite> sprites)
    {
        Sprite[] Group(string prefix) => Pieces.Where(p => p.StartsWith(prefix)).Select(p => sprites[p]).ToArray();
        Sprite[] fill = Group("grass_fill_");

        IsometricRuleTile rule = LoadOrCreate<IsometricRuleTile>(RuleTilePath);
        rule.m_DefaultSprite = fill[0];
        rule.m_DefaultColliderType = Tile.ColliderType.Sprite;
        rule.m_TilingRules = new List<RuleTile.TilingRule>
        {
            Rule(Group("grass_corner"), (DownLeft, false), (DownRight, false)),
            Rule(Group("grass_se_"), (DownRight, false)),
            Rule(Group("grass_sw_"), (DownLeft, false)),
            Rule(fill),
        };
        EditorUtility.SetDirty(rule);
        return rule;
    }

    // a rule outputting `sprites` (at random when there are several) when each listed
    // neighbour is (true) or isn't (false) grass
    static RuleTile.TilingRule Rule(Sprite[] sprites, params (Vector3Int at, bool grass)[] neighbours)
    {
        var rule = new RuleTile.TilingRule
        {
            m_Sprites = sprites,
            m_Output = sprites.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random
                                          : RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_ColliderType = Tile.ColliderType.Sprite,
        };
        rule.ApplyNeighbors(neighbours.ToDictionary(
            n => n.at,
            n => n.grass ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis));
        return rule;
    }

    // ------------------------------------------------------------------ palette

    // The rule tile first, then every piece, in a row; laid out like the ground
    // tileset's palette (rectangle cells, sorted along z).
    static void MakePalette(TileBase rule, List<Tile> tiles)
    {
        string path = $"{Folder}/{PaletteName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            GridPaletteUtility.CreateNewPalette(Folder, PaletteName, GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.CustomAxis, new Vector3(0f, 0f, 1f));

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
        tilemap.ClearAllTiles();
        tilemap.SetTile(Vector3Int.zero, rule);
        for (int i = 0; i < tiles.Count; i++) tilemap.SetTile(new Vector3Int(i + 2, 0, 0), tiles[i]);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'),
                                       System.IO.Path.GetFileName(folder));

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}

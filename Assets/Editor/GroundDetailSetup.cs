using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.Tilemaps;

// Sets up the Ground Detail tilemap: a second tilemap on the scene's Grid, drawn just
// above the ground (Background layer; ground -1, detail 0, then the snow trail at 1,
// splatters at 2, shadows at 3), for flat details painted over the snow such as grass
// patches. Its tiles are bigger than a cell and overlap freely, so patches don't read
// as squares. Chunk mode: the whole layer renders as a few meshes, however many patches
// get painted.
//
// Every .aseprite in Folder is a patch source, every frame of it a TerrainTile: "big
// tile - X" makes "X patch" (Frame_0) and "X patch N" (Frame_N), a file without that
// prefix its own name. X names the ground type too (grass, ice, snow, stone, dirt; see
// GroundType), so name a file after the ground it is. Each file is set to import the
// way the tiles need (Merge Frame, the ground's 32 PPU, pivot at the canvas's
// bottom-centre). Each frame is a 4x4-cell block on a 128x128 canvas, like the grass:
// start a new file with Save As from it. The painted cell's centre is the point 3 px
// above the canvas centre, so the block's middle sits on the cell clicked. The sprites
// are linked by name here, since the Aseprite importer assigns sprite IDs at random.
//
// Each tile bakes a mask of its frame's pixels (alpha at least half, eroded EdgeInset
// px past the soft outline): where on the art an entity counts as on that ground
// (TerrainMap). The art holds only the ground itself -- a guide layer under it must be
// hidden, so it isn't imported. GroundDetailImport re-runs the tile, mask and palette
// part (Rebuild) whenever a file here is saved, added or removed.
//
// Tools/Tiles/Set Up Ground Detail, with Camp.unity open (save the scene after): Rebuild,
// plus the scene's Ground Detail tilemap and its TerrainMap. Safe to re-run: the
// palette is rebuilt with one row per file, its frames left to right, and tiles
// already painted stay linked (a tile whose frame or file is gone is kept and logged,
// since painted cells may still use it).
static class GroundDetailSetup
{
    const string Folder = "Assets/Sprites/Tiles/Ground Detail";
    const string FramePrefix = "Frame_";
    const string FilePrefix = "big tile - ";
    const string TileSuffix = " patch";
    const string PaletteName = "ground-detail-palette";
    const string LayerName = "Ground Detail";
    const float PixelsPerUnit = 32f;

    // the painted cell's centre, in pixels above the canvas centre
    const float AnchorAboveCentre = 3f;

    // mask: a pixel counts at this alpha, and the outermost EdgeInset px are dropped
    const byte AlphaThreshold = 128;
    const int EdgeInset = 1;

    const int GroundOrder = -1;
    const int DetailOrder = 0;

    internal const string SourceFolder = Folder;

    [MenuItem("Tools/Tiles/Set Up Ground Detail")]
    static void SetUp()
    {
        if (!Rebuild()) return;
        if (!MakeLayer()) return;
        AssetDatabase.SaveAssets();
        Debug.Log($"[GroundDetailSetup] '{LayerName}' tilemap and its TerrainMap ready. Save the scene.");
    }

    // The tiles (with their masks) and the palette, from the files in Folder. Needs no scene.
    internal static bool Rebuild()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Sprites/Tiles", "Ground Detail");

        string[] sources = AssetDatabase.FindAssets("", new[] { Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetExtension(p) == ".aseprite" && Path.GetDirectoryName(p).Replace('\\', '/') == Folder)
            .OrderBy(p => p)
            .ToArray();

        var rows = new List<Tile[]>();
        foreach (string source in sources)
        {
            Tile[] row = MakeTiles(source);
            if (row.Length > 0) rows.Add(row);
        }
        if (rows.Count == 0)
        {
            Debug.LogError($"[GroundDetailSetup] No .aseprite with '{FramePrefix}N' sprites in {Folder}.");
            return false;
        }

        MakePalette(rows);
        LogStale(rows.SelectMany(r => r));
        AssetDatabase.SaveAssets();
        Debug.Log($"[GroundDetailSetup] {rows.Sum(r => r.Length)} tile(s) from {rows.Count} file(s), masks baked " +
                  $"({PaletteName}).");
        return true;
    }

    // One tile per frame of the file, in frame order; empty (logged) when it can't be used.
    static Tile[] MakeTiles(string source)
    {
        if (AssetImporter.GetAtPath(source) is not AsepriteImporter importer)
        {
            Debug.LogError($"[GroundDetailSetup] {source} isn't imported as an Aseprite file; skipped.");
            return new Tile[0];
        }
        Configure(importer);

        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>()
            .Where(s => s.name.StartsWith(FramePrefix))
            .OrderBy(FrameIndex)
            .ToArray();
        if (frames.Length == 0)
        {
            Debug.LogError($"[GroundDetailSetup] No '{FramePrefix}N' sprites in {source}; skipped.");
            return new Tile[0];
        }

        string file = Path.GetFileNameWithoutExtension(source);
        string ground = file.StartsWith(FilePrefix) ? file.Substring(FilePrefix.Length) : file;
        string name = file.StartsWith(FilePrefix) ? ground + TileSuffix : file;
        if (!System.Enum.TryParse(ground, true, out GroundType type))
        {
            type = GroundType.Snow;
            Debug.LogWarning($"[GroundDetailSetup] {source}: '{ground}' isn't a ground type " +
                             $"({string.Join(", ", System.Enum.GetNames(typeof(GroundType)))}); its tiles count as Snow.");
        }

        // with the pivot at the canvas's bottom-centre, the anchor is straight above it
        var offset = new Vector2(0f, -(importer.canvasSize.y / 2f + AnchorAboveCentre) / PixelsPerUnit);
        return frames.Select(f => MakeTile(f, name, offset, type)).Where(t => t != null).ToArray();
    }

    // Import the way the tiles need; reimports only on a change.
    static void Configure(AsepriteImporter importer)
    {
        bool changed = false;
        if (importer.layerImportMode != LayerImportModes.MergeFrame)
        {
            importer.layerImportMode = LayerImportModes.MergeFrame;
            changed = true;
        }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
        {
            importer.spritePixelsPerUnit = PixelsPerUnit;
            changed = true;
        }
        if (importer.pivotSpace != PivotSpaces.Canvas)
        {
            importer.pivotSpace = PivotSpaces.Canvas;
            changed = true;
        }
        if (importer.pivotAlignment != SpriteAlignment.BottomCenter)
        {
            importer.pivotAlignment = SpriteAlignment.BottomCenter;
            changed = true;
        }
        if (!changed) return;
        EditorUtility.SetDirty(importer);   // the setters don't, and SaveAndReimport only saves a dirty importer
        importer.SaveAndReimport();
    }

    static int FrameIndex(Sprite sprite) =>
        int.TryParse(sprite.name.Substring(FramePrefix.Length), out int i) ? i : int.MaxValue;

    // null (logged) when a plain Tile already holds the path
    static TerrainTile MakeTile(Sprite sprite, string name, Vector2 offset, GroundType ground)
    {
        int index = FrameIndex(sprite);
        string path = $"{Folder}/{(index == 0 ? name : $"{name} {index}")}.asset";
        TileBase existing = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (existing != null && existing is not TerrainTile)
        {
            Debug.LogError($"[GroundDetailSetup] {path} is a plain {existing.GetType().Name}, not a TerrainTile; " +
                           "delete it (erase it where painted first) and re-run.");
            return null;
        }

        var tile = existing as TerrainTile;
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<TerrainTile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.ground = ground;
        tile.colliderType = Tile.ColliderType.None;   // flat on the ground
        tile.transform = Matrix4x4.Translate(offset);
        tile.flags = TileFlags.LockColor | TileFlags.LockTransform;
        BakeMask(tile, sprite);
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // Which of the sprite's pixels are its ground: alpha at least AlphaThreshold, minus
    // the EdgeInset px nearest the edge (a pixel survives only if all 4 neighbours
    // within the inset are solid too), in sprite pixels with the pivot at (0, 0).
    static void BakeMask(TerrainTile tile, Sprite sprite)
    {
        Color32[] pixels = SpritePixels.Read(sprite, out int w, out int h);
        var mask = new bool[w * h];
        for (int i = 0; i < mask.Length; i++) mask[i] = pixels[i].a >= AlphaThreshold;

        for (int pass = 0; pass < EdgeInset; pass++)
        {
            var eroded = new bool[mask.Length];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                eroded[i] = mask[i]
                            && x > 0 && mask[i - 1] && x < w - 1 && mask[i + 1]
                            && y > 0 && mask[i - w] && y < h - 1 && mask[i + w];
            }
            mask = eroded;
        }

        // sprite.pivot is in sprite-rect pixels; textureRectOffset is where the packed
        // (trimmed) pixels start inside that rect (as SnowStamp places its bands)
        Vector2 origin = sprite.textureRectOffset - sprite.pivot;
        tile.SetMask(mask, new Vector2Int(Mathf.RoundToInt(origin.x), Mathf.RoundToInt(origin.y)),
                     new Vector2Int(w, h), sprite.pixelsPerUnit);
    }

    // The palette holds exactly the files' tiles: a row per file, its frames left to right.
    static void MakePalette(List<Tile[]> rows)
    {
        string path = $"{Folder}/{PaletteName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            GridPaletteUtility.CreateNewPalette(Folder, PaletteName, GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.CustomAxis, new Vector3(0f, 0f, 1f));

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
        tilemap.ClearAllTiles();
        for (int y = 0; y < rows.Count; y++)
            for (int x = 0; x < rows[y].Length; x++)
                tilemap.SetTile(new Vector3Int(x, -y, 0), rows[y][x]);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // Tiles in Folder that no frame made this run (their frame or file is gone).
    static void LogStale(IEnumerable<Tile> made)
    {
        var current = new HashSet<Tile>(made);
        string[] stale = AssetDatabase.FindAssets("t:Tile", new[] { Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !current.Contains(AssetDatabase.LoadAssetAtPath<Tile>(p)))
            .ToArray();
        if (stale.Length > 0)
            Debug.LogWarning("[GroundDetailSetup] Not in the palette (no frame makes them any more; delete them " +
                             "once nothing painted uses them):\n" + string.Join("\n", stale));
    }

    // The detail tilemap beside the ground one, under the scene's Grid, sorted just above it.
    static bool MakeLayer()
    {
        Grid grid = Object.FindFirstObjectByType<Grid>();
        Tilemap ground = grid != null
            ? grid.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(t => t.name != LayerName)
            : null;
        if (ground == null)
        {
            Debug.LogError("[GroundDetailSetup] Open Camp.unity first (needs its Grid and ground Tilemap).");
            return false;
        }
        var groundRenderer = ground.GetComponent<TilemapRenderer>();

        Transform existing = grid.transform.Find(LayerName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(LayerName, typeof(Tilemap), typeof(TilemapRenderer));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Ground Detail");
            go.transform.SetParent(grid.transform, false);
            go.transform.SetSiblingIndex(ground.transform.GetSiblingIndex() + 1);
        }

        var tilemap = go.GetComponent<Tilemap>();
        tilemap.tileAnchor = ground.tileAnchor;
        tilemap.orientation = ground.orientation;
        // painted cells keep the transform they were painted with until refreshed
        tilemap.RefreshAllTiles();

        // what entities ask for the ground under their feet (GroundSensor, SnowTrail)
        if (!go.TryGetComponent(out TerrainMap _)) Undo.AddComponent<TerrainMap>(go);

        var renderer = go.GetComponent<TilemapRenderer>();
        renderer.sharedMaterial = groundRenderer.sharedMaterial;
        renderer.sortingLayerID = groundRenderer.sortingLayerID;
        renderer.sortingOrder = DetailOrder;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        renderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;

        Undo.RecordObject(groundRenderer, "Ground Detail");
        groundRenderer.sortingOrder = GroundOrder;

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        return true;
    }
}

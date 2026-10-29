using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

// Sets up the Ground Detail tilemap: a second tilemap on the scene's Grid, drawn just
// above the ground (Background layer; ground -1, detail 0, then the snow trail at 1,
// splatters at 2, shadows at 3), for flat details painted over the snow such as grass
// patches. Its tiles are bigger than a cell and overlap freely, so patches don't read
// as squares. Chunk mode: the whole layer renders as a few meshes, however many patches
// get painted.
//
// The grass patch tile is Frame_0 of big tile - grass.aseprite, a 4x4-cell block (at 32
// PPU, the ground's), centred so the painted cell is one of the block's four middle
// cells. The sprite is linked by name here, since the Aseprite importer assigns sprite
// IDs at random.
//
// Tools/Tiles/Set Up Ground Detail, with Camp.unity open (save the scene after). Safe
// to re-run.
static class GroundDetailSetup
{
    const string Folder = "Assets/Sprites/Tiles/Ground Detail";
    const string SourcePath = "Assets/Sprites/Tiles/big tile - grass.aseprite";
    const string SpriteName = "Frame_0";
    const string TilePath = Folder + "/grass patch.asset";
    const string PaletteName = "ground-detail-palette";
    const string LayerName = "Ground Detail";

    // centre of one of the block's four middle cells, in pixels from the sprite's
    // bottom-left (the 32x16 diamonds' grid over the 128x75 block)
    static readonly Vector2 Anchor = new(64f, 35f);

    const int GroundOrder = -1;
    const int DetailOrder = 0;

    [MenuItem("Tools/Tiles/Set Up Ground Detail")]
    static void SetUp()
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Sprite>().FirstOrDefault(s => s.name == SpriteName);
        if (sprite == null)
        {
            Debug.LogError($"[GroundDetailSetup] No sprite '{SpriteName}' in {SourcePath}.");
            return;
        }

        Tile tile = MakeTile(sprite);
        MakePalette(tile);
        if (!MakeLayer()) return;

        AssetDatabase.SaveAssets();
        Debug.Log($"[GroundDetailSetup] '{LayerName}' tilemap ready with the grass patch tile ({PaletteName}). Save the scene.");
    }

    static Tile MakeTile(Sprite sprite)
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Sprites/Tiles", "Ground Detail");

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, TilePath);
        }

        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;   // flat on the ground
        // shift the sprite so Anchor, wherever the importer put the pivot, sits on the
        // painted cell's centre (the tilemap's tile anchor)
        tile.transform = Matrix4x4.Translate((sprite.pivot - Anchor) / sprite.pixelsPerUnit);
        tile.flags = TileFlags.LockColor | TileFlags.LockTransform;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    static void MakePalette(TileBase tile)
    {
        string path = $"{Folder}/{PaletteName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            GridPaletteUtility.CreateNewPalette(Folder, PaletteName, GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ,
                TransparencySortMode.CustomAxis, new Vector3(0f, 0f, 1f));

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Tilemap tilemap = root.GetComponentInChildren<Tilemap>();
        tilemap.ClearAllTiles();
        tilemap.SetTile(Vector3Int.zero, tile);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
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

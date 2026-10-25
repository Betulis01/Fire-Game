using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

// Import rule + wiring for the snow trail art (Sprites/Effects/Snow Walking).
// On a file's first import it's set up for SnowTrail: Individual Layers (one sprite
// per layer: Buttom = floor, Middle = wall, Top = rim), the world's 32 PPU, and a
// canvas-space pivot on the guide tile's centre (canvas pixel 16,24), so all three
// layers line up and the stamp centres on the feet. No prefab or clips -- these are
// stamps, not animations. A hidden Green guide layer isn't imported.
//
// The folder is the source of truth for the trail's stamp variants: whenever an
// .aseprite in it is imported or deleted, the Player's SnowTrail.variants is rebuilt
// with one entry per file (sorted by name), so dropping in a snow-trail3.aseprite
// adds a variant with no other step. Tools/Snow Trail/Set Up Player does the same.
class SnowTrailImport : AssetPostprocessor
{
    const string Folder = "Assets/Sprites/Effects/Snow Walking/";
    const string PlayerPrefabPath = "Assets/Prefabs/Entities/Player/Player.prefab";

    void OnPreprocessAsset()
    {
        if (!assetPath.StartsWith(Folder) || assetImporter is not AsepriteImporter importer) return;
        if (!importer.importSettingsMissing) return;   // first import only; later edits are the artist's

        importer.importMode = FileImportModes.AnimatedSprite;
        importer.layerImportMode = LayerImportModes.IndividualLayers;
        importer.spritePixelsPerUnit = SnowTrailCanvas.PixelsPerUnit;
        importer.pivotSpace = PivotSpaces.Canvas;
        importer.pivotAlignment = SpriteAlignment.Custom;
        importer.customPivotPosition = new Vector2(0.5f, 0.25f);
        importer.generateModelPrefab = false;
        importer.generateAnimationClips = false;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        bool InFolder(string p) => p.StartsWith(Folder) && p.EndsWith(".aseprite");
        if (imported.Any(InFolder) || deleted.Any(InFolder) || moved.Any(InFolder) || movedFrom.Any(InFolder))
            EditorApplication.delayCall += SetUpPlayer;
    }

    [MenuItem("Tools/Snow Trail/Set Up Player")]
    static void SetUpPlayer()
    {
        var variants = new System.Collections.Generic.List<SnowTrail.StampVariant>();
        foreach (string path in AssetDatabase.FindAssets("", new[] { Folder.TrimEnd('/') })
                                             .Select(AssetDatabase.GUIDToAssetPath)
                                             .Where(p => p.EndsWith(".aseprite"))
                                             .OrderBy(p => p))
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            var v = new SnowTrail.StampVariant
            {
                floor = sprites.FirstOrDefault(s => s.name.StartsWith("Buttom")),
                wall = sprites.FirstOrDefault(s => s.name.StartsWith("Middle")),
                rim = sprites.FirstOrDefault(s => s.name.StartsWith("Top")),
            };
            if (v.floor == null || v.wall == null || v.rim == null)
            {
                Debug.LogWarning($"[SnowTrailImport] {path} needs Buttom, Middle and Top layer sprites " +
                                 "(Individual Layers import); skipped. Found: " + string.Join(", ", sprites.Select(s => s.name)));
                continue;
            }
            variants.Add(v);
        }

        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (!player.TryGetComponent(out SnowTrail trail)) trail = player.AddComponent<SnowTrail>();

            var so = new SerializedObject(trail);
            SerializedProperty list = so.FindProperty("variants");
            if (SameVariants(list, variants)) return;

            list.arraySize = variants.Count;
            for (int i = 0; i < variants.Count; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("floor").objectReferenceValue = variants[i].floor;
                e.FindPropertyRelative("wall").objectReferenceValue = variants[i].wall;
                e.FindPropertyRelative("rim").objectReferenceValue = variants[i].rim;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Debug.Log($"[SnowTrailImport] Player SnowTrail: {variants.Count} stamp variant(s) from {Folder}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    static bool SameVariants(SerializedProperty list, System.Collections.Generic.List<SnowTrail.StampVariant> variants)
    {
        if (list.arraySize != variants.Count) return false;
        for (int i = 0; i < variants.Count; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("floor").objectReferenceValue != variants[i].floor ||
                e.FindPropertyRelative("wall").objectReferenceValue != variants[i].wall ||
                e.FindPropertyRelative("rim").objectReferenceValue != variants[i].rim)
                return false;
        }
        return true;
    }
}

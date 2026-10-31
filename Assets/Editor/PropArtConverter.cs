using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

// Converts a prop from hand-linked layers (a body SpriteRenderer on the root, a
// SpriteLayerFollower child per other layer, hand-made clips keying the body) to the
// Aseprite importer's own art (see PropArt):
//   1. removes the root SpriteRenderer, root Animator (keeping its controller),
//      RandomSprite and the follower children, and clears references to them
//   2. nests the file's model prefab as the "Art" child and adds PropArt (body layer,
//      the kept controller), plus RandomVariant for an untagged multi-frame file
//   3. points each controller state at the importer clip of the same name (the
//      Aseprite tag), so the clips stay in sync with the file from then on
// Run once per prop: Tools/Aseprite/Convert Props To Importer Art. To convert a new
// prop, add it to Props (its file under PropArtImportHook's folders, imported as
// Individual Layers + Import Hidden Layers).
static class PropArtConverter
{
    readonly struct Prop
    {
        public readonly string prefab, aseprite, bodyLayer;

        public Prop(string prefab, string aseprite, string bodyLayer)
        {
            this.prefab = prefab;
            this.aseprite = aseprite;
            this.bodyLayer = bodyLayer;
        }
    }

    const string Prefabs = "Assets/Prefabs/Entities/";
    const string Sprites = "Assets/Sprites/";

    static readonly Prop[] Props =
    {
        new(Prefabs + "Ground/Stone.prefab", Sprites + "Ground/Stone/Stone.aseprite", "Stone"),
        new(Prefabs + "Ground/Scots Pine Wood.prefab", Sprites + "Ground/Wood/Wood.aseprite", "Wood"),
        new(Prefabs + "Ground/Grey Alder Wood.prefab", Sprites + "Ground/Wood/Grey Alder Wood.aseprite", "Wood"),
        new(Prefabs + "Ground/Stone Ore.prefab", Sprites + "Ground/Ores/Stone/Stone Ore.aseprite", "Layer 1"),
        new(Prefabs + "Ground/Iron Ore.prefab", Sprites + "Ground/Ores/Iron/Iron Ore.aseprite", "Layer 1"),
        new(Prefabs + "Ground/Scots Pine.prefab", Sprites + "Ground/Tree/Scots Pine/Scots_Pine.aseprite", "Root"),
        new(Prefabs + "Ground/Grey Alder.prefab", Sprites + "Ground/Tree/Grey Alder/Grey Alder.aseprite", "Root"),
        new(Prefabs + "Ground/Rock.prefab", Sprites + "Ground/Rock/Rock.aseprite", "Layer 1"),
        new(Prefabs + "Ground/Stick.prefab", Sprites + "Ground/Stick/Stick.aseprite", "Layer 1"),
        new(Prefabs + "Ground/Iron Stone.prefab", Sprites + "Ground/Iron Stone/Iron Stone.aseprite", "Layer 1"),
        new(Prefabs + "Ground/Sap.prefab", Sprites + "Ground/Sap/Sap.aseprite", "Layer 1"),
        new(Prefabs + "Ground/Iron Bar.prefab", Sprites + "Ground/Iron Bar/Iron Bar.aseprite", "Layer 1"),
        new(Prefabs + "Materials/Leather.prefab", Sprites + "Materials/Leather.aseprite", "Layer 1"),
        new(Prefabs + "Food/Polar Bear Meat/Polar Bear Meat.prefab",
            Sprites + "Ground/Food/Meat/Polar Bear Meat/Polar Bear Meat.aseprite", "Layer 1"),
        new(Prefabs + "Food/Polar Bear Meat/Polar Bear Meat Cooked.prefab",
            Sprites + "Ground/Food/Meat/Polar Bear Meat/Polar Bear Meat Cooked.aseprite", "Layer 1"),
        new(Prefabs + "Food/Red Berries/Blue Berries.prefab", Sprites + "Ground/Food/Berries/Blue Berries.aseprite", "Bush"),
    };

    [MenuItem("Tools/Aseprite/Convert Props To Importer Art")]
    static void ConvertAll()
    {
        int converted = 0;
        foreach (Prop prop in Props)
            if (Convert(prop)) converted++;
        AssetDatabase.SaveAssets();
        Debug.Log($"[PropArtConverter] Converted {converted} of {Props.Length} prop(s).");
    }

    static bool Convert(in Prop prop)
    {
        if (!PrepareImport(prop.aseprite)) return false;

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(prop.aseprite);
        if (model == null)
        {
            Debug.LogError($"[PropArtConverter] {prop.aseprite} has no model prefab (Generate Model Prefab off?).");
            return false;
        }
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(prop.aseprite).OfType<AnimationClip>().ToArray();

        GameObject root = PrefabUtility.LoadPrefabContents(prop.prefab);
        try
        {
            if (root.transform.Find(PropArt.ArtName) != null)
            {
                Debug.Log($"[PropArtConverter] {prop.prefab} already has its Art; skipped.");
                return false;
            }

            RuntimeAnimatorController controller = root.TryGetComponent(out Animator oldAnimator)
                ? oldAnimator.runtimeAnimatorController : null;
            StripHandLinkedArt(root);

            var art = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            art.name = PropArt.ArtName;
            art.transform.SetSiblingIndex(0);
            art.transform.localPosition = Vector3.zero;

            var so = new SerializedObject(root.AddComponent<PropArt>());
            so.FindProperty("bodyLayer").stringValue = prop.bodyLayer;
            so.FindProperty("controller").objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();

            // an untagged file's one clip spans every frame: its frames are variants
            string wholeFile = Path.GetFileNameWithoutExtension(prop.aseprite) + "_Clip";
            if (clips.Length == 1 && clips[0].name == wholeFile) root.AddComponent<RandomVariant>();

            if (controller is AnimatorController ac) LinkStates(ac, clips, prop.aseprite);

            PrefabUtility.SaveAsPrefabAsset(root, prop.prefab);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Variants play from the whole-file clip, so a multi-frame file must generate clips.
    static bool PrepareImport(string aseprite)
    {
        if (AssetImporter.GetAtPath(aseprite) is not AsepriteImporter importer)
        {
            Debug.LogError($"[PropArtConverter] {aseprite} isn't an Aseprite file.");
            return false;
        }
        if (importer.layerImportMode != LayerImportModes.IndividualLayers || !importer.generateModelPrefab)
        {
            Debug.LogError($"[PropArtConverter] {aseprite}: import as Individual Layers with Generate Model Prefab on.");
            return false;
        }
        bool multiFrame = AssetDatabase.LoadAllAssetsAtPath(aseprite).OfType<Sprite>().Any(s => s.name.EndsWith("_Frame_1"));
        if (multiFrame && !importer.generateAnimationClips)
        {
            importer.generateAnimationClips = true;
            EditorUtility.SetDirty(importer);   // the setter doesn't, and SaveAndReimport only saves a dirty importer
            importer.SaveAndReimport();
        }
        return true;
    }

    static void StripHandLinkedArt(GameObject root)
    {
        foreach (SpriteLayerFollower follower in root.GetComponentsInChildren<SpriteLayerFollower>(true))
            if (follower != null && follower.gameObject != root) Object.DestroyImmediate(follower.gameObject);

        if (root.TryGetComponent(out RandomSprite random)) Object.DestroyImmediate(random);
        if (root.TryGetComponent(out Animator animator)) Object.DestroyImmediate(animator);
        if (root.TryGetComponent(out SpriteRenderer body)) Object.DestroyImmediate(body);

        // references into the removed art now resolve through PropArt by name
        if (root.TryGetComponent(out Highlightable highlightable))
            Clear(highlightable, "overlay", "baseRenderer", "variants");
        foreach (HitAnimation hit in root.GetComponentsInChildren<HitAnimation>(true)) Clear(hit, "animator");
        foreach (CollideAnimation collide in root.GetComponentsInChildren<CollideAnimation>(true)) Clear(collide, "animator");
        foreach (SpriteShadow shadow in root.GetComponentsInChildren<SpriteShadow>(true)) Clear(shadow, "layers");
        foreach (OccluderFade fade in root.GetComponentsInChildren<OccluderFade>(true)) Clear(fade, "renderers", "sortReference");
    }

    static void Clear(Object target, params string[] fields)
    {
        var so = new SerializedObject(target);
        foreach (string field in fields)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) continue;
            if (p.isArray) p.arraySize = 0;
            else if (p.propertyType == SerializedPropertyType.ObjectReference) p.objectReferenceValue = null;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Each state plays the importer clip of its name (the Aseprite tag). A state with a
    // clip but no such tag keeps its old clip and is reported.
    static void LinkStates(AnimatorController controller, AnimationClip[] clips, string aseprite)
    {
        var byName = new Dictionary<string, AnimationClip>(System.StringComparer.OrdinalIgnoreCase);
        foreach (AnimationClip clip in clips) byName[clip.name] = clip;

        foreach (ChildAnimatorState s in controller.layers.SelectMany(l => l.stateMachine.states))
        {
            if (byName.TryGetValue(s.state.name, out AnimationClip clip))
            {
                if (s.state.motion != clip)
                {
                    s.state.motion = clip;
                    EditorUtility.SetDirty(s.state);
                }
            }
            else if (s.state.motion != null)
                Debug.LogWarning($"[PropArtConverter] {controller.name}: state '{s.state.name}' has no tag of that " +
                                 $"name in {aseprite}; it keeps its old clip.", controller);
        }
        EditorUtility.SetDirty(controller);
    }
}

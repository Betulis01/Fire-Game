using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Wires the skeleton's extra art layers (Skeleton.aseprite) into the Enemy -- each
// gets a child SpriteRenderer beside Body and a matching sprite track in every Enemy
// Animator state's clip, copied from the importer's own clip for that state's tag
// (the hand-made clips only key Body and Head):
//   - "Ground Shadow": the drawn shadow under the feet, sorted just under Body
//   - "Highlight": the melee-target outline (TargetHighlight), drawn around the
//     body and sorted behind it (it sits under Shadow and Body in Aseprite); a
//     hidden layer there, so the file imports hidden layers
// Runs by itself once when anything is missing, and from Tools/Skeleton/Set Up
// Extra Layers.
//
// The importer names tracks after the Aseprite layer. When the child and layer names
// match, the track paths match, so AsepriteTagClipSync keeps the track on the right
// frames after art edits like Body and Head. The shadow layer may still be called
// "Shadow"; until it's renamed "Ground Shadow", rerun the menu item after editing frames.
static class SkeletonLayerTracks
{
    const string PrefabPath = "Assets/Prefabs/Entities/Enemy/Enemy.prefab";
    const string ControllerPath = "Assets/Animation/Enemy/Enemy Animator Controller.controller";
    const string AsepritePath = "Assets/Sprites/Enemy/Skeleton.aseprite";

    // child: the renderer's name and track path; layerNames: the Aseprite layer it
    // shows, first match wins; orderOffset: sorting order relative to Body.
    readonly struct ExtraLayer
    {
        public readonly string child;
        public readonly string[] layerNames;
        public readonly int orderOffset;

        public ExtraLayer(string child, string[] layerNames, int orderOffset)
        {
            this.child = child;
            this.layerNames = layerNames;
            this.orderOffset = orderOffset;
        }
    }

    static readonly ExtraLayer[] Layers =
    {
        new("Ground Shadow", new[] { "Ground Shadow", "Shadow" }, -1),
        new(TargetHighlight.LayerName, new[] { TargetHighlight.LayerName }, -2),  // under Ground Shadow, as in Aseprite
    };

    [InitializeOnLoadMethod]
    static void AutoSetUp() => EditorApplication.delayCall += () =>
    {
        if (NeedsSetUp()) SetUp();
    };

    static bool NeedsSetUp()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (prefab == null || controller == null) return false;

        AnimationClip[] generated = GeneratedClips();
        foreach (ExtraLayer layer in Layers)
        {
            bool art = generated.Any(g => LayerPath(g, layer) != null);
            if (!art) continue;   // not imported (yet): nothing to wire
            if (prefab.transform.Find(layer.child) == null) return true;
            if (StateClips(controller).Any(sc =>
                    !AnimationUtility.GetObjectReferenceCurveBindings(sc.clip).Any(b => b.path == layer.child) &&
                    LayerPath(generated.FirstOrDefault(g => g.name == sc.tag), layer) != null))
                return true;
        }
        return false;
    }

    [MenuItem("Tools/Skeleton/Set Up Extra Layers")]
    static void SetUp()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return;

        AnimationClip[] generated = GeneratedClips();
        foreach (ExtraLayer layer in Layers)
        {
            if (!generated.Any(g => LayerPath(g, layer) != null))
            {
                Debug.LogWarning($"[SkeletonLayerTracks] No '{string.Join("' / '", layer.layerNames)}' layer " +
                                 $"imported from {AsepritePath}; skipping '{layer.child}'.");
                continue;
            }

            AddChild(layer);

            int tracks = 0;
            foreach ((AnimationClip clip, string tag) in StateClips(controller))
                if (CopyTrack(clip, generated.FirstOrDefault(g => g.name == tag), layer)) tracks++;
            Debug.Log($"[SkeletonLayerTracks] '{layer.child}' renderer on the Enemy; track in {tracks} clip(s).");
        }

        AssetDatabase.SaveAssets();
    }

    // A SpriteRenderer beside Body, sorted orderOffset from it.
    static void AddChild(in ExtraLayer layer)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (root.transform.Find(layer.child) != null) return;
            Transform body = root.transform.Find("Body");
            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();

            var go = new GameObject(layer.child) { layer = body.gameObject.layer };
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = body.localPosition;
            go.transform.SetSiblingIndex(layer.orderOffset < 0 ? body.GetSiblingIndex() : body.GetSiblingIndex() + 1);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = bodyRenderer.sharedMaterial;
            sr.sortingLayerID = bodyRenderer.sortingLayerID;
            sr.sortingOrder = bodyRenderer.sortingOrder + layer.orderOffset;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Replace the clip's curves for the child with the generated clip's layer curves
    // (sprite keys, and the importer's on/off keys for frames with no cel), retimed
    // if the hand-made clip's length differs.
    static bool CopyTrack(AnimationClip clip, AnimationClip generated, in ExtraLayer layer)
    {
        string from = LayerPath(generated, layer);
        if (from == null) return false;
        string child = layer.child;
        float scale = generated.length > 0f ? clip.length / generated.length : 1f;

        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(b => b.path == child))
            AnimationUtility.SetObjectReferenceCurve(clip, b, null);
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == child))
            AnimationUtility.SetEditorCurve(clip, b, null);

        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(generated).Where(b => b.path == from))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(generated, b)
                .Select(k => new ObjectReferenceKeyframe { time = k.time * scale, value = k.value })
                .ToArray();
            EditorCurveBinding to = b;
            to.path = child;
            AnimationUtility.SetObjectReferenceCurve(clip, to, keys);
        }
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(generated).Where(b => b.path == from))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(generated, b);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++) keys[i].time *= scale;
            curve.keys = keys;
            EditorCurveBinding to = b;
            to.path = child;
            AnimationUtility.SetEditorCurve(clip, to, curve);
        }

        EditorUtility.SetDirty(clip);
        return true;
    }

    // The generated clip's track path for the layer, or null when it has none.
    static string LayerPath(AnimationClip generated, in ExtraLayer layer)
    {
        if (generated == null) return null;
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(generated);
        return layer.layerNames.FirstOrDefault(n => bindings.Any(b => b.path == n && b.propertyName == "m_Sprite"));
    }

    static AnimationClip[] GeneratedClips() =>
        AssetDatabase.LoadAllAssetsAtPath(AsepritePath).OfType<AnimationClip>().ToArray();

    // Each distinct hand-made clip with the state (= Aseprite tag) name it plays under.
    static IEnumerable<(AnimationClip clip, string tag)> StateClips(AnimatorController controller)
    {
        var seen = new HashSet<AnimationClip>();
        foreach (ChildAnimatorState s in controller.layers.SelectMany(l => l.stateMachine.states))
            if (s.state.motion is AnimationClip clip && seen.Add(clip))
                yield return (clip, s.state.name);
    }
}

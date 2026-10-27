using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Wires the skeleton's ground shadow art layer (Skeleton.aseprite) into the Enemy: a
// "Ground Shadow" child SpriteRenderer drawn just under Body, and a matching sprite
// track in every Enemy Animator state's clip, copied from the importer's own clip for
// that state's tag (the hand-made clips only key Body and Head). Runs by itself once
// when either is missing, and from Tools/Skeleton/Set Up Ground Shadow.
//
// The importer names tracks after the Aseprite layer: while the layer is still called
// "Shadow" its track is taken from there. Renaming the layer to "Ground Shadow" makes
// the track paths match, so AsepriteTagClipSync keeps it on the right frames after
// art edits like Body and Head (until then, rerun the menu item after editing frames).
static class SkeletonGroundShadow
{
    const string PrefabPath = "Assets/Prefabs/Entities/Enemy/Enemy.prefab";
    const string ControllerPath = "Assets/Animation/Enemy/Enemy Animator Controller.controller";
    const string AsepritePath = "Assets/Sprites/Enemy/Skeleton.aseprite";
    const string Child = "Ground Shadow";
    static readonly string[] LayerNames = { "Ground Shadow", "Shadow" };

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
        if (prefab.transform.Find(Child) == null) return true;

        AnimationClip[] generated = GeneratedClips();
        return StateClips(controller).Any(sc =>
            !AnimationUtility.GetObjectReferenceCurveBindings(sc.clip).Any(b => b.path == Child) &&
            ShadowLayer(generated.FirstOrDefault(g => g.name == sc.tag)) != null);
    }

    [MenuItem("Tools/Skeleton/Set Up Ground Shadow")]
    static void SetUp()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return;

        AddChild();

        AnimationClip[] generated = GeneratedClips();
        int tracks = 0;
        foreach ((AnimationClip clip, string tag) in StateClips(controller))
            if (CopyTrack(clip, generated.FirstOrDefault(g => g.name == tag))) tracks++;

        AssetDatabase.SaveAssets();
        Debug.Log($"[SkeletonGroundShadow] '{Child}' renderer on the Enemy; shadow track in {tracks} clip(s).");
    }

    // A "Ground Shadow" SpriteRenderer beside Body, sorted just under it.
    static void AddChild()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (root.transform.Find(Child) != null) return;
            Transform body = root.transform.Find("Body");
            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();

            var go = new GameObject(Child) { layer = body.gameObject.layer };
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = body.localPosition;
            go.transform.SetSiblingIndex(body.GetSiblingIndex());

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = bodyRenderer.sharedMaterial;
            sr.sortingLayerID = bodyRenderer.sortingLayerID;
            sr.sortingOrder = bodyRenderer.sortingOrder - 1;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Replace the clip's Ground Shadow curves with the generated clip's shadow-layer
    // curves (sprite keys, and the importer's on/off keys for frames with no shadow
    // cel), retimed if the hand-made clip's length differs.
    static bool CopyTrack(AnimationClip clip, AnimationClip generated)
    {
        string layer = ShadowLayer(generated);
        if (layer == null) return false;
        float scale = generated.length > 0f ? clip.length / generated.length : 1f;

        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(clip).Where(b => b.path == Child))
            AnimationUtility.SetObjectReferenceCurve(clip, b, null);
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip).Where(b => b.path == Child))
            AnimationUtility.SetEditorCurve(clip, b, null);

        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(generated).Where(b => b.path == layer))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(generated, b)
                .Select(k => new ObjectReferenceKeyframe { time = k.time * scale, value = k.value })
                .ToArray();
            EditorCurveBinding to = b;
            to.path = Child;
            AnimationUtility.SetObjectReferenceCurve(clip, to, keys);
        }
        foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(generated).Where(b => b.path == layer))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(generated, b);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++) keys[i].time *= scale;
            curve.keys = keys;
            EditorCurveBinding to = b;
            to.path = Child;
            AnimationUtility.SetEditorCurve(clip, to, curve);
        }

        EditorUtility.SetDirty(clip);
        return true;
    }

    // The generated clip's shadow layer path, or null when it has none.
    static string ShadowLayer(AnimationClip generated)
    {
        if (generated == null) return null;
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(generated);
        return LayerNames.FirstOrDefault(n => bindings.Any(b => b.path == n && b.propertyName == "m_Sprite"));
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

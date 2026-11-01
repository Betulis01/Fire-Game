using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Keys the player's BackRig (where BackStow crosses the stowed weapons) in every
// player clip, so the X on the back moves with the body art. For each frame of the
// clip's body sprite curve (SpriteRenderer.m_Sprite on the root) the rig sits at that
// sprite's top-centre: x = its bounds' centre (the torso's lean), y = its top edge,
// relative to the feet pivot. The keys are stepped, like the sprite frames.
//
// Tools/Player/Bake Back Rig overwrites BackRig's position curves in every clip in
// ClipFolder; re-run it whenever Simple Pete's frames or the clips' sprite keys change.
static class PlayerBackRigBaker
{
    const string ClipFolder = "Assets/Animation/Player";
    const string RigPath = "BackRig";

    [MenuItem("Tools/Player/Bake Back Rig")]
    static void Bake()
    {
        var keyed = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipFolder }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (clip != null && BakeClip(clip)) keyed.Add(clip.name);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[PlayerBackRigBaker] Keyed {RigPath} in {keyed.Count} clips: {string.Join(", ", keyed)}");
    }

    static bool BakeClip(AnimationClip clip)
    {
        ObjectReferenceKeyframe[] frames = null;
        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            if (b.path == "" && b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite")
                frames = AnimationUtility.GetObjectReferenceCurve(clip, b);
        if (frames == null || frames.Length == 0) return false;

        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();
        foreach (ObjectReferenceKeyframe f in frames)
        {
            if (f.value is not Sprite sprite) continue;
            Bounds b = sprite.bounds;
            x.AddKey(Stepped(f.time, b.center.x));
            y.AddKey(Stepped(f.time, b.max.y));
            z.AddKey(Stepped(f.time, 0f));
        }
        if (y.length == 0) return false;

        Set(clip, "m_LocalPosition.x", x);
        Set(clip, "m_LocalPosition.y", y);
        Set(clip, "m_LocalPosition.z", z);
        EditorUtility.SetDirty(clip);
        return true;
    }

    // constant until the next key: the rig jumps with each sprite frame
    static Keyframe Stepped(float time, float value) =>
        new(time, value, float.PositiveInfinity, float.PositiveInfinity);

    static void Set(AnimationClip clip, string property, AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(RigPath, typeof(Transform), property), curve);
    }
}

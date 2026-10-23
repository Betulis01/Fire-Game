using UnityEditor;
using UnityEngine;

// Keys the hand anchors of the Whirlwind clip (Animation/Player/whirlwind.anim).
// The clip's Body/Head frames (Simple Pete's "whirlwind" tag, 6 frames x 0.1s) turn
// the body one full circle counter-clockwise starting from SE: SE, E, NE, NW, W,
// SW. This orbits the hands around the hips in step with that turn -- the weapon
// anchor (RightHand) out in front along the facing, the other hand tucked in on
// the opposite side -- with the axe rotated to point outward and mirrored (scale
// x -1 on the weapon anchor) so its blade leads the counter-clockwise spin instead
// of the back of the head. Write-defaults resets that scale when the state ends.
// Flattened into an
// ellipse for the 3/4 view. Adds the OnWhirlwindHit event once per turn
// (WhirlwindSkill strikes on it) and makes the clip loop; WhirlwindSkill plays it
// `spins` times. A left-hand cast plays it mirrored (clockwise), so only the
// right-hand version is authored.
//
// Tools/Skills/Bake Whirlwind Clip overwrites the anchor curves and events, so
// re-run it after changing the numbers here rather than hand-editing those keys.
static class WhirlwindClipBaker
{
    const string ClipPath = "Assets/Animation/Player/whirlwind.anim";
    const string WeaponAnchor = "HandRig/RightHand";
    const string OffAnchor = "HandRig/LeftHand";

    const float TurnLength = 0.6f;   // one full turn: the 6 body frames at 0.1s
    const float StartAngle = -45f;   // the first body frame faces SE
    const int KeysPerTurn = 12;

    // Hand-rig local space. Centre.y must match HeldItemSorter.spinCentreY, which
    // puts the axe behind the body on the far half of the circle.
    static readonly Vector2 Centre = new(0.015f, -0.55f);
    const float RadiusX = 0.25f;      // 8 body px: the hand stays at the hip, the axe does the reaching
    const float RadiusY = 0.1f;       // 3/4 view squash
    const float OffHandReach = 0.5f;  // the free hand circles closer to the body

    const float HitTime = 0.3f;       // mid-turn: the axe sweeps the far side

    [MenuItem("Tools/Skills/Bake Whirlwind Clip")]
    static void Bake()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null) { Debug.LogError($"[WhirlwindClipBaker] Missing {ClipPath}"); return; }

        KeyAnchor(clip, WeaponAnchor, 0f, 1f);
        KeyAnchor(clip, OffAnchor, 180f, OffHandReach);

        // The axe art has its blade on its right (+x) when pointing up; turned to
        // point outward that faces backwards along a counter-clockwise spin. Mirror
        // it across the handle so the blade faces the way it's travelling.
        KeyScale(clip, WeaponAnchor, new Vector3(-1f, 1f, 1f));

        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            new AnimationEvent { time = HitTime, functionName = nameof(WhirlwindSkill.OnWhirlwindHit) },
        });

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.stopTime = TurnLength;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
        Debug.Log($"[WhirlwindClipBaker] Baked hand keys and hit event into {ClipPath}");
    }

    // One anchor's orbit: `phase` degrees ahead of the facing, at `reach` x the radius.
    // Angles are keyed unwrapped (-135 .. 225) so the rotation spins rather than
    // snapping back halfway round.
    static void KeyAnchor(AnimationClip clip, string path, float phase, float reach)
    {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var rot = new AnimationCurve();

        for (int i = 0; i <= KeysPerTurn; i++)
        {
            float t = TurnLength * i / KeysPerTurn;
            float angle = StartAngle + phase + 360f * i / KeysPerTurn;
            float a = angle * Mathf.Deg2Rad;
            x.AddKey(t, Centre.x + RadiusX * reach * Mathf.Cos(a));
            y.AddKey(t, Centre.y + RadiusY * reach * Mathf.Sin(a));
            rot.AddKey(t, angle - 90f);   // the axe art points up: turn it to point outward
        }

        Linear(x);
        Linear(y);
        Linear(rot);
        AnimationCurve zero = AnimationCurve.Constant(0f, TurnLength, 0f);

        Set(clip, path, "m_LocalPosition.x", x);
        Set(clip, path, "m_LocalPosition.y", y);
        Set(clip, path, "m_LocalPosition.z", zero);
        Set(clip, path, "localEulerAnglesRaw.x", zero);
        Set(clip, path, "localEulerAnglesRaw.y", zero);
        Set(clip, path, "localEulerAnglesRaw.z", rot);
    }

    // A constant scale for the whole turn.
    static void KeyScale(AnimationClip clip, string path, Vector3 scale)
    {
        Set(clip, path, "m_LocalScale.x", AnimationCurve.Constant(0f, TurnLength, scale.x));
        Set(clip, path, "m_LocalScale.y", AnimationCurve.Constant(0f, TurnLength, scale.y));
        Set(clip, path, "m_LocalScale.z", AnimationCurve.Constant(0f, TurnLength, scale.z));
    }

    static void Set(AnimationClip clip, string path, string property, AnimationCurve curve) =>
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);

    static void Linear(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }
}

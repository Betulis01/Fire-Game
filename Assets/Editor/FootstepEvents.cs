using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// OnFootstep Animation Events on every run / walk clip's contact frames, for
// FootstepAudio. The table below is the one place the frames live:
//   - Tools/Audio/Bake Footstep Events writes them onto the clips (the player's and the
//     skeleton's are hand-made clips; the wolf's, boar's and deer's too, until their next
//     rebuild)
//   - EnemyArtSetup asks With() for the clips it regenerates, so a rebuild keeps them
// Writing replaces only OnFootstep events; every other event on a clip (the attacks')
// is kept. Frames are sprite-key indices from the clip's first key, measured from the
// art: the two frames of each cycle with the feet spread widest (the contacts). Tune
// them here and bake again.
public static class FootstepEvents
{
    public const string Function = nameof(FootstepAudio.OnFootstep);

    static readonly Dictionary<string, int[]> Frames = new()
    {
        ["Assets/Animation/Player/se_run.anim"] = new[] { 0, 4 },
        ["Assets/Animation/Player/ne_run.anim"] = new[] { 2, 6 },
        ["Assets/Animation/Enemy/se_run.anim"] = new[] { 1, 5 },
        ["Assets/Animation/Enemy/ne_run.anim"] = new[] { 2, 6 },
        ["Assets/Animation/Wolf/se_run.anim"] = new[] { 2, 6 },
        ["Assets/Animation/Wolf/ne_run.anim"] = new[] { 2, 6 },
        ["Assets/Animation/Boar/se_run.anim"] = new[] { 0, 2 },
        ["Assets/Animation/Boar/ne_run.anim"] = new[] { 1, 3 },
        ["Assets/Animation/Deer/se_run.anim"] = new[] { 2, 7 },
        ["Assets/Animation/Deer/ne_run.anim"] = new[] { 1, 6 },
        ["Assets/Animation/Deer/se_walk.anim"] = new[] { 1, 6 },
        ["Assets/Animation/Deer/ne_walk.anim"] = new[] { 2, 7 },
    };

    [MenuItem("Tools/Audio/Bake Footstep Events")]
    public static void BakeAll()
    {
        int baked = 0;
        foreach (string path in Frames.Keys)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { Debug.LogWarning($"[FootstepEvents] No clip at {path}."); continue; }
            AnimationUtility.SetAnimationEvents(clip, With(clip, path, AnimationUtility.GetAnimationEvents(clip)));
            EditorUtility.SetDirty(clip);
            baked++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FootstepEvents] Baked footsteps onto {baked} clips.");
    }

    // `others` minus any old footsteps, plus this clip's footsteps (if it has a row).
    public static AnimationEvent[] With(AnimationClip clip, string path, AnimationEvent[] others)
    {
        var events = (others ?? new AnimationEvent[0]).Where(e => e.functionName != Function).ToList();
        if (Frames.TryGetValue(path, out int[] frames))
        {
            ObjectReferenceKeyframe[] keys = SpriteKeys(clip);
            foreach (int f in frames)
            {
                if (keys == null || f >= keys.Length)
                {
                    Debug.LogError($"[FootstepEvents] {path} has {keys?.Length ?? 0} sprite keys; frame {f} is out of range. " +
                                   "Update its row in FootstepEvents.cs.", clip);
                    continue;
                }
                events.Add(new AnimationEvent { time = keys[f].time, functionName = Function });
            }
        }
        return events.OrderBy(e => e.time).ToArray();
    }

    static ObjectReferenceKeyframe[] SpriteKeys(AnimationClip clip)
    {
        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            if (b.propertyName == "m_Sprite") return AnimationUtility.GetObjectReferenceCurve(clip, b);
        return null;
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Keeps hand-made Animator clips that play an Aseprite tag pointed at the right
// frames. The Aseprite importer names merged-frame sprites by frame number
// ("Frame_N") and their IDs follow the name, so inserting or deleting a frame in
// Aseprite silently shifts every clip keyed after it by that many frames (e.g. a
// new unlit frame 0 on the campfire made "ember" start on the unlit frame).
//
// For every Animator state whose clip keys root sprites from an .aseprite file that
// has a tag with the state's name, key i is rewritten to the tag's frame i (extra
// hold key keeps its last frame), taken from the importer's own clip for that tag.
// Only clips with one key per tag frame are touched, so hand-ordered ones survive.
// Files imported as separate layers (the player's paper doll) have one curve per
// layer and are left to GearSpriteSync. Runs after any .aseprite import and from
// Tools/Aseprite/Sync Tag Clips.
class AsepriteTagClipSync : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Any(p => p.EndsWith(".aseprite")))
            EditorApplication.delayCall += Sync;
    }

    [MenuItem("Tools/Aseprite/Sync Tag Clips")]
    static void Sync()
    {
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var tagCache = new Dictionary<(string, string), Sprite[]>();
        int changed = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController"))
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
            if (controller == null) continue;

            foreach (AnimatorState state in controller.layers.SelectMany(l => AllStates(l.stateMachine)))
            {
                if (state.motion is not AnimationClip clip) continue;

                ObjectReferenceKeyframe[] kf = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (kf == null || kf.Length == 0) continue;

                // the source file: any key whose sprite still resolves
                string source = kf.Select(k => AssetDatabase.GetAssetPath(k.value))
                                  .FirstOrDefault(p => p != null && p.EndsWith(".aseprite"));
                if (source == null) continue;

                if (!tagCache.TryGetValue((source, state.name), out Sprite[] frames))
                    tagCache[(source, state.name)] = frames = TagFrames(source, state.name);
                if (frames == null || frames.Length == 0) continue;

                // one key per tag frame (+ an optional hold key on the end); anything
                // else (ping-pong, reordered or skipped frames) was keyed by hand on purpose
                if (kf.Length != frames.Length && kf.Length != frames.Length + 1) continue;

                bool dirty = false;
                for (int i = 0; i < kf.Length; i++)
                {
                    Sprite want = frames[Mathf.Min(i, frames.Length - 1)];
                    if (kf[i].value == want) continue;
                    kf[i].value = want;
                    dirty = true;
                }
                if (!dirty) continue;

                AnimationUtility.SetObjectReferenceCurve(clip, binding, kf);
                EditorUtility.SetDirty(clip);
                changed++;
                Debug.Log($"[AsepriteTagClipSync] Re-pointed '{clip.name}' (state '{state.name}') to tag frames of {source}.", clip);
            }
        }

        if (changed > 0) AssetDatabase.SaveAssets();
    }

    // The tag's sprites in frame order, from the importer's generated clip of that
    // name; null when there's no such tag or the file is imported per layer.
    static Sprite[] TagFrames(string asepritePath, string tag)
    {
        AnimationClip gen = AssetDatabase.LoadAllAssetsAtPath(asepritePath)
                                         .OfType<AnimationClip>()
                                         .FirstOrDefault(c => c.name == tag);
        if (gen == null) return null;

        EditorCurveBinding[] sprites = AnimationUtility.GetObjectReferenceCurveBindings(gen)
                                                       .Where(b => b.propertyName == "m_Sprite")
                                                       .ToArray();
        if (sprites.Length != 1) return null;

        return AnimationUtility.GetObjectReferenceCurve(gen, sprites[0])
                               .OrderBy(k => k.time)
                               .Select(k => k.value as Sprite)
                               .ToArray();
    }

    static IEnumerable<AnimatorState> AllStates(AnimatorStateMachine sm) =>
        sm.states.Select(s => s.state).Concat(sm.stateMachines.SelectMany(c => AllStates(c.stateMachine)));
}

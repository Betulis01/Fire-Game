using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Keeps hand-made Animator clips that play an Aseprite tag pointed at the right
// frames. The Aseprite importer names sprites by frame number ("Frame_N", or
// "<Layer>_Frame_N" for files imported as separate layers) and the stored sprite
// references follow those names, so inserting or deleting a frame in Aseprite
// silently shifts or breaks every clip keyed after it -- e.g. a new unlit frame 0
// made the campfire's "ember" start on the unlit frame, and trimming the skeleton's
// die frames left its die clips' Head keys pointing at sprites that no longer exist.
//
// For every Animator state whose clip keys sprites from an .aseprite file that has a
// tag with the state's name, each sprite track's key i is rewritten to the tag's
// frame i (an extra hold key keeps its last frame), taken from the importer's own
// clip for that tag:
//   - single-layer files: the clip's root track <- the tag clip's only track
//   - layered files (the skeleton's Body / Head): each track <- the tag clip's track
//     on the same layer path
// The player's clips key the root while its file is layered, so they never match
// here and stay with GearSpriteSync. Only tracks with one key per tag frame are
// touched, so hand-ordered ones survive. Runs after any .aseprite import and from
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
        var tagCache = new Dictionary<(string, string), Dictionary<string, Sprite[]>>();
        var done = new HashSet<AnimationClip>();
        int changed = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController"))
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
            if (controller == null) continue;

            foreach (AnimatorState state in controller.layers.SelectMany(l => AllStates(l.stateMachine)))
            {
                if (state.motion is not AnimationClip clip || !done.Add(clip)) continue;

                EditorCurveBinding[] tracks = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                    .Where(b => b.propertyName == "m_Sprite" && b.type == typeof(SpriteRenderer))
                    .ToArray();
                if (tracks.Length == 0) continue;

                // the source file: any key on any track whose sprite still resolves
                // (a track whose keys all broke can't name it itself)
                string source = tracks
                    .SelectMany(t => AnimationUtility.GetObjectReferenceCurve(clip, t))
                    .Select(k => k.value != null ? AssetDatabase.GetAssetPath(k.value) : null)
                    .FirstOrDefault(p => p != null && p.EndsWith(".aseprite"));
                if (source == null) continue;

                if (!tagCache.TryGetValue((source, state.name), out var tag))
                    tagCache[(source, state.name)] = tag = TagTracks(source, state.name);
                if (tag == null) continue;

                bool clipChanged = false;
                foreach (EditorCurveBinding track in tracks)
                {
                    Sprite[] frames = FramesFor(track.path, tag);
                    if (frames == null || frames.Length == 0) continue;

                    ObjectReferenceKeyframe[] kf = AnimationUtility.GetObjectReferenceCurve(clip, track);

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

                    AnimationUtility.SetObjectReferenceCurve(clip, track, kf);
                    clipChanged = true;
                }
                if (!clipChanged) continue;

                EditorUtility.SetDirty(clip);
                changed++;
                Debug.Log($"[AsepriteTagClipSync] Re-pointed '{clip.name}' (state '{state.name}') to tag frames of {source}.", clip);
            }
        }

        if (changed > 0) AssetDatabase.SaveAssets();
    }

    // The tag clip's track for a hand-made clip's track path: the only track, for the
    // root of a single-layer file; otherwise the track on the same layer path.
    static Sprite[] FramesFor(string path, Dictionary<string, Sprite[]> tag)
    {
        if (tag.TryGetValue(path, out Sprite[] frames)) return frames;
        if (path == "" && tag.Count == 1) return tag.Values.First();
        return null;
    }

    // layer path -> the tag's sprites in frame order, from the importer's generated
    // clip of that name; null when the file has no such tag.
    static Dictionary<string, Sprite[]> TagTracks(string asepritePath, string tag)
    {
        AnimationClip gen = AssetDatabase.LoadAllAssetsAtPath(asepritePath)
                                         .OfType<AnimationClip>()
                                         .FirstOrDefault(c => c.name == tag);
        if (gen == null) return null;

        var tracks = new Dictionary<string, Sprite[]>();
        foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(gen))
        {
            if (b.propertyName != "m_Sprite") continue;
            tracks[b.path] = AnimationUtility.GetObjectReferenceCurve(gen, b)
                                             .OrderBy(k => k.time)
                                             .Select(k => k.value as Sprite)
                                             .ToArray();
        }
        return tracks;
    }

    static IEnumerable<AnimatorState> AllStates(AnimatorStateMachine sm) =>
        sm.states.Select(s => s.state).Concat(sm.stateMachines.SelectMany(c => AllStates(c.stateMachine)));
}

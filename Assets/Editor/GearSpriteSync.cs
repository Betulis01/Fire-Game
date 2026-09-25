using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Keeps paper-doll sprite references correct. Every Equippable and every
// SpriteLayerFollower with a layer name gets its frames[] re-resolved *by sprite
// name* ("<Layer>_Frame_N") from the paper-doll Aseprite file (the player's body,
// Simple Pete.aseprite -- one file holds body and all gear). Runs after that file
// is imported (the importer can reassign sprite IDs when cells are added or
// removed, which would silently point stored references at the wrong frames) and
// from Tools/Gear/Sync Layer Sprites.
//
// Also gives an Equippable with no world sprite its first layer frame as one, and
// errors on duplicate layer names inside one file (their sprites would share names,
// so body and gear could swap).
//
// Body clips get the same protection: each player Animator state is named after
// an Aseprite tag (se_idle, ne_attack_l, ...), and the importer's generated clip
// for that tag keys the Body layer's current sprites in order -- so the state's
// hand-made clip gets its root sprite keys rewritten from it. Hiding Body/Head and
// saving, or inserting frames, reassigns sprite IDs; this re-points the clips.
class GearSpriteSync : AssetPostprocessor
{
    const string PaperDollGuid = "332edefdd70e4fe4a9454bfb41b591e2";   // Simple Pete.aseprite
    static readonly Regex LayerFrame = new(@"^(.+)_Frame_(\d+)$");

    static string PaperDollPath => AssetDatabase.GUIDToAssetPath(PaperDollGuid);

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Contains(PaperDollPath))
            EditorApplication.delayCall += Sync;
    }

    [MenuItem("Tools/Gear/Sync Layer Sprites and Body Clips")]
    static void Sync()
    {
        Dictionary<string, Sprite[]> layers = IndexLayers();
        int updated = 0;

        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.EndsWith(".prefab")))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;
            bool changed = false;

            foreach (Equippable e in root.GetComponentsInChildren<Equippable>(true))
            {
                changed |= SyncFrames(e, e.layerName, layers, path);

                SpriteRenderer world = e.GetComponent<SpriteRenderer>();
                Sprite first = e.frames?.FirstOrDefault(s => s != null);
                if (world != null && world.sprite == null && first != null)
                {
                    world.sprite = first;
                    EditorUtility.SetDirty(world);
                    changed = true;
                }
            }

            foreach (SpriteLayerFollower f in root.GetComponentsInChildren<SpriteLayerFollower>(true))
                if (!string.IsNullOrEmpty(f.layerName))
                    changed |= SyncFrames(f, f.layerName, layers, path);

            if (changed)
            {
                EditorUtility.SetDirty(root);
                updated++;
            }
        }

        int clips = SyncBodyClips();

        if (updated > 0 || clips > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[GearSpriteSync] Updated layer sprites in {updated} prefab(s) and body keys in {clips} clip(s).");
        }
    }

    // Rewrite the root sprite keys of every paper-doll Animator state's clip from the
    // generated clip of the tag with the same name. Key i shows the tag's frame i;
    // extra keys (a clip's final hold key) keep the tag's last frame. Returns the
    // number of clips changed.
    static int SyncBodyClips()
    {
        // tag name -> the Body layer's sprites in frame order
        var tagFrames = new Dictionary<string, Sprite[]>();
        foreach (AnimationClip gen in AssetDatabase.LoadAllAssetsAtPath(PaperDollPath).OfType<AnimationClip>())
            foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(gen))
                if (b.propertyName == "m_Sprite" && (b.path == "Body" || b.path.EndsWith("/Body")))
                    tagFrames[gen.name] = AnimationUtility.GetObjectReferenceCurve(gen, b)
                        .OrderBy(k => k.time).Select(k => k.value as Sprite).ToArray();
        if (tagFrames.Count == 0) return 0;

        var controllers = new HashSet<AnimatorController>();
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.EndsWith(".prefab")))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;
            foreach (PaperDoll doll in root.GetComponentsInChildren<PaperDoll>(true))
                if (doll.TryGetComponent(out Animator anim) && anim.runtimeAnimatorController is AnimatorController c)
                    controllers.Add(c);
        }

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        int changedClips = 0;

        foreach (AnimatorController controller in controllers)
            foreach (AnimatorState state in controller.layers.SelectMany(l => AllStates(l.stateMachine)))
            {
                if (state.motion is not AnimationClip clip) continue;
                if (!tagFrames.TryGetValue(state.name, out Sprite[] frames) || frames.Length == 0) continue;

                ObjectReferenceKeyframe[] kf = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (kf == null || kf.Length == 0) continue;

                bool changed = false;
                for (int i = 0; i < kf.Length; i++)
                {
                    Sprite want = frames[Mathf.Min(i, frames.Length - 1)];
                    if (kf[i].value == want) continue;
                    kf[i].value = want;
                    changed = true;
                }
                if (!changed) continue;

                AnimationUtility.SetObjectReferenceCurve(clip, binding, kf);
                EditorUtility.SetDirty(clip);
                changedClips++;
                Debug.Log($"[GearSpriteSync] Re-pointed body keys of '{clip.name}' (state '{state.name}').", clip);
            }

        return changedClips;
    }

    static IEnumerable<AnimatorState> AllStates(AnimatorStateMachine sm) =>
        sm.states.Select(s => s.state).Concat(sm.stateMachines.SelectMany(c => AllStates(c.stateMachine)));

    // Write `frames` on a component via SerializedObject; true if anything changed.
    static bool SyncFrames(Component c, string layerName, Dictionary<string, Sprite[]> layers, string path)
    {
        if (string.IsNullOrEmpty(layerName)) return false;
        if (!layers.TryGetValue(layerName, out Sprite[] wanted))
        {
            Debug.LogError($"[GearSpriteSync] {path}: no layer '{layerName}' in {PaperDollPath} " +
                           "(is it visible, including its groups, when saved?).", c);
            return false;
        }

        var so = new SerializedObject(c);
        SerializedProperty frames = so.FindProperty("frames");
        bool same = frames.arraySize == wanted.Length;
        for (int i = 0; same && i < wanted.Length; i++)
            same = frames.GetArrayElementAtIndex(i).objectReferenceValue == wanted[i];
        if (same) return false;

        frames.arraySize = wanted.Length;
        for (int i = 0; i < wanted.Length; i++)
            frames.GetArrayElementAtIndex(i).objectReferenceValue = wanted[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // layer name -> sprites indexed by frame, from the paper-doll file.
    static Dictionary<string, Sprite[]> IndexLayers()
    {
        var byLayer = new Dictionary<string, Dictionary<int, Sprite>>();
        var seen = new HashSet<string>();

        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(PaperDollPath).OfType<Sprite>())
        {
            Match m = LayerFrame.Match(s.name);
            if (!m.Success) continue;

            if (!seen.Add(s.name))
                Debug.LogError($"[GearSpriteSync] {PaperDollPath}: two sprites named '{s.name}' -- " +
                               $"layer '{m.Groups[1].Value}' exists twice. Layer names must be unique file-wide.");

            if (!byLayer.TryGetValue(m.Groups[1].Value, out var frames))
                byLayer[m.Groups[1].Value] = frames = new Dictionary<int, Sprite>();
            frames[int.Parse(m.Groups[2].Value)] = s;
        }

        var result = new Dictionary<string, Sprite[]>();
        foreach (var (layer, frames) in byLayer.Select(kv => (kv.Key, kv.Value)))
        {
            var arr = new Sprite[frames.Keys.Max() + 1];
            foreach (var kv in frames) arr[kv.Key] = kv.Value;
            result[layer] = arr;
        }
        return result;
    }
}

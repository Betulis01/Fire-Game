using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off migration for switching the player body (Simple Pete.aseprite) from
// Merge Frame to Individual Layers import without breaking what references its
// sprites. Merged sprites are named "Frame_N"; in Individual Layers mode the body
// layer's are "<Layer>_Frame_N" with new sprite IDs, so every sprite key and
// SpriteRenderer pointing at the old ones would go missing.
//
//   1 Record  (still in Merge Frame): note which frame number every reference uses.
//   -- switch the importer to Individual Layers, Apply --
//   2 Rebind: point each reference at the body layer's sprite for that frame.
//
// Covers every .anim clip in the project and SpriteRenderers in the open scenes
// (keep Camp open for both steps). Delete this file once the migration is done.
static class BodyImportMigration
{
    const string BodyGuid = "332edefdd70e4fe4a9454bfb41b591e2";   // Simple Pete.aseprite
    const string BodyLayer = "Stick";
    const string RecordPath = "Library/BodyImportMigration.json";

    static readonly Regex FrameName = new(@"(?:^|_)Frame_(\d+)$");

    [System.Serializable]
    class ClipRecord
    {
        public string clipPath;
        public string path;
        public string typeName;
        public string propertyName;
        public int[] frames;   // per keyframe; -1 = not a body sprite, left alone
    }

    [System.Serializable]
    class RendererRecord
    {
        public string globalId;
        public int frame;
    }

    [System.Serializable]
    class Record
    {
        public List<ClipRecord> clips = new();
        public List<RendererRecord> renderers = new();
    }

    static string BodyPath => AssetDatabase.GUIDToAssetPath(BodyGuid);

    static int FrameOf(Object o)
    {
        if (o is not Sprite s || AssetDatabase.GetAssetPath(s) != BodyPath) return -1;
        Match m = FrameName.Match(s.name);
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }

    static IEnumerable<(string path, AnimationClip clip)> Clips() =>
        AssetDatabase.FindAssets("t:AnimationClip")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".anim"))
            .Select(p => (p, AssetDatabase.LoadAssetAtPath<AnimationClip>(p)))
            .Where(x => x.Item2 != null);

    static IEnumerable<SpriteRenderer> SceneRenderers() =>
        Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    [MenuItem("Tools/Body Migration/1 Record")]
    static void RecordReferences()
    {
        var record = new Record();
        int keys = 0;

        foreach ((string clipPath, AnimationClip clip) in Clips())
        {
            foreach (EditorCurveBinding b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                ObjectReferenceKeyframe[] kf = AnimationUtility.GetObjectReferenceCurve(clip, b);
                int[] frames = kf.Select(k => FrameOf(k.value)).ToArray();
                if (frames.All(f => f < 0)) continue;

                record.clips.Add(new ClipRecord
                {
                    clipPath = clipPath, path = b.path, typeName = b.type.AssemblyQualifiedName,
                    propertyName = b.propertyName, frames = frames,
                });
                keys += frames.Count(f => f >= 0);
            }
        }

        foreach (SpriteRenderer sr in SceneRenderers())
        {
            int frame = FrameOf(sr.sprite);
            if (frame < 0) continue;
            record.renderers.Add(new RendererRecord
            {
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(sr).ToString(), frame = frame,
            });
        }

        File.WriteAllText(RecordPath, JsonUtility.ToJson(record, true));
        Debug.Log($"[BodyMigration] Recorded {keys} sprite keys in {record.clips.Select(c => c.clipPath).Distinct().Count()} clips " +
                  $"and {record.renderers.Count} scene SpriteRenderers -> {RecordPath}. " +
                  "Now switch Simple Pete to Individual Layers, Apply, then run 2 Rebind.");
    }

    [MenuItem("Tools/Body Migration/2 Rebind")]
    static void Rebind()
    {
        if (!File.Exists(RecordPath)) { Debug.LogError("[BodyMigration] Run 1 Record first."); return; }
        Record record = JsonUtility.FromJson<Record>(File.ReadAllText(RecordPath));

        // Body layer sprites by frame number ("Stick_Frame_N").
        var byFrame = new Dictionary<int, Sprite>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Sprite>())
        {
            if (!s.name.StartsWith(BodyLayer + "_Frame_")) continue;
            byFrame[int.Parse(FrameName.Match(s.name).Groups[1].Value)] = s;
        }
        if (byFrame.Count == 0)
        {
            string names = string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Sprite>().Select(s => s.name).Take(10));
            Debug.LogError($"[BodyMigration] No '{BodyLayer}_Frame_N' sprites found — is the importer on Individual Layers? First sprites: {names}");
            return;
        }

        int rebound = 0;
        var missing = new SortedSet<int>();

        foreach (ClipRecord c in record.clips)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(c.clipPath);
            var binding = new EditorCurveBinding
            {
                path = c.path, type = System.Type.GetType(c.typeName), propertyName = c.propertyName,
            };
            ObjectReferenceKeyframe[] kf = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (kf == null || kf.Length != c.frames.Length)
            {
                Debug.LogError($"[BodyMigration] {c.clipPath} '{c.propertyName}' changed since Record — skipped.");
                continue;
            }

            for (int i = 0; i < kf.Length; i++)
            {
                if (c.frames[i] < 0) continue;
                if (byFrame.TryGetValue(c.frames[i], out Sprite s)) { kf[i].value = s; rebound++; }
                else missing.Add(c.frames[i]);
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, kf);
            EditorUtility.SetDirty(clip);
        }

        int renderers = 0;
        foreach (RendererRecord r in record.renderers)
        {
            if (!GlobalObjectId.TryParse(r.globalId, out GlobalObjectId id)) continue;
            if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is not SpriteRenderer sr)
            {
                Debug.LogError($"[BodyMigration] SpriteRenderer {r.globalId} not found — is its scene open?");
                continue;
            }
            if (!byFrame.TryGetValue(r.frame, out Sprite s)) { missing.Add(r.frame); continue; }
            Undo.RecordObject(sr, "Body migration rebind");
            sr.sprite = s;
            EditorSceneManager.MarkSceneDirty(sr.gameObject.scene);
            renderers++;
        }

        AssetDatabase.SaveAssets();
        if (missing.Count > 0)
            Debug.LogError($"[BodyMigration] No '{BodyLayer}_Frame_N' sprite for frames: {string.Join(", ", missing)}");
        Debug.Log($"[BodyMigration] Rebound {rebound} clip keys and {renderers} scene SpriteRenderers " +
                  $"({missing.Count} frames missing). Save the scene.");
    }
}

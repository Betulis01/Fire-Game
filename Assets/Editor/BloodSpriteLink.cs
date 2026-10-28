using System.Linq;
using UnityEditor;
using UnityEngine;

// Points the blood effects at their Aseprite frames, found *by sprite name*
// ("Frame_N"): the importer gives every frame sprite a random id, so a reference to
// one can't be written by hand and goes stale if the file's frames change. Runs
// after either blood file is imported and from Tools/Effects/Link Blood Sprites.
//
//   Ground Blood Splatter   -> BloodSplatter.prefab (sprite + GroundSplatter variations)
//   Hit Blood Splatter (2, 3) -> HitBloodSplatter.prefab (sprite, from the first) and
//                              one clip per variant (one key a frame); the prefab's
//                              RandomAnimatorState picks a variant per hit
class BloodSpriteLink : AssetPostprocessor
{
    const string Art = "Assets/Sprites/Effects/Blood/";
    const string Dir = "Assets/Prefabs/Effects/Hit Effects/Blood/";
    const string GroundArt = Art + "Ground Blood Splatter.aseprite";
    const string GroundPrefab = Dir + "BloodSplatter.prefab";
    const string HitPrefab = Dir + "HitBloodSplatter.prefab";
    const float FrameTime = 0.1f;   // the Aseprite files' frame duration

    // hit spray variant: its art and the clip that plays it
    static readonly (string art, string clip)[] HitVariants =
    {
        (Art + "Hit Blood Splatter.aseprite", Dir + "HitBloodSplatter_Clip.anim"),
        (Art + "Hit Blood Splatter 2.aseprite", Dir + "HitBloodSplatter 2_Clip.anim"),
        (Art + "Hit Blood Splatter 3.aseprite", Dir + "HitBloodSplatter 3_Clip.anim"),
    };

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Contains(GroundArt) || HitVariants.Any(v => imported.Contains(v.art)))
            EditorApplication.delayCall += Link;
    }

    [MenuItem("Tools/Effects/Link Blood Sprites")]
    static void Link()
    {
        Sprite[] ground = Frames(GroundArt);

        if (ground.Length > 0)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(GroundPrefab);
            if (root != null)
            {
                SetSprite(root, ground[0]);
                var so = new SerializedObject(root.GetComponent<GroundSplatter>());
                SerializedProperty variations = so.FindProperty("variations");
                variations.arraySize = ground.Length;
                for (int i = 0; i < ground.Length; i++)
                    variations.GetArrayElementAtIndex(i).objectReferenceValue = ground[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(root);
            }
        }

        int hitVariants = 0;
        foreach ((string art, string clipPath) in HitVariants)
        {
            Sprite[] hit = Frames(art);
            if (hit.Length == 0) continue;
            hitVariants++;

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(HitPrefab);
            if (root != null && clipPath == HitVariants[0].clip)
            {
                SetSprite(root, hit[0]);
                EditorUtility.SetDirty(root);
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip != null)
            {
                // a key per frame, plus a hold key so the last frame gets its full time
                var keys = new ObjectReferenceKeyframe[hit.Length + 1];
                for (int i = 0; i < hit.Length; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i * FrameTime, value = hit[i] };
                keys[hit.Length] = new ObjectReferenceKeyframe { time = hit.Length * FrameTime - 0.01f, value = hit[^1] };
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                EditorUtility.SetDirty(clip);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[BloodSpriteLink] Linked {ground.Length} ground blood frame(s) and {hitVariants} hit spray variant(s).");
    }

    static void SetSprite(GameObject root, Sprite sprite)
    {
        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        EditorUtility.SetDirty(sr);
    }

    // The file's frame sprites in frame order (Frame_0, Frame_1, ...).
    static Sprite[] Frames(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name[(s.name.LastIndexOf('_') + 1)..], out int n) ? n : 0)
            .ToArray();
}

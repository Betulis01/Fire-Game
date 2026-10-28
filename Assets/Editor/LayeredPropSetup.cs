using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Keeps props whose Aseprite art imports as Individual Layers (+ hidden layers) wired
// up, so each art layer -- including the hidden "Highlight" the melee target outline
// uses -- is its own sprite:
//   - the root SpriteRenderer shows the base layer (the body); its hand-made clips
//     key only the root, so each state's root keys are rewritten to the base layer's
//     frames of the Aseprite tag with the state's name (hit, collide, ...)
//   - every other layer is a child named after it with a SpriteLayerFollower, which
//     shows that layer's sprite for whatever frame the root is on, sorted by the
//     layer's place in the Aseprite stack relative to the base (Highlight is drawn
//     under the body, so it sorts behind it)
// Keeping the root as the body means YSort, held-item sorting, OccluderFade,
// SpriteShadow and Highlightable all keep working on one main renderer.
//
// Runs after any listed file imports (the importer reassigns sprite IDs, which would
// leave the root's sprite and clip keys pointing at nothing), and from Tools/Aseprite/
// Set Up Layered Props. Idempotent. To convert another prop: switch its file to
// Individual Layers + Import Hidden Layers and add it to Props.
static class LayeredPropSetup
{
    readonly struct Prop
    {
        public readonly string aseprite;
        public readonly string baseLayer;    // the layer the root renderer shows
        public readonly string[] prefabs;

        public Prop(string aseprite, string baseLayer, params string[] prefabs)
        {
            this.aseprite = aseprite;
            this.baseLayer = baseLayer;
            this.prefabs = prefabs;
        }
    }

    static readonly Prop[] Props =
    {
        new("Assets/Sprites/Ground/Ores/Stone/Stone Ore.aseprite", "Layer 1",
            "Assets/Prefabs/Entities/Ground/Stone Ore.prefab"),
        new("Assets/Sprites/Ground/Wood/Wood.aseprite", "Wood",
            "Assets/Prefabs/Entities/Ground/Scots Pine Wood.prefab", "Assets/Scots Pine Wood.prefab"),
        new("Assets/Sprites/Ground/Wood/Grey Alder Wood.aseprite", "Wood",
            "Assets/Prefabs/Entities/Ground/Grey Alder Wood.prefab"),
        new("Assets/Sprites/Ground/Tree/Scots Pine/Scots_Pine.aseprite", "Root",
            "Assets/Prefabs/Entities/Ground/Scots Pine.prefab"),
        new("Assets/Sprites/Ground/Tree/Grey Alder/Grey Alder.aseprite", "Root",
            "Assets/Prefabs/Entities/Ground/Grey Alder.prefab"),
    };

    static readonly Regex LayerFrame = new(@"^(.+)_Frame_(\d+)$");

    class AutoRun : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Any(p => Props.Any(prop => prop.aseprite == p)))
                EditorApplication.delayCall += () => SetUpAll(false);
        }
    }

    [MenuItem("Tools/Aseprite/Set Up Layered Props")]
    static void SetUpFromMenu() => SetUpAll(true);

    static void SetUpAll(bool verbose)
    {
        int changed = 0;
        foreach (Prop prop in Props) changed += SetUp(prop, verbose);
        if (changed > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[LayeredPropSetup] Updated {changed} prefab(s)/clip(s).");
        }
    }

    static int SetUp(in Prop prop, bool verbose)
    {
        Dictionary<string, Sprite[]> layers = IndexLayers(prop.aseprite);
        if (!layers.TryGetValue(prop.baseLayer, out Sprite[] baseFrames) || baseFrames.Length == 0 || baseFrames[0] == null)
        {
            if (verbose)
                Debug.LogError($"[LayeredPropSetup] {prop.aseprite}: no '{prop.baseLayer}_Frame_0' sprite. Is the file " +
                               "imported as Individual Layers (with Import Hidden Layers for Highlight)?");
            return 0;
        }

        ReadHeader(prop.aseprite, out List<string> stack, out var tags);   // stack: bottom to top
        int baseIndex = stack.IndexOf(prop.baseLayer);

        int changed = 0;
        foreach (string path in prop.prefabs)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            if (SetUpPrefab(path, prop, layers, stack, baseIndex)) changed++;
            changed += SetUpClips(path, prop, baseFrames, tags, verbose);
        }
        return changed;
    }

    // Root shows the base layer; one follower child per other layer.
    static bool SetUpPrefab(string path, in Prop prop, Dictionary<string, Sprite[]> layers, List<string> stack, int baseIndex)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        bool dirty = false;
        try
        {
            SpriteRenderer body = root.GetComponent<SpriteRenderer>();
            Sprite first = layers[prop.baseLayer][0];
            if (body.sprite != first)
            {
                body.sprite = first;
                dirty = true;
            }

            // A one-sprite RandomSprite pins the old merged frame; layered art has no
            // per-variant root sprite to pick.
            if (root.TryGetComponent(out RandomSprite random))
            {
                Object.DestroyImmediate(random, true);
                dirty = true;
            }

            // The outline variants pointed at merged frames; the Highlight follower
            // now supplies the outline for every frame.
            if (root.TryGetComponent(out Highlightable highlightable))
            {
                var so = new SerializedObject(highlightable);
                SerializedProperty variants = so.FindProperty("variants");
                if (variants.arraySize > 0)
                {
                    variants.arraySize = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    dirty = true;
                }
            }

            foreach ((string layer, Sprite[] frames) in layers.Select(kv => (kv.Key, kv.Value)))
            {
                if (layer == prop.baseLayer) continue;
                int index = stack.IndexOf(layer);
                int offset = index >= 0 && baseIndex >= 0 ? index - baseIndex : 1;
                dirty |= SetUpLayerChild(root, body, layer, frames, offset);
            }

            if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return dirty;
    }

    static bool SetUpLayerChild(GameObject root, SpriteRenderer body, string layer, Sprite[] frames, int orderOffset)
    {
        bool dirty = false;
        Transform child = root.transform.Find(layer);
        if (child == null)
        {
            child = new GameObject(layer) { layer = root.layer }.transform;
            child.SetParent(root.transform, false);
            dirty = true;
        }
        if (child.localPosition != Vector3.zero)
        {
            child.localPosition = Vector3.zero;
            dirty = true;
        }

        if (!child.TryGetComponent(out SpriteRenderer sr))
        {
            sr = child.gameObject.AddComponent<SpriteRenderer>();
            dirty = true;
        }
        if (sr.sharedMaterial != body.sharedMaterial || sr.sortingLayerID != body.sortingLayerID)
        {
            sr.sharedMaterial = body.sharedMaterial;
            sr.sortingLayerID = body.sortingLayerID;
            dirty = true;
        }

        // The follower owns this layer's sorting; a YSort here would fight it.
        if (child.TryGetComponent(out YSort ySort))
        {
            Object.DestroyImmediate(ySort, true);
            dirty = true;
        }

        if (!child.TryGetComponent(out SpriteLayerFollower follower))
        {
            follower = child.gameObject.AddComponent<SpriteLayerFollower>();
            dirty = true;
        }

        var so = new SerializedObject(follower);
        dirty |= Set(so.FindProperty("source"), body);
        SerializedProperty name = so.FindProperty("layerName");
        if (name.stringValue != layer) { name.stringValue = layer; dirty = true; }
        SerializedProperty order = so.FindProperty("orderOffset");
        if (order.intValue != orderOffset) { order.intValue = orderOffset; dirty = true; }
        SerializedProperty list = so.FindProperty("frames");
        if (list.arraySize != frames.Length) { list.arraySize = frames.Length; dirty = true; }
        for (int i = 0; i < frames.Length; i++)
            dirty |= Set(list.GetArrayElementAtIndex(i), frames[i]);
        so.ApplyModifiedPropertiesWithoutUndo();
        return dirty;
    }

    static bool Set(SerializedProperty p, Object value)
    {
        if (p.objectReferenceValue == value) return false;
        p.objectReferenceValue = value;
        return true;
    }

    // Rewrite each state clip's root sprite keys from the Aseprite tag with the state's
    // name: key i shows the base layer's sprite for the tag's frame i, extra keys (a
    // final hold) keep its last frame. Tag ranges are read from the file itself, so
    // this doesn't depend on how the importer names its generated clips' tracks.
    // Returns the clips changed.
    static int SetUpClips(string prefabPath, in Prop prop, Sprite[] baseFrames, Dictionary<string, (int from, int to)> tags, bool verbose)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (!prefab.TryGetComponent(out Animator anim) || anim.runtimeAnimatorController is not AnimatorController controller)
            return 0;

        var rootSprite = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        int changed = 0;

        foreach (ChildAnimatorState s in controller.layers.SelectMany(l => l.stateMachine.states))
        {
            if (s.state.motion is not AnimationClip clip) continue;
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, rootSprite);
            if (keys == null || keys.Length == 0) continue;

            if (!tags.TryGetValue(s.state.name, out var range))
            {
                if (verbose)
                    Debug.LogWarning($"[LayeredPropSetup] {prefabPath}: state '{s.state.name}' keys the root sprite " +
                                     $"but {prop.aseprite} has no tag of that name; its keys were left alone.", clip);
                continue;
            }

            bool dirty = false;
            for (int i = 0; i < keys.Length; i++)
            {
                int frame = Mathf.Min(range.from + i, range.to);
                Sprite w = frame < baseFrames.Length ? baseFrames[frame] : null;
                if (w == null || keys[i].value == w) continue;
                keys[i].value = w;
                dirty = true;
            }
            if (!dirty) continue;

            AnimationUtility.SetObjectReferenceCurve(clip, rootSprite, keys);
            EditorUtility.SetDirty(clip);
            changed++;
        }
        return changed;
    }

    // layer name -> sprites indexed by frame ("<Layer>_Frame_N").
    static Dictionary<string, Sprite[]> IndexLayers(string aseprite)
    {
        var byLayer = new Dictionary<string, Dictionary<int, Sprite>>();
        foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(aseprite).OfType<Sprite>())
        {
            Match m = LayerFrame.Match(s.name);
            if (!m.Success) continue;
            if (!byLayer.TryGetValue(m.Groups[1].Value, out var frames))
                byLayer[m.Groups[1].Value] = frames = new Dictionary<int, Sprite>();
            frames[int.Parse(m.Groups[2].Value)] = s;
        }

        var result = new Dictionary<string, Sprite[]>();
        foreach (var kv in byLayer)
        {
            var arr = new Sprite[kv.Value.Keys.Max() + 1];
            foreach (var f in kv.Value) arr[f.Key] = f.Value;
            result[kv.Key] = arr;
        }
        return result;
    }

    // The file's layer names bottom to top and its tags (name -> frame range), read
    // from the first frame's chunks. Aseprite format: 128-byte header, then frames of
    // chunks; 0x2004 = layer (name at +16 of its data), 0x2018 = tags (count, 8
    // reserved bytes, then per tag: from, to, direction, repeat, 6 reserved, RGB,
    // 1 extra, name).
    static void ReadHeader(string aseprite, out List<string> layers, out Dictionary<string, (int from, int to)> tags)
    {
        layers = new List<string>();
        tags = new Dictionary<string, (int, int)>();
        byte[] d = File.ReadAllBytes(aseprite);
        int frame = 128;
        int chunks = System.BitConverter.ToUInt16(d, frame + 6);
        uint newChunks = System.BitConverter.ToUInt32(d, frame + 12);
        if (newChunks != 0) chunks = (int)newChunks;

        int p = frame + 16;
        for (int c = 0; c < chunks; c++)
        {
            int size = (int)System.BitConverter.ToUInt32(d, p);
            int type = System.BitConverter.ToUInt16(d, p + 4);
            if (type == 0x2004)
            {
                layers.Add(ReadString(d, p + 22));
            }
            else if (type == 0x2018)
            {
                int count = System.BitConverter.ToUInt16(d, p + 6);
                int q = p + 16;
                for (int t = 0; t < count; t++)
                {
                    int from = System.BitConverter.ToUInt16(d, q);
                    int to = System.BitConverter.ToUInt16(d, q + 2);
                    string name = ReadString(d, q + 17);
                    tags[name] = (from, to);
                    q += 19 + System.BitConverter.ToUInt16(d, q + 17);
                }
            }
            p += size;
        }
    }

    // Aseprite STRING: WORD length, then UTF-8 bytes.
    static string ReadString(byte[] d, int at) =>
        System.Text.Encoding.UTF8.GetString(d, at + 2, System.BitConverter.ToUInt16(d, at));
}

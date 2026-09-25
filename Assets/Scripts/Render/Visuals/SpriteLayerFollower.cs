using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// Paper-doll layer: shows this layer's sprite for whatever frame the source (body)
// renderer is on. The body's clips only animate the body sprite ("Body_Frame_N");
// this renderer follows with its own "<Layer>_Frame_N" -- e.g. the Head layer now,
// gear slots later. Also mirrors the source's flipX and material (so the white
// hit flash covers it too), and sorts a fixed offset above the source.
//
// Sprites from the same Aseprite file share the canvas-space pivot, so placing
// this at the source's local origin lines every layer up.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLayerFollower : MonoBehaviour
{
    [Tooltip("Renderer to follow. Falls back to the parent's SpriteRenderer.")]
    [SerializeField] SpriteRenderer source;

    [Tooltip("This layer's sprites, indexed by frame number. Fill with the " +
             "context-menu 'Fill Frames From Layer'.")]
    [SerializeField] Sprite[] frames;

    [Tooltip("Sorting order relative to the source.")]
    [SerializeField] int orderOffset = 1;

#if UNITY_EDITOR
    [Tooltip("Aseprite layer name to fill frames from (editor only).")]
    [SerializeField] string layerName = "Head";
#endif

    static readonly Regex FrameSuffix = new(@"_Frame_(\d+)$");
    static readonly Dictionary<Sprite, int> frameOf = new();   // shared parse cache

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (source == null && transform.parent != null)
            source = transform.parent.GetComponent<SpriteRenderer>();
    }

    // After the Animator has written this frame's body sprite.
    void LateUpdate()
    {
        if (source == null) return;

        int frame = FrameOf(source.sprite);
        sr.sprite = frame >= 0 && frame < frames.Length ? frames[frame] : null;
        sr.flipX = source.flipX;
        sr.sharedMaterial = source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder + orderOffset;
    }

    public static int FrameOf(Sprite s)
    {
        if (s == null) return -1;
        if (!frameOf.TryGetValue(s, out int frame))
        {
            Match m = FrameSuffix.Match(s.name);
            frame = m.Success ? int.Parse(m.Groups[1].Value) : -1;
            frameOf[s] = frame;
        }
        return frame;
    }

#if UNITY_EDITOR
    // Collect "<layerName>_Frame_N" from the source sprite's Aseprite file into
    // frames[N]. Frames where the layer is empty stay null (nothing drawn).
    [ContextMenu("Fill Frames From Layer")]
    void FillFrames()
    {
        SpriteRenderer src = source != null ? source
            : transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
        if (src == null || src.sprite == null)
        {
            Debug.LogError("[SpriteLayerFollower] Needs a source renderer with a sprite assigned.", this);
            return;
        }

        string path = UnityEditor.AssetDatabase.GetAssetPath(src.sprite);
        var found = new Dictionary<int, Sprite>();
        foreach (Object o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s && s.name.StartsWith(layerName + "_Frame_"))
                found[FrameOf(s)] = s;

        if (found.Count == 0)
        {
            Debug.LogError($"[SpriteLayerFollower] No '{layerName}_Frame_N' sprites in {path}.", this);
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Fill layer frames");
        int max = 0;
        foreach (int f in found.Keys) max = Mathf.Max(max, f);
        frames = new Sprite[max + 1];
        foreach (var kv in found) frames[kv.Key] = kv.Value;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[SpriteLayerFollower] Filled {found.Count} '{layerName}' frames from {path}.", this);
    }
#endif
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

// Paper-doll layer: shows this layer's sprite for whatever frame the source (body)
// renderer is on. The body's clips only animate the body sprite ("Body_Frame_N");
// this renderer follows with its own "<Layer>_Frame_N" -- the Head layer, and the
// gear slots PaperDoll creates. Also mirrors the source's flipX and material (so the
// white hit flash covers it too), and sorts a fixed offset above the source.
//
// Sprites from the same Aseprite file share the canvas-space pivot, so placing
// this at the source's local origin lines every layer up.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLayerFollower : MonoBehaviour
{
    [Tooltip("Renderer to follow. Falls back to the parent's SpriteRenderer.")]
    [SerializeField] SpriteRenderer source;

    [Tooltip("Aseprite layer whose frames fill `frames` (GearSpriteSync, editor). " +
             "Leave empty for a renderer whose frames are set in code (gear slots).")]
    public string layerName;

    [Tooltip("This layer's sprites, indexed by frame number. Filled automatically.")]
    [SerializeField] Sprite[] frames;

    [Tooltip("Sorting order relative to the source.")]
    [SerializeField] int orderOffset = 1;

    static readonly Regex FrameSuffix = new(@"_Frame_(\d+)$");
    static readonly Dictionary<Sprite, int> frameOf = new();   // shared parse cache

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (source == null && transform.parent != null)
            source = transform.parent.GetComponent<SpriteRenderer>();
    }

    // Swap what this layer draws (null/empty = nothing), e.g. equipping gear.
    public void SetFrames(Sprite[] newFrames) => frames = newFrames;
    public void SetOrderOffset(int offset) => orderOffset = offset;

    // After the Animator has written this frame's body sprite.
    void LateUpdate()
    {
        if (source == null) return;

        int frame = FrameOf(source.sprite);
        sr.sprite = frames != null && frame >= 0 && frame < frames.Length ? frames[frame] : null;
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
}

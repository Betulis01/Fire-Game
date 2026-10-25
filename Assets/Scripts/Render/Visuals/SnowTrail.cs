using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Leaves a continuous snow trench behind an entity (the player now; enemies later):
// every `spacing` art pixels it moves, it stamps the trail's three layers into the
// shared SnowTrailCanvas, which paints them once and merges them into one trench.
// Each stamp picks one of the variants at random (snow-trail, snow-trail2, ...), so
// the trench's edge doesn't repeat; the canvas merge keeps the trench itself whole.
// Stamps are laid along the whole path moved since the last one, so a fast move
// (lunge, knockback) leaves no gaps. CanStamp is the hook for "only on snow tiles".
// The variants mirror the .aseprite files in Sprites/Effects/Snow Walking
// (SnowTrailImport rebuilds the list whenever one is added, changed or removed).
public class SnowTrail : MonoBehaviour
{
    // One stamp: the three layers of a snow-trail*.aseprite file.
    [Serializable]
    public class StampVariant
    {
        public Sprite floor;   // Buttom: the trench floor, drawn over everything
        public Sprite wall;    // Middle: the inner wall ring
        public Sprite rim;     // Top: the snow pushed up around it, outside only
    }

    [SerializeField] StampVariant[] variants = Array.Empty<StampVariant>();

    [Tooltip("Art pixels moved between stamps. Smaller than the floor (8 px) keeps the trench continuous.")]
    [SerializeField] int spacing = 6 ;

    [Tooltip("Where the trench is pressed. Falls back to this transform (the player's pivot is at the feet).")]
    [SerializeField] Transform feet;

    [Tooltip("A jump farther than this (teleport, respawn) restarts the trail instead of drawing a line.")]
    [SerializeField] float maxGap = 3f;

    SnowStamp[] stamps = Array.Empty<SnowStamp>();
    Vector2 last;
    bool started;

    void Start()
    {
        var built = new List<SnowStamp>();
        foreach (StampVariant v in variants)
        {
            if (v != null && v.floor != null && v.wall != null && v.rim != null)
                built.Add(SnowStamp.For(v.floor, v.wall, v.rim));
            else
                Debug.LogWarning("[SnowTrail] A stamp variant is missing its floor, wall or rim sprite; skipped.", this);
        }
        stamps = built.ToArray();
        if (stamps.Length == 0) Debug.LogWarning("[SnowTrail] No complete stamp variants; no trail.", this);
    }

    void LateUpdate()
    {
        if (stamps.Length == 0) return;

        Vector2 pos = feet != null ? feet.position : transform.position;
        if (!started)
        {
            started = true;
            last = pos;
            return;
        }

        Vector2 delta = pos - last;
        float distance = delta.magnitude;
        if (distance > maxGap) { last = pos; return; }

        float step = (float)spacing / SnowTrailCanvas.PixelsPerUnit;
        if (distance < step) return;

        Vector2 dir = delta / distance;
        int count = Mathf.FloorToInt(distance / step);
        for (int i = 1; i <= count; i++)
        {
            Vector2 at = last + dir * (step * i);
            if (CanStamp(at)) SnowTrailCanvas.Instance.Stamp(at, stamps[Random.Range(0, stamps.Length)]);
        }
        last += dir * (step * count);
    }

    // Whether the ground at `worldPos` takes a trench (later: only snow tiles).
    protected virtual bool CanStamp(Vector2 worldPos) => true;
}

// A trail stamp's pixels, read once from its three layer sprites and shared by
// every emitter using the same sprites. Bands are painted rim -> wall -> floor with
// ranks 1 -> 3 (see SnowTrailCanvas). Offsets place each band relative to the stamp
// point using the sprite's pivot, so the three layers line up as in Aseprite.
public class SnowStamp
{
    public class Band
    {
        public Color32[] pixels;
        public int width, height;
        public int offsetX, offsetY;   // band's bottom-left relative to the stamp point
        public byte rank;
    }

    public readonly Band[] Bands;

    static readonly Dictionary<(Sprite, Sprite, Sprite), SnowStamp> cache = new();

    SnowStamp(Sprite floor, Sprite wall, Sprite rim)
    {
        Bands = new[] { Read(rim, 1), Read(wall, 2), Read(floor, 3) };
    }

    public static SnowStamp For(Sprite floor, Sprite wall, Sprite rim)
    {
        if (!cache.TryGetValue((floor, wall, rim), out SnowStamp stamp))
            cache[(floor, wall, rim)] = stamp = new SnowStamp(floor, wall, rim);
        return stamp;
    }

    // The sprite's packed pixels. Imported Aseprite textures aren't CPU-readable, so
    // unless they are, copy through a temporary RenderTexture (exact at 1:1 size).
    static Band Read(Sprite sprite, byte rank)
    {
        Rect r = sprite.textureRect;
        int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
        int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y);
        Texture2D source = sprite.texture;

        Color32[] pixels;
        if (source.isReadable)
        {
            Color[] c = source.GetPixels(x, y, w, h);
            pixels = new Color32[c.Length];
            for (int i = 0; i < c.Length; i++) pixels[i] = c[i];
        }
        else
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0,
                                                          RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            copy.ReadPixels(new Rect(x, y, w, h), 0, 0);
            copy.Apply(false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            pixels = copy.GetPixels32();
            UnityEngine.Object.Destroy(copy);
        }

        // sprite.pivot is in sprite-rect pixels; textureRectOffset is where the packed
        // (trimmed) pixels start inside that rect
        Vector2 offset = sprite.textureRectOffset - sprite.pivot;
        return new Band
        {
            pixels = pixels,
            width = w,
            height = h,
            offsetX = Mathf.RoundToInt(offset.x),
            offsetY = Mathf.RoundToInt(offset.y),
            rank = rank,
        };
    }
}

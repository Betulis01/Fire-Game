using System.Collections.Generic;
using UnityEngine;

// A fake drop shadow, the cheap stand-in for a ShadowCaster2D: copies of this
// entity's sprites (the body and its paper-doll layers) drawn flat, semi-transparent
// black on the ground and sheared away from a light -- one shadow per ShadowLight
// reaching the entity (up to maxShadows, strongest first), so standing between two
// fires casts two.
//
// Each shadow is the sprite lying down: its height is laid along the ground pointing
// away from its light -- a full sprite height right by the light, shrinking toward
// the light's edge -- and its width runs across that on the isometric ground (so a
// shadow cast east or west is as wide as one cast north or south, just foreshortened
// by groundAspect). A Transform can't skew, so that 2x2 map is split into
// rotate -> scale -> rotate across two nested transforms, which Unity composes
// exactly. Shadows get their own root objects instead of living under the entity, so
// they sort on the ground (Background) rather than inside the entity's SortingGroup.
//
// A shadow is a pooled "slot" bound to one light: it fades in when its light starts
// reaching the entity, follows it, and fades out (then frees) when the light goes
// out, leaves, or drops out of the strongest few. Its darkness follows the light's
// strength at the feet. Lights parented under the entity (a held torch) are ignored.
// Where shadows overlap, the Sprites/Shadow stencil lets the first drawn win, so the
// strongest light's shadow sorts first and the overlap reads as one darkness.
//
// Two per-object fixes for shapes the plain sprite doesn't shadow well:
//   castHeight   only the art pixels below this height above the pivot cast (a cropped
//                copy of the sprite, cached per sprite) -- a tree's crown sits above
//                the firelight and would project away from the ground near it, so only
//                its trunk shadows.
//   anchorOffset where the shadow starts, in art pixels from the pivot -- a rock's
//                pivot is the front tip of its V-shaped base, so laying the shadow from
//                there leaves wedges unshadowed beside it; starting from the middle of
//                its footprint fills them (the part under the rock is hidden by it).
//
// Lights come from ShadowLight's registry (no scene scans), and an entity that's
// off-screen -- neither its body nor any of its shadows visible to a camera -- skips
// its update entirely, leaving its shadows as they were.
[DefaultExecutionOrder(1000)]   // after the Animator and SpriteLayerFollowers set this frame's sprites
public class SpriteShadow : MonoBehaviour
{
    [Tooltip("Sprites/Shadow material (draws each shadow pixel once).")]
    [SerializeField] Material material;

    [Tooltip("Sprites the shadow copies (e.g. the skeleton's Body and Head). Empty = this " +
             "object's SpriteRenderer plus its SpriteLayerFollowers (the player's paper doll).")]
    [SerializeField] SpriteRenderer[] layers;

    [Tooltip("Shadow darkness at full light.")]
    [Range(0f, 1f)] [SerializeField] float opacity = 0.45f;

    [Tooltip("Shadow length, in sprite heights, right next to the light.")]
    [SerializeField] float nearLength = 1f;
    [Tooltip("Shadow length, in sprite heights, at the edge of the light's radius.")]
    [SerializeField] float farLength = 0.3f;

    [Tooltip("Vertical squash of the isometric ground (2:1 iso = 0.5), used to lay the " +
             "shadow's width across its direction on the ground.")]
    [SerializeField] float groundAspect = 0.5f;

    [Tooltip("Only art pixels up to this height above the pivot cast a shadow (a tree's " +
             "bare trunk, below its crown). 0 = the whole sprite.")]
    [Min(0)] [SerializeField] int castHeight;

    [Tooltip("Where the shadow starts, in art pixels from the pivot -- e.g. up into the " +
             "middle of a rock's footprint, so its V-shaped base has no gaps beside it.")]
    [SerializeField] Vector2 anchorOffset;

    [Tooltip("Most shadows at once -- one per light, strongest lights first.")]
    [Min(1)] [SerializeField] int maxShadows = 3;

    [Tooltip("How quickly a shadow swings and fades toward its light (per second).")]
    [SerializeField] float smoothing = 10f;

    [SerializeField] string sortingLayer = "Background";
    [Tooltip("Above the ground tilemap (0), the snow trail (1) and ground splatters (2). " +
             "The strongest shadow sorts here, the next one above it, and so on.")]
    [SerializeField] int sortingOrder = 3;

    const float Invisible = 0.005f;   // alpha below which a shadow is hidden (and a fading one freed)
    const float DefaultPixelsPerUnit = 32f;

    // castHeight crops, shared by every shadow: (sprite, height) -> cropped copy
    // (the sprite itself when it's no taller, null when nothing is left)
    static readonly Dictionary<(Sprite, int), Sprite> cropped = new();

    // One shadow: a root -> skew pair carrying a renderer per sprite layer.
    class Slot
    {
        public Transform root, skew;
        public SpriteRenderer[] renderers;   // parallel to `sources`
        public bool inUse;
        public ShadowLight light;
        public bool wanted;                  // among this frame's strongest lights
        public int rank;                     // sort position; strongest = 0
        public Vector2 shear, targetShear;   // the sprite's up axis on the ground
        public float alpha, targetAlpha;
    }

    readonly List<SpriteRenderer> sources = new();
    readonly List<Slot> slots = new();
    Vector3[] offsets;

    // this frame's strongest lights, strongest first
    ShadowLight[] top;
    float[] topStrength, topReach;
    int topCount;

    void Start()
    {
        if (layers != null && layers.Length > 0)
        {
            foreach (SpriteRenderer layer in layers)
                if (layer != null) sources.Add(layer);
        }
        else
        {
            if (TryGetComponent(out SpriteRenderer body)) sources.Add(body);
            foreach (SpriteLayerFollower f in GetComponentsInChildren<SpriteLayerFollower>(true))
            {
                SpriteRenderer sr = f.GetComponent<SpriteRenderer>();
                if (!f.shadowLayer && !TargetHighlight.IsHighlight(sr)) sources.Add(sr);   // a target outline casts nothing
            }
        }
        offsets = new Vector3[sources.Count];

        top = new ShadowLight[maxShadows];
        topStrength = new float[maxShadows];
        topReach = new float[maxShadows];
    }

    void LateUpdate()
    {
        if (!Visible()) return;

        Vector2 feet = transform.TransformPoint(anchorOffset / PixelsPerUnit());
        GatherLights(feet);

        foreach (Slot s in slots) s.wanted = false;
        for (int i = 0; i < topCount; i++)
        {
            ShadowLight l = top[i];
            Vector2 away = feet - (Vector2)l.transform.position;
            float dist = away.magnitude;
            Vector2 dir = dist > 0.0001f ? away / dist : Vector2.down;
            Vector2 shear = dir * Mathf.Lerp(farLength, nearLength, topReach[i]);

            Slot s = SlotFor(l) ?? Claim(l, shear);
            if (s == null) continue;
            s.wanted = true;
            s.rank = i;
            s.targetShear = shear;
            s.targetAlpha = opacity * Mathf.Clamp01(topStrength[i]);
        }

        for (int j = 0; j < sources.Count; j++)
            if (sources[j] != null) offsets[j] = transform.InverseTransformPoint(sources[j].transform.position);

        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        foreach (Slot s in slots)
        {
            if (!s.inUse) continue;
            if (!s.wanted)
            {
                s.targetAlpha = 0f;
                s.rank = top.Length;   // fading shadows sort last
            }
            s.shear = Vector2.Lerp(s.shear, s.targetShear, k);
            s.alpha = Mathf.Lerp(s.alpha, s.targetAlpha, k);

            if (!s.wanted && s.alpha < Invisible) Free(s);
            else Draw(s, feet);
        }
    }

    // The strongest few ShadowLights reaching the feet, into top[0..topCount),
    // strongest first -- an insertion into a fixed array, no allocation.
    void GatherLights(Vector2 feet)
    {
        topCount = 0;
        int cap = top.Length;
        foreach (ShadowLight l in ShadowLight.All)
        {
            if (l.transform.IsChildOf(transform)) continue;

            float reach = l.Reach(feet);
            float strength = l.Intensity * reach;
            if (strength <= 0f) continue;
            if (topCount == cap && strength <= topStrength[cap - 1]) continue;

            int i = topCount < cap ? topCount++ : cap - 1;   // full: the weakest drops off the end
            for (; i > 0 && topStrength[i - 1] < strength; i--)
            {
                top[i] = top[i - 1];
                topStrength[i] = topStrength[i - 1];
                topReach[i] = topReach[i - 1];
            }
            top[i] = l;
            topStrength[i] = strength;
            topReach[i] = reach;
        }
    }

    // Worth updating: the body or one of its shadows is on a camera (Game or Scene
    // view). Checking the shadows too keeps one that pokes on-screen before the body
    // does up to date.
    bool Visible()
    {
        foreach (SpriteRenderer src in sources)
            if (src != null && src.isVisible) return true;
        foreach (Slot s in slots)
        {
            if (!s.inUse) continue;
            foreach (SpriteRenderer sr in s.renderers)
                if (sr.enabled && sr.isVisible) return true;
        }
        return false;
    }

    Slot SlotFor(ShadowLight l)
    {
        foreach (Slot s in slots)
            if (s.inUse && ReferenceEquals(s.light, l)) return s;
        return null;
    }

    // A slot for a light that just started casting: a free one, a new one, or -- with
    // the pool full of fading shadows -- the faintest of those. It starts invisible at
    // its target angle and fades in.
    Slot Claim(ShadowLight l, Vector2 shear)
    {
        Slot s = null;
        foreach (Slot x in slots)
            if (!x.inUse) { s = x; break; }
        if (s == null && slots.Count < top.Length * 2) s = NewSlot();
        if (s == null)
        {
            foreach (Slot x in slots)
                if (!x.wanted && (s == null || x.alpha < s.alpha)) s = x;
            if (s == null) return null;
        }

        s.inUse = true;
        s.light = l;
        s.shear = s.targetShear = shear;
        s.alpha = 0f;
        return s;
    }

    Slot NewSlot()
    {
        var s = new Slot
        {
            root = new GameObject($"{name} Shadow").transform,
            renderers = new SpriteRenderer[sources.Count],
        };
        s.skew = new GameObject("Skew").transform;
        s.skew.SetParent(s.root, false);

        for (int j = 0; j < sources.Count; j++)
        {
            var sr = new GameObject(sources[j] != null ? sources[j].name : "Layer").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(s.skew, false);
            sr.sharedMaterial = material;
            sr.sortingLayerName = sortingLayer;
            sr.enabled = false;
            s.renderers[j] = sr;
        }
        slots.Add(s);
        return s;
    }

    void Free(Slot s)
    {
        s.inUse = false;
        s.light = null;
        foreach (SpriteRenderer sr in s.renderers) sr.enabled = false;
    }

    void Draw(Slot s, Vector2 feet)
    {
        bool show = s.alpha >= Invisible;
        if (show) Place(s, feet);

        var tint = new Color(0f, 0f, 0f, s.alpha);
        for (int j = 0; j < sources.Count; j++)
        {
            SpriteRenderer src = sources[j], sr = s.renderers[j];
            bool on = show && src != null && src.enabled && src.gameObject.activeInHierarchy;
            sr.enabled = on;
            if (!on) continue;

            sr.sprite = Cropped(src.sprite, castHeight);
            sr.flipX = src.flipX;
            sr.flipY = src.flipY;
            sr.color = tint;
            sr.sortingOrder = sortingOrder + s.rank;
            sr.transform.localPosition = offsets[j];
        }
    }

    // The art's pixel size, from the first sprite showing (the world art is 32 PPU).
    float PixelsPerUnit()
    {
        foreach (SpriteRenderer src in sources)
            if (src != null && src.sprite != null) return src.sprite.pixelsPerUnit;
        return DefaultPixelsPerUnit;
    }

    // The part of `sprite` up to `height` art pixels above its pivot, as a sprite with
    // the same pivot -- made once per (sprite, height) and shared by every shadow.
    static Sprite Cropped(Sprite sprite, int height)
    {
        if (sprite == null || height <= 0) return sprite;

        // a cached crop can be destroyed under us (a sprite made in an earlier play
        // session); only a real null means "nothing left to cast"
        if (cropped.TryGetValue((sprite, height), out Sprite crop) && (crop != null || ReferenceEquals(crop, null)))
            return crop;

        Rect tr = sprite.textureRect;   // the sprite's packed pixels on its texture
        Vector2 pivot = sprite.pivot - sprite.textureRectOffset;   // pivot within those pixels
        float top = Mathf.Min(tr.height, Mathf.Round(pivot.y + height));

        if (top <= 0f) crop = null;
        else if (top >= tr.height) crop = sprite;
        else
        {
            crop = Sprite.Create(sprite.texture, new Rect(tr.x, tr.y, tr.width, top),
                                 new Vector2(pivot.x / tr.width, pivot.y / top),
                                 sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            crop.name = $"{sprite.name} (shadow)";
        }
        cropped[(sprite, height)] = crop;
        return crop;
    }

    // Lay the sprite down: up becomes the slot's shear, and x becomes the ground
    // direction at a right angle to it (worked out on the un-squashed ground, then
    // squashed back), so north/south keeps x as x and east/west turns it into a short
    // vertical span. M = [width | shear] is decomposed as R(phi) * Scale(sx, sy) *
    // R(theta) -- root, then skew.
    void Place(Slot s, Vector2 feet)
    {
        Vector2 shear = s.shear;
        var ground = new Vector2(shear.x, shear.y / groundAspect);
        Vector2 width = ground.sqrMagnitude > 1e-8f ? new Vector2(ground.y, -ground.x).normalized : Vector2.right;
        width.y *= groundAspect;

        float e = (width.x + shear.y) * 0.5f, f = (width.x - shear.y) * 0.5f;
        float g = (width.y + shear.x) * 0.5f, h = (width.y - shear.x) * 0.5f;
        float q = Mathf.Sqrt(e * e + h * h), r = Mathf.Sqrt(f * f + g * g);
        float a1 = Mathf.Atan2(g, f), a2 = Mathf.Atan2(h, e);

        s.root.SetPositionAndRotation(new Vector3(feet.x, feet.y, transform.position.z),
                                      Quaternion.Euler(0f, 0f, (a2 + a1) * 0.5f * Mathf.Rad2Deg));
        s.root.localScale = new Vector3(q + r, q - r, 1f);
        s.skew.localRotation = Quaternion.Euler(0f, 0f, (a2 - a1) * 0.5f * Mathf.Rad2Deg);
    }

    void OnEnable()
    {
        foreach (Slot s in slots) s.root.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        foreach (Slot s in slots)
            if (s.root != null) s.root.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        foreach (Slot s in slots)
            if (s.root != null) Destroy(s.root.gameObject);
    }
}

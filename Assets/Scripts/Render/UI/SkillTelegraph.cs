using System.Collections.Generic;
using UnityEngine;

// The ground telegraph of an aimed spell -- armed, or mid-cast while it still chooses
// its target (ActiveSkillSlots.TryGetAiming): its shape (ActiveSkill.TryGetAim) laid
// flat from the swing origin along the aim -- a cone's wedge, or a lane -- plus a ring
// under the target it would hit (TargetPreview's primary while it previews the spell).
// Both are pixel sprites built at runtime at the art's PPU (a translucent fill inside a
// 1 px rim), cached per size. Sits on the player next to AimIndicator; the renderers
// live on objects of their own, unparented, so the player's SortingGroup can't swallow
// their sorting layer. Runs after TargetPreview, so the ring sits on this frame's target.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(ActiveSkillSlots))]
public class SkillTelegraph : MonoBehaviour
{
    [SerializeField] string sortingLayer = "Indicators";
    [SerializeField] int sortingOrder = 0;
    [Tooltip("Art pixels per world unit, so the shapes' pixels match the sprites'.")]
    [SerializeField] int pixelsPerUnit = 32;

    [Header("Shape")]
    [SerializeField] Color shapeFill = new(1f, 0.8f, 0f, 0.12f);
    [SerializeField] Color shapeRim = new(1f, 0.8f, 0f, 0.55f);

    [Header("Target ring")]
    [SerializeField] Color ringFill = new(1f, 0.8f, 0f, 0.15f);
    [SerializeField] Color ringRim = new(1f, 0.8f, 0f, 1f);
    [Tooltip("World units the ring reaches past each side of the target's hurt collider.")]
    [SerializeField] float ringPadding = 0.15f;
    [SerializeField] float minRingWidth = 0.5f;
    [Tooltip("Ring height as a fraction of its width (an ellipse lying on the ground).")]
    [SerializeField, Range(0.2f, 1f)] float ringFlatten = 0.5f;

    ActiveSkillSlots skills;
    TargetPreview preview;
    WeaponUse weaponUse;
    Camera cam;
    SpriteRenderer shape;
    SpriteRenderer ring;

    readonly Dictionary<(int length, int arc, bool lane, int width), Sprite> shapeCache = new();
    readonly Dictionary<(int w, int h), Sprite> ringCache = new();

    void Awake()
    {
        skills = GetComponent<ActiveSkillSlots>();
        preview = GetComponent<TargetPreview>();
        weaponUse = GetComponent<WeaponUse>();
        shape = MakeRenderer("Skill Telegraph", sortingOrder);
        ring = MakeRenderer("Skill Target Ring", sortingOrder + 1);
    }

    SpriteRenderer MakeRenderer(string name, int order)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        sr.enabled = false;
        return sr;
    }

    void OnDisable()
    {
        if (shape != null) shape.enabled = false;
        if (ring != null) ring.enabled = false;
    }

    void OnDestroy()
    {
        if (shape != null) Destroy(shape.gameObject);
        if (ring != null) Destroy(ring.gameObject);
        foreach (Sprite s in shapeCache.Values) if (s != null) Destroy(s.texture);
        foreach (Sprite s in ringCache.Values) if (s != null) Destroy(s.texture);
    }

    void LateUpdate()
    {
        bool playing = UserInput.Instance != null
                       && (GameStateManager.Instance == null || GameStateManager.Instance.State == GameState.Playing);
        SkillAim aim = default;
        bool aiming = playing && skills.TryGetAiming(out ActiveSkill skill, out HandSide side) && skill.TryGetAim(side, out aim);
        if (!aiming)
        {
            shape.enabled = ring.enabled = false;
            return;
        }

        Vector2 origin = weaponUse != null ? weaponUse.Origin : (Vector2)transform.position;
        if (cam == null) cam = weaponUse != null && weaponUse.cam != null ? weaponUse.cam : Camera.main;
        Vector2 dir = UserInput.Instance.AimDirection(origin, cam);

        shape.sprite = ShapeSprite(aim);
        shape.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
        shape.enabled = true;

        Hurtbox target = preview != null && preview.PreviewingSkill ? preview.PrimaryHurtbox : null;
        if (target == null || target.HurtCollider == null || target.Owner == null)
        {
            ring.enabled = false;
            return;
        }
        float width = Mathf.Max(minRingWidth, target.HurtCollider.bounds.size.x + 2f * ringPadding);
        ring.sprite = RingSprite(width);
        ring.transform.position = target.Owner.transform.position;
        ring.enabled = true;
    }

    Sprite ShapeSprite(in SkillAim aim)
    {
        int length = Mathf.Max(1, Mathf.RoundToInt(aim.range * pixelsPerUnit));
        int width = aim.lane ? Mathf.Max(1, Mathf.RoundToInt(aim.width * pixelsPerUnit)) : 0;
        int arc = aim.lane ? 0 : Mathf.RoundToInt(aim.arc);
        var key = (length, arc, aim.lane, width);
        if (shapeCache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        sprite = aim.lane ? Lane(length, width) : Wedge(length, arc);
        shapeCache[key] = sprite;
        return sprite;
    }

    // A cone of `arc` degrees and `radius` pixels from the texture's centre pixel (the
    // pivot), pointing along +X; 360 is a full disc.
    Sprite Wedge(int radius, float arc)
    {
        int size = radius * 2 + 1;
        float half = arc * 0.5f;
        var inside = new bool[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius, dy = y - radius;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                inside[y * size + x] = d <= radius
                    && (d < 0.5f || Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg) <= half);
            }
        }
        return Paint(inside, size, size, new Vector2(0.5f, 0.5f), shapeFill, shapeRim);
    }

    // A strip `length` long and `width` wide, pivoted at its near end's middle.
    Sprite Lane(int length, int width)
    {
        var inside = new bool[length * width];
        for (int i = 0; i < inside.Length; i++) inside[i] = true;
        return Paint(inside, length, width, new Vector2(0f, 0.5f), shapeFill, shapeRim);
    }

    // An ellipse `worldWidth` wide, flattened onto the ground; odd sizes so it centres
    // on a pixel.
    Sprite RingSprite(float worldWidth)
    {
        int w = Mathf.Max(3, Mathf.RoundToInt(worldWidth * pixelsPerUnit)) | 1;
        int h = Mathf.Max(3, Mathf.RoundToInt(w * ringFlatten)) | 1;
        if (ringCache.TryGetValue((w, h), out Sprite sprite) && sprite != null) return sprite;

        float a = w * 0.5f, b = h * 0.5f;
        var inside = new bool[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f - a) / a, v = (y + 0.5f - b) / b;
                inside[y * w + x] = u * u + v * v <= 1f;
            }
        }
        sprite = Paint(inside, w, h, new Vector2(0.5f, 0.5f), ringFill, ringRim);
        ringCache[(w, h)] = sprite;
        return sprite;
    }

    // `fill` inside the shape, `rim` on its edge: inside pixels with a 4-neighbour out.
    Sprite Paint(bool[] inside, int w, int h, Vector2 pivot, Color32 fill, Color32 rim)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!inside[i]) continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                            || !inside[i - 1] || !inside[i + 1] || !inside[i - w] || !inside[i + w];
                px[i] = edge ? rim : fill;
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, pixelsPerUnit);
    }
}

using UnityEngine;
using UnityEngine.Rendering;

// Manual Y-sorting: orders a sprite (or a whole SortingGroup) by a chosen anchor's
// world Y, so things lower on screen draw in front. We do this in code instead of
// the renderer's transparency axis-sort because it lets us bake an elevation tier
// into the order (a whole upper level drawing above a lower one) once the height
// system lands.
//
//   order = elevationTier * tierBand  -  round(anchor.y * precision)
//
// Put the anchor at the FRONT-BOTTOM of the occluding footprint (e.g. a tree's
// trunk base), not the sprite center. A footprint collider keeps the player out of
// the ambiguous band so the in-front/behind flip never happens on-screen.
[DisallowMultipleComponent]
public class YSort : MonoBehaviour
{
    [Tooltip("Sort point in world space. Defaults to this transform. Place it at " +
             "the front-bottom of the footprint (a tree's trunk base, the feet).")]
    [SerializeField] Transform anchor;

    [Tooltip("Elevation level. Reserved for the multi-level height system; a higher " +
             "tier always sorts above a lower one. Leave 0 for ground level.")]
    [SerializeField] int elevationTier = 0;

    [Tooltip("Recompute every frame. Enable for moving objects (the player); leave " +
             "off for static props so they only sort once at start.")]
    [SerializeField] bool dynamic = false;

    [Tooltip("World units to sortingOrder steps. 100 = 0.01-unit precision.")]
    [SerializeField] int precision = 100;

    [Tooltip("Order range reserved per elevation tier. Must exceed the map's height " +
             "in units * precision so tiers never overlap.")]
    [SerializeField] int tierBand = 5000;

    SortingGroup group;
    SpriteRenderer sr;

    void Awake()
    {
        if (anchor == null) anchor = transform;

        // A SortingGroup (e.g. the player) sorts the whole rig as one unit; otherwise
        // drive the sprite directly.
        group = GetComponent<SortingGroup>();
        if (group == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable() => Apply();

    void Start() => Apply();

    void LateUpdate()
    {
        if (dynamic) Apply();
    }

    void Apply()
    {
        int order = elevationTier * tierBand - Mathf.RoundToInt(anchor.position.y * precision);

        if (group != null) group.sortingOrder = order;
        else if (sr != null) sr.sortingOrder = order;
    }

    // Called by the height system when an entity changes elevation.
    public void SetTier(int tier)
    {
        elevationTier = tier;
        Apply();
    }
}

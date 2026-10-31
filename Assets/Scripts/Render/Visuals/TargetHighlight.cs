using UnityEngine;

// Owns a hittable entity's Highlight art -- its child renderer(s) named "Highlight",
// e.g. the Aseprite layer of that name -- which two systems want to show:
//   - melee targeting (TargetPreview): tinted primary / secondary colour
//   - interaction focus (Highlightable): the outline in its authored colour
// Targeting wins; with neither, the layer is hidden. Highlightable routes through
// SetInteract when this is present, so the two never fight over the renderer.
//
// A pickup item's Highlight layer shows in PickupColor (green) for both -- the E focus
// (PlayerInteractor attaches this to any pickup with Highlight art) and, for a
// hittable one like a log, a melee target -- so the two never read as two highlights.
//
// Hidden = renderer off AND fully transparent, so an animation that keys the layer's
// renderer back on still shows nothing. While shown it re-applies every LateUpdate
// (after the Animator, SpriteLayerFollower and OccluderFade), multiplying in the
// alpha of the renderer it sits on, so the highlight fades with an occluded tree.
//
// Attached by the entity's Hurtbox when the entity has Highlight art, so every
// hittable target picks it up without per-prefab setup (it starts hidden).
public class TargetHighlight : MonoBehaviour
{
    public const string LayerName = "Highlight";

    // A pickup item's highlight colour; PlayerInteractor sets it from its inspector.
    public static Color PickupColor = new(0.3f, 0.9f, 0.3f, 1f);

    [Tooltip("Highlight renderers. Defaults to every child SpriteRenderer named \"Highlight\".")]
    [SerializeField] SpriteRenderer[] renderers;

    SpriteRenderer[] bodies;   // the renderer each highlight sits on (fade source); may be null
    Color[] authored;          // each renderer's own colour, shown for an interaction focus
    Color tint;
    bool targeted;
    bool interact;
    bool pickup;

    public static bool IsHighlight(SpriteRenderer sr) => sr != null && sr.gameObject.name == LayerName;

    // The target's highlight: the existing component, or a new one when the target
    // has Highlight art. Null when it has none (nothing to show).
    public static TargetHighlight Attach(GameObject target)
    {
        if (target.TryGetComponent(out TargetHighlight existing)) return existing;
        foreach (SpriteRenderer sr in target.GetComponentsInChildren<SpriteRenderer>(true))
            if (IsHighlight(sr)) return target.AddComponent<TargetHighlight>();
        return null;
    }

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = System.Array.FindAll(GetComponentsInChildren<SpriteRenderer>(true), IsHighlight);

        pickup = TryGetComponent(out Pickupable _);
        // importer art (PropArt): the layers are siblings, so the body is named, not the parent
        SpriteRenderer propBody = TryGetComponent(out PropArt art) ? art.Body : null;
        bodies = new SpriteRenderer[renderers.Length];
        authored = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform parent = renderers[i].transform.parent;
            bodies[i] = parent != null && parent.TryGetComponent(out SpriteRenderer onParent) ? onParent : propBody;
            authored[i] = renderers[i].color;
        }

        Refresh();
    }

    // Melee targeting: show tinted as a primary / secondary target.
    public void SetTarget(Color color)
    {
        tint = color;
        targeted = true;
        Refresh();
    }

    public void ClearTarget()
    {
        targeted = false;
        Refresh();
    }

    // Interaction focus (Highlightable): show the outline as authored.
    public void SetInteract(bool on)
    {
        interact = on;
        Refresh();
    }

    void Refresh()
    {
        enabled = targeted || interact;
        Apply(enabled);
    }

    void LateUpdate() => Apply(true);

    void Apply(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            Color c = !on ? Color.clear : pickup ? PickupColor : targeted ? tint : authored[i];
            if (on && bodies[i] != null) c.a *= bodies[i].color.a;
            sr.color = c;
            sr.enabled = on;
        }
    }
}

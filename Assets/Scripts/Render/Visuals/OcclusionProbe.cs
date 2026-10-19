using UnityEngine;
using UnityEngine.Rendering;

// The player-side half of occluder fading: a trigger sized to the player's visible
// body, on the OcclusionProbe layer (which collides only with Occluder). OccluderFade
// watches for it and reads where the body is drawn, so a prop only fades when the
// player actually sorts behind it.
[RequireComponent(typeof(Collider2D))]
public class OcclusionProbe : MonoBehaviour
{
    SortingGroup group;
    SpriteRenderer sr;

    void Awake()
    {
        // The player sorts as one SortingGroup (YSort drives it); fall back to a
        // plain sprite for anything without one.
        group = GetComponentInParent<SortingGroup>();
        if (group == null) sr = GetComponentInParent<SpriteRenderer>();
    }

    public int SortingLayerID => group != null ? group.sortingLayerID : sr != null ? sr.sortingLayerID : 0;
    public int SortingOrder => group != null ? group.sortingOrder : sr != null ? sr.sortingOrder : 0;
}

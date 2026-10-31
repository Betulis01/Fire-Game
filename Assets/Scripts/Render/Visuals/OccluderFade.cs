using System.Collections.Generic;
using UnityEngine;

// Fades a tall prop (tree, ore) see-through while the player is hidden behind it.
// Lives on the prop's "Occluder" child: trigger collider(s) covering the sprite's
// visible pixels, on the Occluder layer, which collides only with the player's
// OcclusionProbe. Overlap alone isn't enough -- the prop fades only while the probe
// also sorts behind it (YSort), so walking in front of a trunk never fades it.
//
// Only alpha is touched, so tints from other effects survive. The component sleeps
// (disabled) once the fade settles with no probe inside; trigger callbacks still
// reach disabled behaviours and wake it.
[RequireComponent(typeof(Collider2D))]
public class OccluderFade : MonoBehaviour
{
    [Tooltip("Renderers to fade. Defaults to every SpriteRenderer under the parent prop, " +
             "except a hittable prop's Highlight layer (TargetHighlight fades that).")]
    [SerializeField] SpriteRenderer[] renderers;

    [Tooltip("Renderer whose sort order decides in front / behind. Defaults to the first renderer.")]
    [SerializeField] SpriteRenderer sortReference;

    [Tooltip("Alpha while the player is behind the prop.")]
    [Range(0f, 1f)]
    [SerializeField] float fadedAlpha = 0.45f;

    [Tooltip("Alpha change per second.")]
    [SerializeField] float fadeSpeed = 4f;

    // One entry per overlapping collider pair: a multi-box occluder sees the same
    // probe enter once per box, so membership is counted, not a set.
    readonly List<OcclusionProbe> probes = new();
    float alpha = 1f;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
        {
            // A hittable prop's Highlight layer belongs to TargetHighlight, which fades
            // it with the body itself
            Transform prop = transform.parent != null ? transform.parent : transform;
            bool hittable = prop.GetComponentInChildren<Hurtbox>(true) != null;
            renderers = System.Array.FindAll(prop.GetComponentsInChildren<SpriteRenderer>(true),
                                             sr => !hittable || !TargetHighlight.IsHighlight(sr));
        }
        if (sortReference == null)
        {
            PropArt art = GetComponentInParent<PropArt>();
            sortReference = art != null ? art.Body : renderers.Length > 0 ? renderers[0] : null;
        }

        enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out OcclusionProbe probe)) return;
        probes.Add(probe);
        enabled = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out OcclusionProbe probe)) return;
        probes.Remove(probe);
        enabled = true;   // fade back in
    }

    void Update()
    {
        probes.RemoveAll(p => p == null);

        // Re-checked every frame while inside: the player can cross the prop's sort
        // line without leaving the trigger.
        bool behind = false;
        foreach (OcclusionProbe p in probes)
        {
            if (p.isActiveAndEnabled && IsBehind(p)) { behind = true; break; }
        }

        float target = behind ? fadedAlpha : 1f;
        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
        Apply();

        if (probes.Count == 0 && alpha == target) enabled = false;
    }

    bool IsBehind(OcclusionProbe p)
    {
        if (sortReference == null) return false;

        int mine = SortingLayer.GetLayerValueFromID(sortReference.sortingLayerID);
        int theirs = SortingLayer.GetLayerValueFromID(p.SortingLayerID);
        if (theirs != mine) return theirs < mine;
        return p.SortingOrder < sortReference.sortingOrder;
    }

    void Apply()
    {
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}

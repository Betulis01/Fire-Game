using System.Collections.Generic;
using UnityEngine;

// The damaging region produced by an attack. Authored on a weapon prefab; the shape
// is a circle of `radius`. Strike() only DETECTS hits: it polls once, skips the
// attacker, dedups per entity, and delivers a HitInfo to each victim's Hurtbox. What a
// hit does (damage, knockback, ...) lives in the victim's IHitReactor components
// (Health checks ToolDamageFilter before applying damage); recoil lives in the
// attacker's IAttackReactor.
public class Hitbox : MonoBehaviour
{
    public float radius = 0.6f;        // strike size
    public LayerMask hurtboxLayers;    // set to the "Hurtbox" layer

    readonly List<Collider2D> results = new();
    readonly HashSet<Health> hitThisStrike = new();

    public void Strike(in AttackData attack, GameObject owner, Vector2 center)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hurtboxLayers);

        results.Clear();
        hitThisStrike.Clear();
        Physics2D.OverlapCircle(center, radius, filter, results);

        bool anyHit = false;
        Vector2 contactPoint = center;     // where the circle actually touched a hurtbox

        foreach (Collider2D col in results)
        {
            Hurtbox hb = col.GetComponentInParent<Hurtbox>();
            if (hb == null || !hitThisStrike.Add(hb.Health)) continue;   // once per entity
            if (!HitResolution.TryHit(col, owner, attack, center, out HitInfo hit)) continue;

            if (!anyHit) contactPoint = hit.point;
            anyHit = true;
        }

        // notify the attacker's reactors (e.g. AttackRecoil) once per connecting swing,
        // at the point the strike circle actually met the (first) hurtbox rather than
        // the circle's own center, so attacker-side effects land where contact happened.
        if (anyHit) HitResolution.NotifyAttacker(owner, attack, contactPoint);
    }
}

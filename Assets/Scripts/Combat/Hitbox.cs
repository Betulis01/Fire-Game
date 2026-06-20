using System.Collections.Generic;
using UnityEngine;

// The damaging region produced by an attack. Authored on a weapon prefab; the shape
// is a circle of `radius`. Strike() only DETECTS hits: it polls once, skips the
// attacker and anything its ToolDamageFilter rejects, dedups per entity, and delivers
// a HitInfo to each victim's Hurtbox. What a hit does (damage, knockback, ...) lives
// in the victim's IHitReactor components; recoil lives in the attacker's IAttackReactor.
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

        Vector2 attackerPos = owner != null ? (Vector2)owner.transform.position : center;
        bool anyHit = false;
        Vector2 contactPoint = center;     // where the circle actually touched a hurtbox

        foreach (Collider2D col in results)
        {
            Hurtbox hb = col.GetComponentInParent<Hurtbox>();
            if (hb == null || hb.Health == null || hb.Owner == owner) continue;
            // entities can restrict which tool kinds hurt them (axe-only trees, ...);
            // no filter means any tool damages them.
            ToolDamageFilter gate = hb.Health.GetComponent<ToolDamageFilter>();
            if (gate != null && !gate.Accepts(attack.kind)) continue;
            if (!hitThisStrike.Add(hb.Health)) continue;   // once per entity

            Vector2 toTarget = (Vector2)hb.Owner.transform.position - attackerPos;
            Vector2 dir = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector2.zero;
            if (!anyHit) contactPoint = hb.HurtCollider.ClosestPoint(center);
            hb.TakeHit(new HitInfo(owner, hb.Owner, attack, center, dir));
            anyHit = true;
        }

        // notify the attacker's reactors (e.g. AttackRecoil) once per connecting swing,
        // at the point the strike circle actually met the (first) hurtbox rather than
        // the circle's own center, so attacker-side VFX lands where contact happened.
        if (anyHit && owner != null)
        {
            HitInfo info = new HitInfo(owner, null, attack, contactPoint, Vector2.zero);
            foreach (IAttackReactor reactor in owner.GetComponents<IAttackReactor>())
                reactor.OnDealtHit(info);
        }
    }
}

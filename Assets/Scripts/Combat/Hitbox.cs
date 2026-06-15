using System.Collections.Generic;
using UnityEngine;

// The damaging region produced by an attack. Authored on a weapon prefab; the
// shape is a circle of `radius`. Strike() polls once and damages each overlapped
// Health a single time, skipping the attack's own owner.
public class Hitbox : MonoBehaviour
{
    public float radius = 0.6f;        // strike size
    public LayerMask hurtboxLayers;    // set to the "Hurtbox" layer

    readonly List<Collider2D> results = new();
    readonly HashSet<Health> hitThisStrike = new();

    public void Strike(float damage, GameObject owner, Vector2 center)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hurtboxLayers);

        results.Clear();
        hitThisStrike.Clear();
        Physics2D.OverlapCircle(center, radius, filter, results);

        foreach (Collider2D col in results)
        {
            Hurtbox hb = col.GetComponentInParent<Hurtbox>();
            if (hb == null || hb.Health == null || hb.Owner == owner) continue;
            if (!hitThisStrike.Add(hb.Health)) continue;   // once per entity
            hb.Health.TakeDamage(damage);
        }
    }
}

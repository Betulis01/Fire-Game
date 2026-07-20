using UnityEngine;

// Shared "does this collider count as a valid hit" check, used by every hit
// detector (Hitbox's one-shot overlap, Projectile's per-frame trigger). Keeping it
// in one place means melee and ranged attacks can't drift apart on what a hit is:
// skip the attacker, require a Hurtbox with Health.
public static class HitResolution
{
    public static bool TryHit(Collider2D col, GameObject owner, in AttackData attack,
                               Vector2 center, out HitInfo hit)
    {
        hit = default;

        Hurtbox hb = col.GetComponentInParent<Hurtbox>();
        if (hb == null || hb.Health == null || hb.Owner == owner) return false;

        Vector2 attackerPos = owner != null ? (Vector2)owner.transform.position : center;
        Vector2 toTarget = (Vector2)hb.Owner.transform.position - attackerPos;
        Vector2 dir = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector2.zero;
        Vector2 point = hb.HurtCollider.ClosestPoint(center);

        hit = new HitInfo(owner, hb.Owner, attack, point, dir);
        hb.TakeHit(hit);
        return true;
    }

    // Notify the attacker's reactors (e.g. AttackRecoil) once per connecting hit, at
    // the point contact actually happened.
    public static void NotifyAttacker(GameObject owner, in AttackData attack, Vector2 contactPoint)
    {
        if (owner == null) return;
        HitInfo info = new HitInfo(owner, null, attack, contactPoint, Vector2.zero);
        foreach (IAttackReactor reactor in owner.GetComponents<IAttackReactor>())
            reactor.OnDealtHit(info);
    }

    // Notify the attacker's swing reactors (e.g. HitStopOnSwing) the instant a swing
    // starts (windup), regardless of whether it goes on to connect.
    public static void NotifySwing(GameObject owner)
    {
        if (owner == null) return;
        foreach (IAttackSwingReactor reactor in owner.GetComponents<IAttackSwingReactor>())
            reactor.OnSwing();
    }
}

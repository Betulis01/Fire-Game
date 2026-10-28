using UnityEngine;

// Shared hit delivery, used by every hit detector (MeleeWeapon's targeted strike,
// Projectile's per-frame trigger). Keeping it in one place means melee and ranged
// attacks can't drift apart on what a hit is: skip the attacker, require a Hurtbox
// with Health, resolve this victim's damage (DamageCalculator), aim the hit
// attacker -> victim.
public static class HitResolution
{
    // Projectile path: does this touched collider count as a hit? Delivers it if so.
    public static bool TryHit(Collider2D col, GameObject owner, in AttackData attack,
                               Vector2 center, out HitInfo hit)
    {
        hit = default;

        Hurtbox hb = col.GetComponentInParent<Hurtbox>();
        if (hb == null || hb.Health == null || hb.Owner == owner || !hb.CanBeHit) return false;

        hit = Deliver(hb, owner, attack, hb.HurtCollider.ClosestPoint(center));
        return true;
    }

    // Hand a landed hit to the victim's reactors, at `point` (where contact happened).
    // A secondary target (cleave collateral) takes secondaryDamage of the damage and
    // crits at secondaryCritFactor of the attacker's chance.
    public static HitInfo Deliver(Hurtbox hb, GameObject owner, in AttackData attack, Vector2 point,
                                  bool primary = true, float secondaryDamage = 1f, float secondaryCritFactor = 1f)
    {
        Vector2 attackerPos = owner != null ? (Vector2)owner.transform.position : point;
        Vector2 toTarget = (Vector2)hb.Owner.transform.position - attackerPos;
        Vector2 dir = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector2.zero;

        float damage = DamageCalculator.Resolve(attack, hb, primary, secondaryDamage, secondaryCritFactor, out bool crit);
        HitInfo hit = new HitInfo(owner, hb.Owner, attack, point, dir, damage, crit, primary);
        hb.TakeHit(hit);
        return hit;
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

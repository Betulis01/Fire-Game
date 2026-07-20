using UnityEngine;

// One landed hit. Hitbox detects it and hands it to the victim's IHitReactor
// components (Health -> damage, Knockback -> shove, ...) and, once per connecting
// swing, to the attacker's IAttackReactor components (AttackRecoil -> recoil). Hitbox
// stays "detect and deliver"; what a hit *does* lives in the reactors.
public readonly struct HitInfo
{
    public readonly GameObject attacker;
    public readonly GameObject victim;        // null on the attacker-side notification
    public readonly float damage;
    public readonly ToolKind kind;
    public readonly Vector2 point;            // strike centre, world space
    public readonly Vector2 direction;        // unit vector, attacker -> victim
    public readonly float targetKnockback;    // shove for the victim
    public readonly float selfKnockback;      // recoil for the attacker

    public HitInfo(GameObject attacker, GameObject victim, in AttackData attack,
                   Vector2 point, Vector2 direction)
    {
        this.attacker = attacker;
        this.victim = victim;
        damage = attack.damage;
        kind = attack.kind;
        this.point = point;
        this.direction = direction;
        targetKnockback = attack.targetKnockback;
        selfKnockback = attack.selfKnockback;
    }
}

// Implemented by components on a victim that react to being hit.
public interface IHitReactor { void OnHit(in HitInfo hit); }

// Implemented by components on an attacker that react to landing a hit.
public interface IAttackReactor { void OnDealtHit(in HitInfo hit); }

// Implemented by components on an attacker that react to a swing starting
// (windup), before it's known whether the swing will connect.
public interface IAttackSwingReactor { void OnSwing(); }

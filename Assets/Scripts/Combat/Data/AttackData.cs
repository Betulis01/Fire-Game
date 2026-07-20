using UnityEngine;

// The parameters of one swing, sourced from the weapon's Tool. Collapses what used
// to be a clump of arguments threaded weapon -> ToolUser -> Hitbox into one value.
// Also carries the weapon's contact VFX so it reaches the victim through HitInfo;
// the victim's SurfaceHitEffect spawns it in its surface's tint.
public readonly struct AttackData
{
    public readonly float damage;
    public readonly ToolKind kind;
    public readonly float targetKnockback;   // shove dealt to a hit target
    public readonly float selfKnockback;     // recoil dealt back to the attacker
    public readonly GameObject hitEffectPrefab;   // the weapon's contact VFX, spawned by the victim

    public AttackData(float damage, ToolKind kind, float targetKnockback, float selfKnockback,
                      GameObject hitEffectPrefab)
    {
        this.damage = damage;
        this.kind = kind;
        this.targetKnockback = targetKnockback;
        this.selfKnockback = selfKnockback;
        this.hitEffectPrefab = hitEffectPrefab;
    }
}

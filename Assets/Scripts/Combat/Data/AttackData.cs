// The parameters of one swing, sourced from the weapon's Tool. Collapses what used
// to be a clump of arguments threaded weapon -> ToolUser -> Hitbox into one value.
public readonly struct AttackData
{
    public readonly float damage;
    public readonly ToolKind kind;
    public readonly float targetKnockback;   // shove dealt to a hit target
    public readonly float selfKnockback;     // recoil dealt back to the attacker

    public AttackData(float damage, ToolKind kind, float targetKnockback, float selfKnockback)
    {
        this.damage = damage;
        this.kind = kind;
        this.targetKnockback = targetKnockback;
        this.selfKnockback = selfKnockback;
    }
}

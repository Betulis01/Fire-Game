// The recipe of one attack, built when it's launched (Tool.GetAttack) and resolved
// into actual damage once per target it lands on (DamageCalculator): the weapon's
// base damage range, the ability's scaling (light = 100% weapon damage, heavy /
// skills scale it), and a snapshot of the attacker's combat stats. Where/whom it
// hits is the swing's StrikeShape.
public readonly struct AttackData
{
    // Weapon base damage, rolled per target.
    public readonly float minDamage;
    public readonly float maxDamage;

    // Ability scaling: damage = baseHit x coefficient + flatBonus.
    public readonly float coefficient;
    public readonly float flatBonus;

    // The attacker's Tier 2 stats when the attack was launched (zero/defaults for
    // an attacker without CharacterStats).
    public readonly float attackPower;
    public readonly float critChance;       // percent
    public readonly float critMultiplier;

    public readonly ToolKind kind;
    public readonly float targetKnockback;   // shove dealt to a hit target
    public readonly float selfKnockback;     // recoil dealt back to the attacker

    public AttackData(float minDamage, float maxDamage, float coefficient, float flatBonus,
                      float attackPower, float critChance, float critMultiplier,
                      ToolKind kind, float targetKnockback, float selfKnockback)
    {
        this.minDamage = minDamage;
        this.maxDamage = maxDamage;
        this.coefficient = coefficient;
        this.flatBonus = flatBonus;
        this.attackPower = attackPower;
        this.critChance = critChance;
        this.critMultiplier = critMultiplier;
        this.kind = kind;
        this.targetKnockback = targetKnockback;
        this.selfKnockback = selfKnockback;
    }

    // Same attack with its ability scaling multiplied (e.g. a skill dealing 140%).
    public AttackData Scaled(float factor) => new(minDamage, maxDamage, coefficient * factor, flatBonus * factor,
                                                  attackPower, critChance, critMultiplier,
                                                  kind, targetKnockback, selfKnockback);

    // Same attack without recoil (e.g. a spin hitting from every side).
    public AttackData WithoutRecoil() => new(minDamage, maxDamage, coefficient, flatBonus,
                                             attackPower, critChance, critMultiplier,
                                             kind, targetKnockback, 0f);
}

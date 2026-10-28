using UnityEngine;

// Tier 3: turns an attack's recipe into the damage one target takes. Runs once per
// target, so each gets its own weapon roll and crit roll.
//   Character (has CharacterStats):
//     baseHit = roll x (1 + AP / 100)
//     raw     = baseHit x coefficient + flatBonus
//     crit    : raw x critMultiplier, at critChance (x secondaryCritFactor for a secondary)
//     final   = raw x (1 - armor / (armor + K)) (x secondaryDamage for a secondary)
//   Anything else (trees, ore): the raw weapon roll -- gathering ignores stats, never
//   crits, and has no armor; only the cleave's secondary penalty still applies.
// ToolDamageFilter and shield blocking are the victim's business (Health.OnHit).
public static class DamageCalculator
{
    public static float Resolve(in AttackData attack, Hurtbox victim, bool primary,
                                float secondaryDamage, float secondaryCritFactor, out bool crit)
    {
        crit = false;
        float roll = Random.Range(attack.minDamage, attack.maxDamage);
        float collateral = primary ? 1f : secondaryDamage;

        CharacterStats stats = victim != null ? victim.Stats : null;
        if (stats == null) return roll * collateral;

        float baseHit = roll * (1f + attack.attackPower / StatFormulas.AttackPowerDivisor);
        float raw = baseHit * attack.coefficient + attack.flatBonus;

        float critChance = attack.critChance * (primary ? 1f : secondaryCritFactor);
        crit = raw > 0f && Random.value * 100f < critChance;
        if (crit) raw *= attack.critMultiplier;

        return raw * (1f - StatFormulas.DamageReduction(stats.Armor)) * collateral;
    }
}

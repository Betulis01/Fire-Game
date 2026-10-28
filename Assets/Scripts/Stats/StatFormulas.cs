// Every conversion and constant of the stat pipeline in one place, so balance is
// tuned here rather than scattered through the code that uses it.
//   Tier 1 -> Tier 2: attributes become linear ratings, never flat percentages
//   (AGI gives a sliver of crit per point, so crit can't run away by mid levels).
//   Tier 3: armor mitigation and the secondary-target (cleave) penalties.
public static class StatFormulas
{
    public const float AttackPowerPerStrength = 2f;
    public const float AttackPowerPerAgility = 1f;
    public const float CritPercentPerAgility = 0.05f;
    public const float SpellPowerPerIntelligence = 2f;
    public const float HealthPerStamina = 10f;

    // Base hit = weapon roll x (1 + AP / AttackPowerDivisor): 100 AP doubles a roll.
    public const float AttackPowerDivisor = 100f;

    // Damage reduction = armor / (armor + ArmorK): armor never reaches 100%, and
    // ArmorK is the armor that halves damage. 30 (a helmet) ~6%, 100 ~17%, 250 ~33%:
    // a single piece is a small edge; a full set is what matters.
    public const float ArmorK = 500f;

    // Defaults for a weapon's cleave: secondaries take half damage and crit half as often.
    public const float DefaultSecondaryDamage = 0.5f;
    public const float DefaultSecondaryCritFactor = 0.5f;

    public static float DamageReduction(float armor) => armor <= 0f ? 0f : armor / (armor + ArmorK);
}

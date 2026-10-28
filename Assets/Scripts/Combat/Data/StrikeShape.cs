// The region and reach of one melee swing: an aimed cone from the wielder's aim
// origin. `range` is the cone's radius (the strike's exact max reach), `arcAngle` its
// full opening in degrees (360 = all around, e.g. Whirlwind), `maxTargets` how many
// entities one swing may hit at most (primary + secondaries), and for the
// secondaries a damage multiplier (`secondaryDamage`) and a crit-chance multiplier
// (`secondaryCritFactor`), so cleaving crowds can't outscale single-target hits.
// Sourced from the weapon's Tool, then adjusted by the wielder's IStrikeModifiers
// (skill tree, ...).
public struct StrikeShape
{
    public float range;
    public float arcAngle;
    public int maxTargets;
    public float secondaryDamage;
    public float secondaryCritFactor;

    public StrikeShape(float range, float arcAngle, int maxTargets,
                       float secondaryDamage = StatFormulas.DefaultSecondaryDamage,
                       float secondaryCritFactor = StatFormulas.DefaultSecondaryCritFactor)
    {
        this.range = range;
        this.arcAngle = arcAngle;
        this.maxTargets = maxTargets;
        this.secondaryDamage = secondaryDamage;
        this.secondaryCritFactor = secondaryCritFactor;
    }
}

// Implemented by components on a wielder that change the shape of its swings, e.g. a
// skill-tree passive raising the target cap for one weapon kind. WeaponUse applies
// every one on the player to each swing (and to the live target preview).
public interface IStrikeModifier { void ModifyStrike(ToolKind kind, ref StrikeShape shape); }

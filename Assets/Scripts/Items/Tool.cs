using UnityEngine;

// Capability: this item is a tool/weapon with combat-ish stats. Data only for
// now (chopping/attacking behaviour comes later); sits next to Burnable as
// another item capability in the composition model.
public class Tool : MonoBehaviour
{

    public ToolKind kind = ToolKind.Fist;   // gates what this tool can damage

    [Tooltip("Needs both hands (the bow, the pickaxe): it fills a loadout set's Main and Off Hand.")]
    public bool twoHanded;

    [Header("Weapon Damage")]
    [Tooltip("Base damage range, rolled per target hit. Scaled by the wielder's Attack " +
             "Power and the ability (light = 100%, heavy = the heavy coefficient) against " +
             "characters; resources (trees, ore) take the raw roll.")]
    public float minDamage = 4f;
    public float maxDamage = 6f;

    [Tooltip("Melee: the strike cone's radius -- the exact max reach from the wielder's " +
             "aim origin. Ranged: the base (uncharged) shot distance.")]
    public float range = 1f;

    [Header("Strike Cone (melee)")]
    [Tooltip("Full opening angle of the strike cone, in degrees, centred on the aim.")]
    [Range(1f, 360f)] public float arcAngle = 90f;

    [Tooltip("Grace distance past `range`: a target this much further out is still " +
             "highlighted, and the swing steps the wielder in during its windup so the hit " +
             "lands at the tip of the reach (player swings only). 0 = no step.")]
    [Min(0f)] public float lungeBuffer = 0.4f;

    [Tooltip("Most entities one swing can hit: the primary plus secondaries. Skill-tree " +
             "passives (IStrikeModifier) can raise it.")]
    [Min(1)] public int maxTargets = 2;

    [Tooltip("Damage multiplier for secondary targets (1 = same as the primary).")]
    [Min(0f)] public float secondaryDamageMultiplier = StatFormulas.DefaultSecondaryDamage;

    [Tooltip("Crit chance multiplier for secondary targets (1 = same as the primary).")]
    [Min(0f)] public float secondaryCritFactor = StatFormulas.DefaultSecondaryCritFactor;

    [Tooltip("Shove dealt to a hit target, away from the attacker (needs a Knockback " +
             "component on the target).")]
    public float targetKnockback = 3f;

    [Tooltip("Recoil dealt back to the attacker on a connecting swing (applied by the " +
             "attacker's AttackRecoil, which can scale it).")]
    public float selfKnockback = 1f;

    [Tooltip("Slash/swing VFX spawned at swing start on every swing, hit or miss. " +
             "The victim's HitEffect contact VFX is separate and layers on top.")]
    public GameObject swingEffectPrefab;

    [Tooltip("Degrees added so the swing effect's drawn forward aligns with the aim " +
             "(+X). Art pointing right = 0, up = -90.")]
    public float swingEffectAngleOffset;

    [Tooltip("Seconds the swing stays frozen on its contact frame when it connects -- " +
             "the axe biting into a trunk, the pickaxe ringing off stone. 0 = no hold.")]
    [Min(0f)] public float impactHold = 0f;

    [Header("Lunge (fires on windup)")]
    [Tooltip("Forward push speed fired the instant the swing starts (windup), not on " +
             "hit. 0 disables the lunge for this tool.")]
    public float lungeSpeed = 0f;

    [Tooltip("How long the lunge takes to ease down to zero.")]
    public float lungeDuration = 0.15f;

    [Tooltip("Shapes the lunge's speed over its duration, 0..1 normalized time to a " +
             "0..1 speed multiplier. Defaults to an ease-out (fast start, smooth stop).")]
    public AnimationCurve lungeCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, 0f, 0f));

    [Header("Heavy Attack (charged)")]
    [Tooltip("Stats used when the attack is released after being charged, instead of " +
             "the light fields above. Defaults to inert (0 damage, no effects) until tuned.")]
    public AttackProfile heavy;

    [Tooltip("Minimum time the button must be held for a release to count as heavy. " +
             "Releasing any earlier than this always resolves light, no matter how far " +
             "the windup animation has played.")]
    public float chargeTime = 0.3f;

    // Which family of player attack clips this tool swings with: PlayerAnimator plays
    // {dir}_attack_{style}_{l|r}, falling back to {dir}_attack_{l|r} when the style is
    // null or that clip doesn't exist.
    public string AttackStyle => kind switch
    {
        ToolKind.Sword => "slash",
        ToolKind.Axe => "hack",
        _ => null,
    };

    // The attack recipe for a swing/shot: this weapon's damage range, the ability's
    // scaling (light = 100% weapon damage, heavy = the heavy profile's coefficient and
    // flat bonus) and the wielder's combat stats (none = no AP, no crits).
    public AttackData GetAttack(bool isHeavy, CharacterStats wielder)
    {
        float ap = wielder != null ? wielder.AttackPower : 0f;
        float critChance = wielder != null ? wielder.CritChance : 0f;
        float critMultiplier = wielder != null ? wielder.CritMultiplier : 1f;
        return isHeavy
            ? new AttackData(minDamage, maxDamage, heavy.damageCoefficient, heavy.flatBonus,
                             ap, critChance, critMultiplier, kind, heavy.targetKnockback, heavy.selfKnockback)
            : new AttackData(minDamage, maxDamage, 1f, 0f,
                             ap, critChance, critMultiplier, kind, targetKnockback, selfKnockback);
    }

    // The strike cone for a swing, before the wielder's IStrikeModifiers. A heavy
    // swing uses the heavy profile's arc/cap where set (0 = same as light).
    public StrikeShape GetStrike(bool isHeavy)
    {
        float arc = isHeavy && heavy.arcAngle > 0f ? heavy.arcAngle : arcAngle;
        int cap = isHeavy && heavy.maxTargets > 0 ? heavy.maxTargets : maxTargets;
        return new StrikeShape(range, arc, cap, secondaryDamageMultiplier, secondaryCritFactor);
    }

    // Lunge params for the resolved swing, from either the light (top-level) fields
    // or the heavy profile.
    public (float speed, float duration, AnimationCurve curve) GetLunge(bool isHeavy) => isHeavy
        ? (heavy.lungeSpeed, heavy.lungeDuration, heavy.lungeCurve)
        : (lungeSpeed, lungeDuration, lungeCurve);

    // Spawn this tool's swing VFX (light or heavy). Art with an ISwingEffectAnchor
    // (SwingEffectOrienter) is fitted to the strike cone -- its leading edge on the
    // cone's rim, `reach` along the aim -- and follows `anchor` (the wielder's aim
    // origin, the cone's apex) while it plays. Legacy art without one spawns at
    // origin + dir * reach, free-rotated along the aim + angle offset, and stays put.
    // `reach` is the swing's final StrikeShape.range (after any IStrikeModifiers).
    // Safe no-op when the resolved profile has no swing effect.
    public void SpawnSwingEffect(Vector2 origin, Vector2 dir, bool mirrorSweep, Transform anchor, bool isHeavy, float reach)
    {
        GameObject prefab = isHeavy ? heavy.swingEffectPrefab : swingEffectPrefab;
        float angleOffset = isHeavy ? heavy.swingEffectAngleOffset : swingEffectAngleOffset;
        if (prefab == null) return;

        if (prefab.TryGetComponent(out ISwingEffectAnchor _))
        {
            GameObject effect = Instantiate(prefab, origin, Quaternion.identity);
            effect.GetComponent<ISwingEffectAnchor>().Orient(dir, mirrorSweep, anchor, reach);
            return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Instantiate(prefab, origin + dir * reach, Quaternion.Euler(0f, 0f, angle + angleOffset));
    }
}

// Damage scaling/knockback/effects/lunge for a charged (heavy) release. Mirrors
// Tool's light-attack fields (which stay top-level/flat to avoid re-entering
// existing weapon data) so a weapon's heavy attack can be tuned fully
// independently. It shares the weapon's damage roll, scaled by damageCoefficient;
// the strike cone shares the light range; arc and cap override it only when set.
[System.Serializable]
public class AttackProfile
{
    [Tooltip("Heavy damage as a multiple of weapon damage (1.4 = 140%). 0 = deals no damage.")]
    public float damageCoefficient;
    [Tooltip("Flat damage added after the coefficient, so the heavy still bites with a weak weapon.")]
    public float flatBonus;
    public float targetKnockback;
    public float selfKnockback;
    [Tooltip("Strike cone angle for the heavy swing. 0 = same as the light swing.")]
    public float arcAngle;
    [Tooltip("Target cap for the heavy swing. 0 = same as the light swing.")]
    public int maxTargets;
    public GameObject swingEffectPrefab;
    public float swingEffectAngleOffset;
    public float lungeSpeed;
    public float lungeDuration;
    public AnimationCurve lungeCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, 0f, 0f));
}

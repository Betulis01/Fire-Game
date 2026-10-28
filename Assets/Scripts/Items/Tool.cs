using UnityEngine;

// Capability: this item is a tool/weapon with combat-ish stats. Data only for
// now (chopping/attacking behaviour comes later); sits next to Burnable as
// another item capability in the composition model.
public class Tool : MonoBehaviour
{

    public ToolKind kind = ToolKind.Fist;   // gates what this tool can damage
    public float damage = 1f;

    [Tooltip("Melee: the strike cone's radius -- the exact max reach from the wielder's " +
             "aim origin. Ranged: the base (uncharged) shot distance.")]
    public float range = 1f;

    [Header("Strike Cone (melee)")]
    [Tooltip("Full opening angle of the strike cone, in degrees, centred on the aim.")]
    [Range(1f, 360f)] public float arcAngle = 90f;

    [Tooltip("Most entities one swing can hit: the primary plus secondaries. Skill-tree " +
             "passives (IStrikeModifier) can raise it.")]
    [Min(1)] public int maxTargets = 2;

    [Tooltip("Damage multiplier for secondary targets (1 = same as the primary).")]
    [Min(0f)] public float secondaryDamageMultiplier = 1f;

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

    // The hit-affecting stats bundled for a swing, from either the light (top-level)
    // fields or the heavy profile.
    public AttackData GetAttack(bool isHeavy) => isHeavy
        ? new AttackData(heavy.damage, kind, heavy.targetKnockback, heavy.selfKnockback)
        : new AttackData(damage, kind, targetKnockback, selfKnockback);

    // The strike cone for a swing, before the wielder's IStrikeModifiers. A heavy
    // swing uses the heavy profile's arc/cap where set (0 = same as light).
    public StrikeShape GetStrike(bool isHeavy)
    {
        float arc = isHeavy && heavy.arcAngle > 0f ? heavy.arcAngle : arcAngle;
        int cap = isHeavy && heavy.maxTargets > 0 ? heavy.maxTargets : maxTargets;
        return new StrikeShape(range, arc, cap, secondaryDamageMultiplier);
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

// Damage/knockback/effects/lunge for a charged (heavy) release. Mirrors Tool's
// light-attack fields (which stay top-level/flat to avoid re-entering existing
// weapon data) so a weapon's heavy attack can be tuned fully independently. The
// strike cone shares the light range; arc and cap override it only when set.
[System.Serializable]
public class AttackProfile
{
    public float damage;
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

using UnityEngine;

// Capability: this item is a tool/weapon with combat-ish stats. Data only for
// now (chopping/attacking behaviour comes later); sits next to Burnable as
// another item capability in the composition model.
public class Tool : MonoBehaviour
{

    public ToolKind kind = ToolKind.Fist;   // gates what this tool can damage
    public float damage = 1f;
    public float range = 1f;        // how far in front of the wielder the strike lands

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

    // The hit-affecting stats bundled for a swing, from either the light (top-level)
    // fields or the heavy profile.
    public AttackData GetAttack(bool isHeavy) => isHeavy
        ? new AttackData(heavy.damage, kind, heavy.targetKnockback, heavy.selfKnockback)
        : new AttackData(damage, kind, targetKnockback, selfKnockback);

    // Lunge params for the resolved swing, from either the light (top-level) fields
    // or the heavy profile.
    public (float speed, float duration, AnimationCurve curve) GetLunge(bool isHeavy) => isHeavy
        ? (heavy.lungeSpeed, heavy.lungeDuration, heavy.lungeCurve)
        : (lungeSpeed, lungeDuration, lungeCurve);

    // Spawn this tool's swing VFX (light or heavy). Directional art (an
    // ISwingEffectAnchor on the prefab, e.g. SwordSwingEffectOrienter or
    // AxeSwingEffectOrienter) is anchored and oriented however that implementation
    // sees fit, then follows `anchor` (the wielder) while it plays. Legacy art
    // without one spawns where the strike will land (origin + dir * range),
    // free-rotated along the aim + angle offset, and stays put. Safe no-op when the
    // resolved profile has no swing effect.
    public void SpawnSwingEffect(Vector2 origin, Vector2 dir, bool mirrorSweep, Transform anchor, bool isHeavy)
    {
        GameObject prefab = isHeavy ? heavy.swingEffectPrefab : swingEffectPrefab;
        float angleOffset = isHeavy ? heavy.swingEffectAngleOffset : swingEffectAngleOffset;
        if (prefab == null) return;

        if (prefab.TryGetComponent(out ISwingEffectAnchor _))
        {
            GameObject effect = Instantiate(prefab, origin + dir * range, Quaternion.identity);
            effect.GetComponent<ISwingEffectAnchor>().Orient(dir, mirrorSweep, anchor, range);
            return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Instantiate(prefab, origin + dir * range, Quaternion.Euler(0f, 0f, angle + angleOffset));
    }
}

// Damage/knockback/effects/lunge for a charged (heavy) release. Mirrors Tool's
// light-attack fields (which stay top-level/flat to avoid re-entering existing
// weapon data) so a weapon's heavy attack can be tuned fully independently.
[System.Serializable]
public class AttackProfile
{
    public float damage;
    public float targetKnockback;
    public float selfKnockback;
    public GameObject swingEffectPrefab;
    public float swingEffectAngleOffset;
    public float lungeSpeed;
    public float lungeDuration;
    public AnimationCurve lungeCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, 0f, 0f));
}

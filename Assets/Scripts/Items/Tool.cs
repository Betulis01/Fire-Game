using UnityEngine;

// Capability: this item is a tool/weapon with combat-ish stats. Data only for
// now (chopping/attacking behaviour comes later); sits next to Burnable as
// another item capability in the composition model.
public class Tool : MonoBehaviour
{

    public ToolKind kind = ToolKind.Fist;   // gates what this tool can damage
    public float damage = 1f;
    public float swingSpeed = 1f;
    public float range = 1f;        // how far in front of the wielder the strike lands

    [Tooltip("Shove dealt to a hit target, away from the attacker (needs a Knockback " +
             "component on the target).")]
    public float targetKnockback = 3f;

    [Tooltip("Recoil dealt back to the attacker on a connecting swing (applied by the " +
             "attacker's AttackRecoil, which can scale it).")]
    public float selfKnockback = 1f;

    [Tooltip("Slash/swing VFX spawned at swing start on every swing, hit or miss. " +
             "The victim's SurfaceMaterial spray is separate and layers on top.")]
    public GameObject swingEffectPrefab;

    [Tooltip("Degrees added so the swing effect's drawn forward aligns with the aim " +
             "(+X). Art pointing right = 0, up = -90.")]
    public float swingEffectAngleOffset;

    [Tooltip("Contact VFX for a connecting hit, spawned by the victim's SurfaceHitEffect " +
             "and tinted by its SurfaceMaterial (blood red on flesh, ...). The swing " +
             "effect above plays regardless of connecting.")]
    public GameObject hitEffectPrefab;

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

    // The hit-affecting stats bundled for a swing (damage/positioning live separately).
    public AttackData Attack => new AttackData(damage, kind, targetKnockback, selfKnockback, hitEffectPrefab);

    // Spawn this tool's swing VFX. Directional art (a SwingEffectOrienter on the
    // prefab) is anchored at the swing origin, rotated to the aim, mirrored for
    // the off-hand sweep, and follows `anchor` (the wielder) while it plays.
    // Legacy art without one spawns where the strike will land (origin + dir *
    // range), free-rotated along the aim + angle offset, and stays put.
    // Safe no-op when the tool has no swing effect.
    public void SpawnSwingEffect(Vector2 origin, Vector2 dir, bool mirrorSweep = false, Transform anchor = null)
    {
        if (swingEffectPrefab == null) return;

        if (swingEffectPrefab.TryGetComponent(out SwingEffectOrienter _))
        {
            GameObject effect = Instantiate(swingEffectPrefab, origin, Quaternion.identity);
            effect.GetComponent<SwingEffectOrienter>().Orient(dir, mirrorSweep, anchor);
            return;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Instantiate(swingEffectPrefab, origin + dir * range,
                    Quaternion.Euler(0f, 0f, angle + swingEffectAngleOffset));
    }
}

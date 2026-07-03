using UnityEngine;

// Attacker-side reactor: a brief freeze whenever this entity lands a hit. Lives on the
// attacker (the player); Hitbox notifies it through IAttackReactor, so it needs no
// wiring into the hit pipeline. Scaled by the attack's targetKnockback rather than
// damage, since knockback is the designer-tuned "how much oomph" value and stays sane
// even if damage numbers grow much larger later.
public class HitStopOnHit : MonoBehaviour, IAttackReactor
{
    [Tooltip("Freeze seconds added per point of the attack's targetKnockback.")]
    public float perKnockback = 0.02f;
    [Tooltip("Freeze duration cap per connecting hit, regardless of knockback.")]
    public float maxDuration = 0.15f;

    public void OnDealtHit(in HitInfo hit)
    {
        if (HitStop.Instance == null) return;
        float duration = Mathf.Min(maxDuration, hit.targetKnockback * perKnockback);
        HitStop.Instance.Stop(duration);
    }
}

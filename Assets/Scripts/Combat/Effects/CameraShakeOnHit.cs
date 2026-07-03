using UnityEngine;

// Attacker-side reactor: a tiny camera shake whenever this entity lands a hit. Lives
// on the attacker (the player); Hitbox notifies it through IAttackReactor, so it
// needs no wiring into the hit pipeline. Scaled by the attack's targetKnockback rather
// than damage, since knockback is the designer-tuned "how much oomph" value and stays
// sane even if damage numbers grow much larger later.
public class CameraShakeOnHit : MonoBehaviour, IAttackReactor
{
    [Tooltip("Trauma added to the camera per point of the attack's targetKnockback.")]
    public float perKnockback = 0.14f;
    [Tooltip("Trauma cap per connecting hit, regardless of knockback.")]
    public float maxAmount = 1f;

    public void OnDealtHit(in HitInfo hit)
    {
        if (CameraShake.Instance == null) return;
        float amount = Mathf.Min(maxAmount, hit.targetKnockback * perKnockback);
        CameraShake.Instance.Shake(amount);
    }
}

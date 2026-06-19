using UnityEngine;

// Attacker-side reactor: a tiny camera shake whenever this entity lands a hit. Lives
// on the attacker (the player); Hitbox notifies it through IAttackReactor, so it
// needs no wiring into the hit pipeline.
public class CameraShakeOnHit : MonoBehaviour, IAttackReactor
{
    [Tooltip("Trauma added to the camera per connecting hit.")]
    public float amount = 0.35f;

    public void OnDealtHit(in HitInfo hit)
    {
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(amount);
    }
}

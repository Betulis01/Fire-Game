using UnityEngine;

// Attacker-side reactor: a small camera jolt the instant a lunge fires (windup),
// before it's known whether the swing will connect. Lives on the attacker; only
// notified for swings whose AttackLunge actually starts (see HitResolution.NotifySwing).
// Flat and small by design (unlike CameraShakeOnHit's knockback scaling) — the
// anticipation snap should read the same regardless of weapon power.
public class CameraShakeOnSwing : MonoBehaviour, IAttackSwingReactor
{
    [Tooltip("Trauma added on swing start.")]
    public float amount = 0.08f;

    public void OnSwing()
    {
        if (CameraShake.Instance == null) return;
        CameraShake.Instance.Shake(amount);
    }
}

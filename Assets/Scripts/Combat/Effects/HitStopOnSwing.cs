using UnityEngine;

// Attacker-side reactor: a very brief freeze the instant a lunge fires (windup),
// before it's known whether the swing will connect. Lives on the attacker; only
// notified for swings whose AttackLunge actually starts (see HitResolution.NotifySwing).
// Flat and small by design (unlike HitStopOnHit's knockback scaling) — the
// anticipation snap should read the same regardless of weapon power.
public class HitStopOnSwing : MonoBehaviour, IAttackSwingReactor
{
    [Tooltip("Freeze seconds on swing start.")]
    public float duration = 0.03f;

    public void OnSwing()
    {
        if (HitStop.Instance == null) return;
        HitStop.Instance.Stop(duration);
    }
}

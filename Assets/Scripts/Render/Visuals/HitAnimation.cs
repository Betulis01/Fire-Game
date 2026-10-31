using UnityEngine;

// Victim-side reactor: fires the Animator's "Hit" trigger whenever this entity takes a
// hit, which plays its one-shot hit clip. Hurtbox discovers it through IHitReactor,
// so it needs no wiring into the hit pipeline.
public class HitAnimation : MonoBehaviour, IHitReactor
{
    static readonly int HitTrigger = Animator.StringToHash("Hit");

    [Tooltip("Animator to drive. Defaults to the Animator on this GameObject, else the " +
             "prop's art (PropArt).")]
    [SerializeField] Animator animator;

    void Awake()
    {
        if (animator == null && !TryGetComponent(out animator) && TryGetComponent(out PropArt art))
            animator = art.Animator;
    }

    public void OnHit(in HitInfo hit)
    {
        if (animator != null) animator.SetTrigger(HitTrigger);
    }
}

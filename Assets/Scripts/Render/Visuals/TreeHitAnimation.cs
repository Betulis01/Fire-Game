using UnityEngine;

// Victim-side reactor: fires the Animator's "Hit" trigger whenever this entity takes a
// hit, which plays the tree's one-shot hit clip. Hurtbox discovers it through
// IHitReactor, so it needs no wiring into the hit pipeline.
[RequireComponent(typeof(Animator))]
public class TreeHitAnimation : MonoBehaviour, IHitReactor
{
    static readonly int HitTrigger = Animator.StringToHash("Hit");

    [Tooltip("Animator to drive. Defaults to the Animator on this GameObject.")]
    [SerializeField] Animator animator;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void OnHit(in HitInfo hit)
    {
        if (animator != null) animator.SetTrigger(HitTrigger);
    }
}

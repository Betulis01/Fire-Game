using UnityEngine;

// Drives the animator's "Colliding" bool from an entity's physical body overlapping
// this object: true for as long as at least one qualifying entity is touching it,
// false once the last one leaves. The animator plays the non-looping "collide" clip
// while true -- holding on its last frame if the touch outlasts the clip -- and
// snaps back to idle (stopping the clip) the instant it goes false. Unlike
// HitAnimation (a combat hit via Hitbox/Hurtbox/IHitReactor), this reacts to a
// plain physical overlap, so it lives on its own trigger collider instead.
public class CollideAnimation : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] LayerMask entityLayers;

    int overlapCount;   // multiple entities can overlap at once; only the last exit should stop it

    void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & entityLayers.value) == 0) return;
        overlapCount++;
        animator.SetBool("Colliding", true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & entityLayers.value) == 0) return;
        overlapCount = Mathf.Max(0, overlapCount - 1);
        if (overlapCount == 0) animator.SetBool("Colliding", false);
    }
}

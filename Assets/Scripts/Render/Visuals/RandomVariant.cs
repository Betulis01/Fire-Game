using UnityEngine;

// Shows one random variant of a prop's art and keeps it. A variant is a frame of an
// untagged multi-frame Aseprite file (Rock, Stick, Iron Stone): the importer plays
// such a file's frames as one looping clip on the art's own Animator. This starts
// that clip at a random point, lets it write that frame to every layer, then turns
// the Animator off so it stays. Nothing is linked: adding a variant frame in
// Aseprite is all it takes.
//
// The Animator stays off for the item's life, so the frame survives the item being
// stowed and dropped again (re-enabling an Animator would restart its clip).
[RequireComponent(typeof(PropArt))]
public class RandomVariant : MonoBehaviour
{
    void Start()   // the Animator has its default state by now
    {
        Animator animator = GetComponent<PropArt>().Animator;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, Random.value);
        animator.Update(0f);
        animator.enabled = false;
    }
}

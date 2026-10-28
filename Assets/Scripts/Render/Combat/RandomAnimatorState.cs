using UnityEngine;

// Plays one of the Animator controller's clips, picked at random on spawn -- drawn
// variants of a one-shot effect (the three hit blood sprays), so repeated hits don't
// look stamped out. Nothing to fill in: every clip in the controller is a variant,
// each played through the state named after it (Unity's default name for a state
// made by dropping a clip in). Sits beside DestroyAfterAnimation, which then waits
// out whichever state was picked.
[RequireComponent(typeof(Animator))]
public class RandomAnimatorState : MonoBehaviour
{
    void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator.runtimeAnimatorController == null) return;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips.Length == 0) return;

        int state = Animator.StringToHash(clips[Random.Range(0, clips.Length)].name);
        if (animator.HasState(0, state)) animator.Play(state, 0, 0f);
        else Debug.LogWarning($"[RandomAnimatorState] No state named after a clip on {name}.", this);
    }
}

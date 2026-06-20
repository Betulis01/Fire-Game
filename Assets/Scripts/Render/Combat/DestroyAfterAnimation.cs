using System.Collections;
using UnityEngine;

// Generic one-shot effect cleanup: waits out the Animator's current clip, then destroys
// the GameObject. Lives on spawned VFX prefabs (hit sparks, slash effects, ...) that
// otherwise have no lifetime of their own. Uses scaled time so it freezes correctly
// alongside HitStop.
[RequireComponent(typeof(Animator))]
public class DestroyAfterAnimation : MonoBehaviour
{
    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(DestroyAfterClip());
    }

    IEnumerator DestroyAfterClip()
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        float length = clips.Length > 0 ? clips[0].length : 0f;
        yield return new WaitForSeconds(length);
        Destroy(gameObject);
    }
}

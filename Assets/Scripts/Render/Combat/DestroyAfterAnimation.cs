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
        // Let the Animator enter its state first, then read the state's length —
        // unlike the raw clip asset's length, it accounts for the state's speed
        // multiplier (a 0.5s clip at speed 4 reports 0.125s).
        yield return null;
        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        Destroy(gameObject);
    }
}

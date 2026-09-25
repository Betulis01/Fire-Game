using System;
using System.Collections;
using UnityEngine;

// Held-item swing animation: flips the item's own sprite through `frames` in a loop
// while its wielder is swinging, then restores the resting sprite. This is the item
// animating in the hand -- separate from the swing effect (Tool.SpawnSwingEffect),
// which spawns its own world object. Started from WeaponUse.OnAttackSwing.
//
// Only swaps SpriteRenderer.sprite, so the west-facing flipX (Hands) and the
// front/back sortingOrder (HeldItemSorter) on the same renderer are untouched.
[RequireComponent(typeof(SpriteRenderer))]
public class HeldSwingAnimation : MonoBehaviour
{
    [Tooltip("Frames played in order, looping, while the swing lasts.")]
    [SerializeField] Sprite[] frames;

    [Tooltip("Seconds each frame is shown (scaled time, so hit-stop freezes it too).")]
    [SerializeField] float frameTime = 0.05f;

    SpriteRenderer sr;
    Sprite rest;
    Coroutine playing;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    // Loop the frames for as long as `keepPlaying` holds, then go back to rest.
    // A new swing restarts from the first frame.
    public void Play(Func<bool> keepPlaying)
    {
        if (frames == null || frames.Length == 0) return;
        Stop();
        rest = sr.sprite;
        playing = StartCoroutine(Loop(keepPlaying));
    }

    IEnumerator Loop(Func<bool> keepPlaying)
    {
        for (int i = 0; keepPlaying(); i = (i + 1) % frames.Length)
        {
            sr.sprite = frames[i];
            yield return new WaitForSeconds(frameTime);
        }
        playing = null;
        sr.sprite = rest;
    }

    public void Stop()
    {
        if (playing == null) return;
        StopCoroutine(playing);
        playing = null;
        sr.sprite = rest;
    }

    // Dropped, stowed or swapped mid-swing: never leave the item on an effect frame.
    void OnDisable() => Stop();
}

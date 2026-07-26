using System.Collections.Generic;
using UnityEngine;

// Drives the player's Animator from movement. The art has discrete isometric
// facing poses (NE/SE; NW/SW are their horizontal mirrors), each with an idle and
// a walk clip. Rather than blend trees, we pick the matching state by name and
// call Animator.Play only when it changes (cheap, and never restarts a looping
// clip mid-stride). NW/SW reuse the NE/SE clips with SpriteRenderer.flipX.
//
// State names must match the AnimatorController / Aseprite tag names:
//   se_idle ne_idle  se_walk ne_walk
//   se_attack_l ne_attack_l  se_attack_r ne_attack_r
// (NW/SW reuse the NE/SE clips via flipX, same as locomotion).
//
// Attacks route through PlayAttack(side, duration, aimDir): it latches the matching
// per-hand attack state so movement can't cut the swing off.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("Animator to drive. Falls back to one on this GameObject.")]
    public Animator animator;

    [Tooltip("Parent of the hands, mirrored for west (localScale.x flipped) so the " +
             "east hand poses become west poses. Leave empty to skip hand mirroring.")]
    public Transform handRig;

    [Tooltip("Movement below this magnitude counts as idle (keeps the last facing).")]
    [SerializeField] float moveDeadzone = 0.05f;

    SpriteRenderer sr;
    Vector2 facing = Vector2.down;   // start facing the camera (south)
    string currentState;
    float handRigBaseScaleX = 1f;    // preserved X magnitude of the hand rig

    // Read-only facing state, for systems that pose off the player's direction
    // (e.g. a per-frame hand placer) without recomputing it.
    public Vector2 Facing => facing;
    public bool FlipX { get; private set; }

    // While attacking, locomotion is suppressed until the attack clip actually
    // finishes (so a short clip doesn't freeze on its last frame). attackFailsafe is
    // a generous ceiling so we can never get stuck if the state never completes.
    bool attacking;
    public bool IsAttacking => attacking;
    float attackFailsafe;
    string attackState;

    // Missing-clip states we've already warned about (see PlayAttack) — logged once
    // each rather than every attempt, since a swing that can't arm still gets retried
    // on every subsequent attack press.
    static readonly HashSet<string> warnedMissingStates = new();

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (handRig != null) handRigBaseScaleX = Mathf.Abs(handRig.localScale.x);
    }

    void Update()
    {
        Vector2 move = UserInput.Instance.Move;
        bool moving = move.sqrMagnitude > moveDeadzone * moveDeadzone;
        if (moving) facing = move;

        // Resolve facing to an isometric direction (see CardinalDir).
        (string dir, bool flip) = ResolveDir(facing);

        sr.flipX = flip;
        FlipX = flip;

        // Mirror the hand rig for west (NW/SW): localScale.x = -base flips both hand
        // positions and sprites around the player's center, turning the NE/SE hand
        // poses into NW/SW poses.
        if (handRig != null)
        {
            Vector3 s = handRig.localScale;
            s.x = flip ? -handRigBaseScaleX : handRigBaseScaleX;
            handRig.localScale = s;
        }

        // An in-progress attack owns the Animator until its clip finishes. We detect
        // completion via normalizedTime (non-looping clips count past 1.0) rather than
        // a fixed time, so the swing never sits frozen on its last frame.
        if (attacking)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            bool clipDone = st.IsName(attackState) && st.normalizedTime >= 1f;
            if (Time.time < attackFailsafe && !clipDone)
            {
                Play(attackState);
                return;
            }
            attacking = false;
        }

        Play($"{dir}_{(moving ? "run" : "idle")}");
    }

    // Play a state by name, but only when it actually changes.
    void Play(string state)
    {
        if (state == currentState) return;
        currentState = state;
        animator.Play(state);
    }

    // Combat hook: play the attack clip for the aim direction and hand. Locomotion is
    // suppressed until the clip finishes; `duration` is only a safety ceiling (the real
    // end is the clip completing). The clip's Animation Event drives the actual hit
    // (WeaponUse.OnAttackHit). NW/SW reuse the NE/SE clip via the flipX applied above.
    // facing is set to aimDir so the swing, sprite flip, and post-attack idle/walk
    // pose all agree with where the hit actually lands (WeaponUse aims the same way).
    public void PlayAttack(HandSide side, float duration, Vector2 aimDir)
    {
        facing = aimDir;
        (string dir, _) = ResolveDir(facing);

        string hand = side == HandSide.Left ? "l" : "r";
        string state = $"{dir}_attack_{hand}";

        // No clip for this direction yet (art pending) -- arming the swing anyway
        // would set attacking=true with no way for it to ever reach clipDone, locking
        // out every future attack until the failsafe (which WeaponUse sets to a full
        // simulated hour for a charge-holdable swing).
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (warnedMissingStates.Add(state))
                Debug.LogWarning($"[PlayerAnimator] No '{state}' clip yet — skipping swing so it can't lock movement/attacks.", this);
            return;
        }

        attackState = state;
        attacking = true;
        attackFailsafe = Time.time + Mathf.Max(duration, 3f);   // safety ceiling only
        currentState = null;   // force the next Play to switch
        animator.speed = 1f;   // in case a prior charge left this paused
    }

    // Charged attacks freeze the clip mid-playback while held, then resume it on
    // release. No-op outside an active attack so a stray call can't unpause locomotion.
    public void PauseAttack() { if (attacking) animator.speed = 0f; }
    public void ResumeAttack() { animator.speed = 1f; }

    static (string dir, bool flip) ResolveDir(Vector2 v) => CardinalDir.Resolve(v);
}

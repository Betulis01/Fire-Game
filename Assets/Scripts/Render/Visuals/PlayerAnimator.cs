using System.Collections.Generic;
using UnityEngine;

// Drives the player's Animator from movement. The art has discrete isometric
// facing poses (NE/SE; NW/SW are their horizontal mirrors), each with an idle and
// a walk clip. Rather than blend trees, we pick the matching state by name and
// call Animator.Play only when it changes (cheap, and never restarts a looping
// clip mid-stride). NW/SW reuse the NE/SE clips with SpriteRenderer.flipX.
//
// State names must match the AnimatorController / Aseprite tag names:
//   se_idle ne_idle  se_run ne_run
//   se_attack_l ne_attack_l  se_attack_r ne_attack_r
// (NW/SW reuse the NE/SE clips via flipX, same as locomotion).
//
// Attacks route through PlayAttack(side, duration, aimDir): it latches the matching
// per-hand attack state so movement can't cut the swing off.
// Order -100 so LateUpdate mirrors the hand anchors before HeldItemRotationFilter
// (default order 0) reads their rotation, and after the Animator writes them.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("Animator to drive. Falls back to one on this GameObject.")]
    public Animator animator;

    [Tooltip("Parent of the hand anchors. Each child is mirrored for west-facing " +
             "(NW/SW). Leave empty to skip hand mirroring.")]
    public Transform handRig;

    [Tooltip("Movement below this magnitude counts as idle (keeps the last facing).")]
    [SerializeField] float moveDeadzone = 0.05f;

    SpriteRenderer sr;
    Vector2 facing = Vector2.down;   // start facing the camera (south)
    string currentState;

    // One hand anchor's mirror bookkeeping: the Animator's unmirrored pose, plus the
    // pose we last wrote to it. Comparing against what we wrote tells us whether the
    // Animator refreshed the anchor this frame — a clip with no HandRig curves (the
    // sprite-only attack stubs) leaves it untouched, and reflecting our own output
    // again would make it oscillate.
    class HandAnchor
    {
    
        public Transform t;
        public Vector3 sourcePos, writtenPos;
        public Quaternion sourceRot = Quaternion.identity, writtenRot = Quaternion.identity;
        public bool primed;
    }

    HandAnchor[] handAnchors = System.Array.Empty<HandAnchor>();

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

        if (handRig != null)
        {
            handAnchors = new HandAnchor[handRig.childCount];
            for (int i = 0; i < handAnchors.Length; i++)
                handAnchors[i] = new HandAnchor { t = handRig.GetChild(i) };
        }
    }

    // Mirror the hands for west-facing by reflecting each anchor's local pose, rather
    // than negating the rig's scale. A negative scale makes every descendant's matrix
    // a reflection, which a quaternion cannot represent: Unity fakes it with a 180°
    // flip on X/Y plus a negative scale, which corrupts the rotation of anything held.
    // Reflecting position (x -> -x) and rotation (conjugate about X, i.e. the same
    // pose spun the other way) keeps the whole chain proper rotations, so a held
    // item's own rotation and scale come through the flip untouched.
    void LateUpdate()
    {
        foreach (HandAnchor a in handAnchors)
        {
            if (a.t == null) continue;

            Vector3 pos = a.t.localPosition;
            Quaternion rot = a.t.localRotation;

            // Anything other than our own last output means the Animator wrote a fresh
            // pose, which becomes the new source to mirror from.
            if (!a.primed || pos != a.writtenPos || rot != a.writtenRot)
            {
                a.sourcePos = pos;
                a.sourceRot = rot;
            }

            Vector3 outPos = a.sourcePos;
            Quaternion outRot = a.sourceRot;
            if (FlipX)
            {
                outPos.x = -outPos.x;
                outRot = new Quaternion(outRot.x, -outRot.y, -outRot.z, outRot.w);
            }

            a.t.localPosition = outPos;
            a.t.localRotation = outRot;
            a.writtenPos = outPos;
            a.writtenRot = outRot;
            a.primed = true;
        }
    }

    void Update()
    {
        Vector2 move = UserInput.Instance.Move;
        bool moving = move.sqrMagnitude > moveDeadzone * moveDeadzone;

        // An in-progress attack owns the Animator until its clip finishes. We detect
        // completion via normalizedTime (non-looping clips count past 1.0) rather than
        // a fixed time, so the swing never sits frozen on its last frame.
        if (attacking)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            bool clipDone = st.IsName(attackState) && st.normalizedTime >= 1f;
            if (Time.time >= attackFailsafe || clipDone) attacking = false;
        }

        // Mid-attack, facing stays latched to the aim PlayAttack set: the attack
        // state's direction and _l/_r suffix were picked from it, so letting movement
        // re-derive the flip here would mirror (or un-mirror) the swing onto the
        // wrong side -- e.g. running east while swinging west.
        if (moving && !attacking) facing = move;
        (string dir, bool flip) = ResolveDir(facing);
        ApplyFlip(flip);

        if (attacking)
        {
            Play(attackState);
            return;
        }

        Play($"{dir}_{(moving ? "run" : "idle")}");
    }

    // Mirror the body for west-facing (NW/SW). The hand rig's west mirror is applied arithmetically in LateUpdate
    // (after the Animator writes the anchors), not by negating scale here -- a
    // negative scale makes every descendant's matrix a reflection, which a quaternion
    // can't represent and which corrupts held items' rotation.
    void ApplyFlip(bool flip)
    {
        sr.flipX = flip;
        FlipX = flip;
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
        (string dir, bool flip) = ResolveDir(facing);

        // The clip suffix picks which HandRig anchor gets animated (_l ->
        // LeftHand, _r -> RightHand), and Hands.Anchor() pins the held item to
        // that same anchor unswapped during an attack -- so the suffix must
        // track `side` directly, not the west-facing mirror. Mirroring (NW/SW)
        // still flips the drawn art and both anchors' positions uniformly via
        // flipX/LateUpdate, which is all that's needed for it to read correctly.
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
        // Flip now rather than on the next Update, so this frame's anchor mirror and
        // Hands.Anchor already agree with the suffix picked above.
        ApplyFlip(flip);
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

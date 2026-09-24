using System.Collections.Generic;
using UnityEngine;

// Drives the player's Animator by state name (no blend trees). The art has NE/SE
// poses; NW/SW reuse them mirrored via SpriteRenderer.flipX. Play() only switches
// when the state changes, so a looping clip is never restarted mid-stride.
//
// State names must match the AnimatorController:
//   se_idle ne_idle  se_run ne_run
//   se_attack_l ne_attack_l  se_attack_r ne_attack_r
//
// Attacks go through PlayAttack(side, duration, aimDir), which latches the attack
// state until its clip finishes so movement can't cut the swing off.
//
// Order -100: LateUpdate mirrors the hand anchors after the Animator writes them
// and before HeldItemRotationFilter (default order 0) reads them.
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

    // One hand anchor's mirror bookkeeping: the Animator's unmirrored pose, plus the
    // pose we last wrote. If the anchor still holds what we wrote, the Animator
    // didn't touch it this frame (e.g. a clip with no HandRig curves), and mirroring
    // it again would make it oscillate.
    class HandAnchor
    {
        public Transform t;
        public Vector3 sourcePos, writtenPos;
        public Quaternion sourceRot = Quaternion.identity, writtenRot = Quaternion.identity;
        public bool primed;
    }

    SpriteRenderer sr;
    HandAnchor[] handAnchors = System.Array.Empty<HandAnchor>();
    Vector2 facing = Vector2.down;   // start facing the camera
    string currentState;

    // An attack owns the Animator until its clip finishes; attackFailsafe is only a
    // ceiling so a state that never completes can't lock us forever.
    bool attacking;
    string attackState;
    float attackFailsafe;

    // Missing attack states already warned about, so each logs once.
    static readonly HashSet<string> warnedMissingStates = new();

    public Vector2 Facing => facing;
    public bool FlipX { get; private set; }
    public bool IsAttacking => attacking;

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

    void Update()
    {
        Vector2 move = UserInput.Instance.Move;
        bool moving = move.sqrMagnitude > moveDeadzone * moveDeadzone;

        // normalizedTime passes 1 when a non-looping clip ends, so the swing never
        // sits frozen on its last frame.
        if (attacking)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            bool clipDone = st.IsName(attackState) && st.normalizedTime >= 1f;
            if (clipDone || Time.time >= attackFailsafe) attacking = false;
        }

        // Mid-attack, facing stays latched to the aim: the attack state's direction
        // and hand suffix were picked from it, so movement mustn't re-flip the swing.
        if (moving && !attacking) facing = move;
        (string dir, bool flip) = CardinalDir.Resolve(facing);
        ApplyFlip(flip);

        Play(attacking ? attackState : $"{dir}_{(moving ? "run" : "idle")}");
    }

    // Mirror the hands for west-facing by reflecting each anchor's local pose
    // (x -> -x, rotation conjugated about X) instead of negating the rig's scale.
    // A negative scale makes every descendant a reflection, which a quaternion can't
    // represent -- Unity fakes it with a 180° flip plus negative scale, corrupting
    // held items' rotation.
    void LateUpdate()
    {
        foreach (HandAnchor a in handAnchors)
        {
            if (a.t == null) continue;

            Vector3 pos = a.t.localPosition;
            Quaternion rot = a.t.localRotation;

            // Anything other than our own last output is a fresh Animator pose.
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

            a.t.localPosition = a.writtenPos = outPos;
            a.t.localRotation = a.writtenRot = outRot;
            a.primed = true;
        }
    }

    // Mirror the body for NW/SW. The hand rig is mirrored separately in LateUpdate.
    void ApplyFlip(bool flip)
    {
        sr.flipX = flip;
        FlipX = flip;
    }

    void Play(string state)
    {
        if (state == currentState) return;
        currentState = state;
        animator.Play(state);
    }

    // Play the attack clip for the aim direction and hand. `duration` is only the
    // failsafe ceiling; the clip finishing ends the attack, and its Animation Events
    // drive the hit (WeaponUse.OnAttackHit). Facing follows the aim so the swing and
    // the post-attack pose agree with where the hit lands.
    public void PlayAttack(HandSide side, float duration, Vector2 aimDir)
    {
        facing = aimDir;
        (string dir, bool flip) = CardinalDir.Resolve(facing);

        // Mirroring turns a right-hand swing into a left-hand one, so the suffix
        // flips with it: left hand facing SW plays se_attack_r mirrored, NW plays
        // ne_attack_r mirrored. Hands.Anchor swaps anchors on the same mirror, so the
        // held item sits on the anchor the clip animates (_l -> LeftHand, _r -> RightHand).
        bool left = side == HandSide.Left;
        string state = $"{dir}_attack_{(left != flip ? "l" : "r")}";

        // Arming a swing with no clip would never reach clipDone and would lock out
        // attacks until the failsafe (an hour for a charge-holdable swing).
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (warnedMissingStates.Add(state))
                Debug.LogWarning($"[PlayerAnimator] No '{state}' clip yet — skipping swing so it can't lock movement/attacks.", this);
            return;
        }

        attackState = state;
        attacking = true;
        attackFailsafe = Time.time + Mathf.Max(duration, 3f);
        ApplyFlip(flip);       // now, so Hands.Anchor agrees with the suffix this frame
        currentState = null;   // force the next Play to switch
        animator.speed = 1f;   // in case a prior charge left it paused
    }

    // Charged attacks freeze the clip while held and resume on release. Pause is a
    // no-op outside an attack so a stray call can't freeze locomotion.
    public void PauseAttack() { if (attacking) animator.speed = 0f; }
    public void ResumeAttack() { animator.speed = 1f; }
}

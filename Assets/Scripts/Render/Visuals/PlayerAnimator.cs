using System.Collections.Generic;
using UnityEngine;

// Drives the player's Animator by state name (no blend trees). The art has NE/SE
// poses; NW/SW reuse them mirrored via SpriteRenderer.flipX. Play() only switches
// when the state changes, so a looping clip is never restarted mid-stride.
//
// The aim (mouse, or gamepad right stick) picks the facing for every clip; movement
// only decides whether the run plays forward or backwards (moving away from the aim
// backpedals with the *_run_backwards states, which play the run clip at speed -1).
//
// State names must match the AnimatorController:
//   se_idle ne_idle  se_run ne_run  se_run_backwards ne_run_backwards
//   se_attack_l ne_attack_l  se_attack_r ne_attack_r  (generic, any tool)
//   {se,ne}_attack_{style}_{l,r}  (per Tool.AttackStyle: slash = swords, hack = axes)
//   se_hit ne_hit  (played from OnHit when a hit lands; never interrupts a swing)
//   whirlwind      (a spell: one full turn, drawn as a spin; see PlayCast)
//
// Attacks go through PlayAttack(side, duration, aimDir, style), which latches the
// attack state until its clip finishes so movement can't cut the swing off. Spells
// go through PlayCast(state, loops, mirrored), which latches the same way for
// `loops` plays of a looping clip.
//
// Order -100: LateUpdate mirrors the hand anchors after the Animator writes them
// and before HeldItemRotationFilter (default order 0) reads them.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour, IHitReactor
{
    [Tooltip("Animator to drive. Falls back to one on this GameObject.")]
    public Animator animator;

    [Tooltip("Parent of the hand anchors. Each child is mirrored for west-facing " +
             "(NW/SW). Leave empty to skip hand mirroring.")]
    public Transform handRig;

    [Tooltip("Movement below this magnitude counts as idle.")]
    [SerializeField] float moveDeadzone = 0.05f;

    [Tooltip("How long the hit clip holds before locomotion resumes.")]
    [SerializeField] float hitDuration = 0.2f;

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
    Camera cam;
    WeaponUse weaponUse;
    HandAnchor[] handAnchors = System.Array.Empty<HandAnchor>();
    Vector2 facing = Vector2.down;   // start facing the camera
    string currentState;

    // An attack owns the Animator until its clip finishes; attackFailsafe is only a
    // ceiling so a state that never completes can't lock us forever.
    bool attacking;
    string attackState;
    string attackStyle;   // the swing's Tool.AttackStyle, kept so RetargetAttack stays in the same clip family
    float attackFailsafe;
    int attackLoops = 1;  // plays of the clip before the attack ends (PlayCast spins loop it)
    bool casting;         // the latched state is a spell (PlayCast), not a weapon swing

    // Hit reaction: holds until hitUntil, then locomotion resumes.
    float hitUntil;

    // Missing attack states already warned about, so each logs once.
    static readonly HashSet<string> warnedMissingStates = new();

    public Vector2 Facing => facing;
    public bool FlipX { get; private set; }
    public bool IsAttacking => attacking;
    public bool IsCasting => attacking && casting;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        weaponUse = GetComponent<WeaponUse>();

        if (handRig != null)
        {
            handAnchors = new HandAnchor[handRig.childCount];
            for (int i = 0; i < handAnchors.Length; i++)
                handAnchors[i] = new HandAnchor { t = handRig.GetChild(i) };
        }
    }

    // A landed hit (Hurtbox fans it out to every IHitReactor on this entity) starts the
    // hit reaction, whether or not it ends up doing damage.
    public void OnHit(in HitInfo hit) => hitUntil = Time.time + hitDuration;

    void Update()
    {
        Vector2 move = UserInput.Instance.Move;
        bool moving = move.sqrMagnitude > moveDeadzone * moveDeadzone;

        // normalizedTime passes 1 when a non-looping clip ends, so the swing never
        // sits frozen on its last frame.
        if (attacking)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            bool clipDone = st.IsName(attackState) && st.normalizedTime >= attackLoops;
            if (clipDone || Time.time >= attackFailsafe) attacking = false;
        }

        // Facing follows the aim. Mid-attack it stays latched to the aim the swing
        // picked (PlayAttack/RetargetAttack), so the swing isn't re-flipped between
        // its events.
        // Aimed from WeaponUse.Origin (the body's center) rather than the feet pivot,
        // so the N/S split runs through the middle of the sprite and matches the swing.
        if (!attacking)
        {
            if (cam == null) cam = Camera.main;
            Vector2 origin = weaponUse != null ? weaponUse.Origin : (Vector2)transform.position;
            facing = UserInput.Instance.AimDirection(origin, cam);
        }
        (string dir, bool flip) = CardinalDir.Resolve(facing);
        ApplyFlip(flip);

        // Moving against the aim backpedals; perpendicular counts as forward.
        bool backwards = Vector2.Dot(move, facing) < 0f;

        if (attacking) Play(attackState);
        else if (Time.time < hitUntil) Play($"{dir}_hit");
        else if (!moving) Play($"{dir}_idle");
        else Play(backwards ? $"{dir}_run_backwards" : $"{dir}_run");
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

        // X_run <-> X_run_backwards share a clip, so keep the stride's phase and just
        // reverse it instead of snapping to frame 0.
        string runBase = RunBase(state);
        if (runBase != null && currentState != null && runBase == RunBase(currentState))
        {
            float t = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            currentState = state;
            animator.Play(state, 0, t - Mathf.Floor(t));
            return;
        }

        currentState = state;
        animator.Play(state);
    }

    // "ne_run_backwards" -> "ne_run"; null for non-run states.
    static string RunBase(string state)
    {
        if (state.EndsWith("_run")) return state;
        if (state.EndsWith("_run_backwards")) return state.Substring(0, state.Length - "_backwards".Length);
        return null;
    }

    // Mirroring turns a right-hand swing into a left-hand one, so the suffix flips
    // with it: left hand facing SW plays se_attack_r mirrored, NW plays ne_attack_r
    // mirrored. Hands.Anchor swaps anchors on the same mirror, so the held item sits
    // on the anchor the clip animates (_l -> LeftHand, _r -> RightHand).
    // A style (Tool.AttackStyle) picks {dir}_attack_{style}_{l|r}; with no style, or
    // no such state, it's the generic {dir}_attack_{l|r}.
    string AttackState(HandSide side, Vector2 aimDir, string style, out bool flip)
    {
        (string dir, bool f) = CardinalDir.Resolve(aimDir);
        flip = f;
        bool left = side == HandSide.Left;
        string hand = left != flip ? "l" : "r";

        if (!string.IsNullOrEmpty(style))
        {
            string styled = $"{dir}_attack_{style}_{hand}";
            if (animator.HasState(0, Animator.StringToHash(styled))) return styled;
        }
        return $"{dir}_attack_{hand}";
    }

    // Play the attack clip for the aim direction and hand. `duration` is only the
    // failsafe ceiling; the clip finishing ends the attack, and its Animation Events
    // drive the hit (WeaponUse.OnAttackHit). Facing follows the aim so the windup
    // starts out agreeing with where the swing began aiming; RetargetAttack re-picks
    // it as the swing/hit events fire. `style` is the weapon's Tool.AttackStyle.
    public void PlayAttack(HandSide side, float duration, Vector2 aimDir, string style = null)
    {
        string state = AttackState(side, aimDir, style, out bool flip);

        // Arming a swing with no clip would never reach clipDone and would lock out
        // attacks until the failsafe (an hour for a charge-holdable swing).
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (warnedMissingStates.Add(state))
                Debug.LogWarning($"[PlayerAnimator] No '{state}' clip yet — skipping swing so it can't lock movement/attacks.", this);
            return;
        }

        facing = aimDir;
        attackState = state;
        attackStyle = style;
        attacking = true;
        attackLoops = 1;
        casting = false;
        attackFailsafe = Time.time + Mathf.Max(duration, 3f);
        ApplyFlip(flip);       // now, so Hands.Anchor agrees with the suffix this frame
        currentState = null;   // force the next Play to switch
        animator.speed = 1f;   // in case a prior charge left it paused
    }

    // Play a spell's clip `loops` times in a row (its state's clip must loop), latched
    // like an attack: movement and new swings can't cut it off. The clip itself is
    // non-directional (e.g. a full spin); `mirrored` plays it flipped, which also swaps
    // the hand anchors (Hands.Anchor), so a left-hand cast swings on the anchor the
    // clip animates. Returns false when the state doesn't exist.
    public bool PlayCast(string state, int loops, bool mirrored)
    {
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (warnedMissingStates.Add(state))
                Debug.LogWarning($"[PlayerAnimator] No '{state}' state in the Animator — can't cast.", this);
            return false;
        }

        // Update re-derives the flip from `facing` every frame, so park facing on the
        // SE/SW diagonal that resolves to the wanted mirror
        facing = mirrored ? new Vector2(-1f, -1f) : new Vector2(1f, -1f);
        attackState = state;
        attackStyle = null;
        attacking = true;
        attackLoops = Mathf.Max(1, loops);
        casting = true;
        attackFailsafe = Time.time + Mathf.Max(attackLoops * 2f, 3f);
        ApplyFlip(mirrored);
        currentState = null;
        animator.speed = 1f;
        return true;
    }

    // Re-aims the in-progress attack clip: called from the swing Animation Event
    // (WeaponUse.OnAttackSwing, or OnAttackHit for a clip without one) with the aim
    // sampled at that instant, so a
    // mouse move mid-attack can still change which directional clip plays. Jumps to
    // the equivalent normalized time in the new clip rather than restarting it -- this
    // relies on a weapon's directional clip variants sharing event timing (confirmed
    // for se/ne_attack_hack_l/r: swing @0.17, hit @0.26). Stays in the swing's style;
    // a weapon missing the target direction's clip just keeps the current one.
    public void RetargetAttack(HandSide side, Vector2 aimDir)
    {
        if (!attacking || casting) return;

        string state = AttackState(side, aimDir, attackStyle, out bool flip);
        if (state == attackState) return;

        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (warnedMissingStates.Add(state))
                Debug.LogWarning($"[PlayerAnimator] No '{state}' clip yet — keeping '{attackState}' for this swing.", this);
            return;
        }

        float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        facing = aimDir;
        attackState = state;
        ApplyFlip(flip);
        currentState = state;   // Play() below matches this already, so record it directly
        animator.Play(state, 0, normalizedTime);
    }

    // Charged attacks freeze the clip while held and resume on release. Pause is a
    // no-op outside an attack so a stray call can't freeze locomotion.
    public void PauseAttack() { if (attacking) animator.speed = 0f; }
    public void ResumeAttack() { animator.speed = 1f; }
}

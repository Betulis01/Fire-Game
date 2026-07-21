using UnityEngine;

// Drives the player's Animator from movement. The art has discrete directional
// poses (south/north/east; west is east mirrored), each with an idle and a walk
// clip. Rather than blend trees, we pick the matching state by name and call
// Animator.Play only when it changes (cheap, and never restarts a looping clip
// mid-stride). West reuses the east clips with SpriteRenderer.flipX.
//
// State names must match the AnimatorController / Aseprite tag names:
//   s_idle n_idle e_idle  s_walk n_walk e_walk
//   s_attack_l n_attack_l e_attack_l  s_attack_r n_attack_r e_attack_r
// (west reuses the east clips via flipX, same as locomotion).
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

        // Resolve facing to a cardinal direction. Horizontal wins ties so a mostly
        // sideways diagonal reads as east/west.
        (string dir, bool flip) = ResolveDir(facing);

        sr.flipX = flip;
        FlipX = flip;

        // Mirror the hand rig for west: localScale.x = -base flips both hand
        // positions and sprites around the player's center, turning the east hand
        // poses into west poses.
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

        Play($"{dir}_{(moving ? "walk" : "idle")}");
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
    // (WeaponUse.OnAttackHit). West reuses the east clip via the flipX applied above.
    // facing is set to aimDir so the swing, sprite flip, and post-attack idle/walk
    // pose all agree with where the hit actually lands (WeaponUse aims the same way).
    public void PlayAttack(HandSide side, float duration, Vector2 aimDir)
    {
        facing = aimDir;
        (string dir, _) = ResolveDir(facing);

        string hand = side == HandSide.Left ? "l" : "r";
        attackState = $"{dir}_attack_{hand}";
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

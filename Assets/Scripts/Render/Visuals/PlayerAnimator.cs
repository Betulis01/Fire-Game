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
// Attacks route through PlayAttack(side, duration): it latches the matching
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

    // While set, an attack clip is playing and locomotion states are suppressed
    // until Time.time passes this. Used by PlayAttack() (future attack clips).
    float attackUntil = -1f;
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
        string dir;
        bool flip = false;
        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y))
        {
            dir = "e";
            flip = facing.x < 0f;   // west = east mirrored
        }
        else
        {
            dir = facing.y >= 0f ? "n" : "s";
        }

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

        // An in-progress attack owns the Animator until its clip finishes.
        if (Time.time < attackUntil)
        {
            Play(attackState);
            return;
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

    // Combat hook: play the attack clip for the current facing and hand, and hold it
    // for `duration` seconds so movement input can't interrupt the swing. The clip's
    // Animation Event drives the actual hit (ToolUser.OnAttackHit). West reuses the
    // east clip via the flipX applied every frame above.
    public void PlayAttack(HandSide side, float duration)
    {
        string dir;
        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y)) dir = "e";
        else dir = facing.y >= 0f ? "n" : "s";

        string hand = side == HandSide.Left ? "l" : "r";
        attackState = $"{dir}_attack_{hand}";
        attackUntil = Time.time + duration;
        currentState = null;   // force the next Play to switch
    }
}

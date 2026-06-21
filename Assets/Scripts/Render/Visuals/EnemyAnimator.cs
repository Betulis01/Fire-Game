using UnityEngine;

// Drives the enemy's Animator from EnemyBrain.MoveDirection. Same directional-pose
// scheme and state names as PlayerAnimator (south/north/east, west mirrored via
// flipX) so the enemy can reuse the player's Animator Controller asset:
//   s_idle n_idle e_idle  s_walk n_walk e_walk  s_attack_r n_attack_r e_attack_r
// Attacks route through PlayAttack(duration, aimDir), the same latch-until-clip-done
// scheme as PlayerAnimator.PlayAttack, just without a HandSide (the enemy swings one
// weapon, not two hands) — see EnemyAttacker.
[RequireComponent(typeof(SpriteRenderer), typeof(EnemyBrain))]
public class EnemyAnimator : MonoBehaviour
{
    [Tooltip("Animator to drive. Falls back to one on this GameObject.")]
    public Animator animator;

    [Tooltip("Movement below this magnitude counts as idle (keeps the last facing).")]
    [SerializeField] float moveDeadzone = 0.05f;

    SpriteRenderer sr;
    EnemyBrain brain;
    Vector2 facing = Vector2.down;
    string currentState;

    bool attacking;
    float attackFailsafe;
    string attackState;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        brain = GetComponent<EnemyBrain>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector2 move = brain.MoveDirection;
        bool moving = move.sqrMagnitude > moveDeadzone * moveDeadzone;
        if (moving) facing = move;

        (string dir, bool flip) = ResolveDir(facing);
        sr.flipX = flip;

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

    void Play(string state)
    {
        if (state == currentState) return;
        currentState = state;
        animator.Play(state);
    }

    // Combat hook: play the attack clip for the aim direction. Locomotion is
    // suppressed until the clip finishes; `duration` is only a safety ceiling.
    // Reuses the player's r-hand clips (e_attack_r, ...) since this enemy swings a
    // single weapon. The clip's Animation Event drives EnemyAttacker.OnAttackHit.
    public void PlayAttack(float duration, Vector2 aimDir)
    {
        facing = aimDir;
        (string dir, _) = ResolveDir(facing);

        attackState = $"{dir}_attack_r";
        attacking = true;
        attackFailsafe = Time.time + Mathf.Max(duration, 3f);
        currentState = null;
    }

    static (string dir, bool flip) ResolveDir(Vector2 v)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) return ("e", v.x < 0f);
        return (v.y >= 0f ? "n" : "s", false);
    }
}

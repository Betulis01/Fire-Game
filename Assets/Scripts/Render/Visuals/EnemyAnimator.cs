using UnityEngine;

// Drives the enemy's Animator from EnemyBrain.MoveDirection. Same directional-pose
// scheme and state names as PlayerAnimator (south/north/east, west mirrored via
// flipX) so the enemy can reuse the player's Animator Controller asset:
//   s_idle n_idle e_idle  s_walk n_walk e_walk
// No attack states yet; add them the same way PlayerAnimator.PlayAttack does if/when
// the enemy gets an attack animation.
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

        string dir;
        bool flip = false;
        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y))
        {
            dir = "e";
            flip = facing.x < 0f;
        }
        else
        {
            dir = facing.y >= 0f ? "n" : "s";
        }

        sr.flipX = flip;
        Play($"{dir}_{(moving ? "walk" : "idle")}");
    }

    void Play(string state)
    {
        if (state == currentState) return;
        currentState = state;
        animator.Play(state);
    }
}

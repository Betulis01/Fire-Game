using System.Collections.Generic;
using UnityEngine;

// Drives the enemy's Animator from EnemyBrain.MoveDirection. Same directional-pose
// scheme and state naming as PlayerAnimator (NE/SE, NW/SW mirrored via flipX):
//   se_idle ne_idle  se_run ne_run  se_attack ne_attack
// Attacks route through PlayAttack(duration, aimDir), the same latch-until-clip-done
// scheme as PlayerAnimator.PlayAttack, just without a HandSide (the enemy swings one
// weapon, not two hands) — see EnemyAttacker.
[RequireComponent(typeof(EnemyBrain))]
public class EnemyAnimator : MonoBehaviour
{
    [Tooltip("Animator to drive. Falls back to one on this GameObject.")]
    public Animator animator;

    [Tooltip("Movement below this magnitude counts as idle (keeps the last facing).")]
    [SerializeField] float moveDeadzone = 0.05f;

    // The skeleton's art is layered (Body, Head) per Skeleton.aseprite's Individual
    // Layers import -- unlike the player's single flattened sprite, flipping needs to
    // apply to every art layer's renderer, not just one on this GameObject. Named
    // lookup (not GetComponentsInChildren) so this can't also pick up unrelated child
    // renderers -- e.g. the nested HealthBar, which must never mirror.
    static readonly string[] layerNames = { "Body", "Head" };
    SpriteRenderer[] renderers;
    EnemyBrain brain;
    Vector2 facing = Vector2.down;
    string currentState;

    bool attacking;
    public bool IsAttacking => attacking;
    float attackFailsafe;
    string attackState;

    // Missing-clip states we've already warned about (see PlayAttack), shared across
    // every enemy instance — logged once total rather than once per enemy per frame.
    // (EnemyAttacker.Update() re-arms every frame it's in range and IsAttacking is
    // false, which it always is here since a swing that can't arm never sets it.)
    static readonly HashSet<string> warnedMissingStates = new();

    void Awake()
    {
        var found = new List<SpriteRenderer>();
        foreach (string layer in layerNames)
        {
            Transform t = transform.Find(layer);
            if (t != null && t.TryGetComponent(out SpriteRenderer r)) found.Add(r);
        }
        renderers = found.ToArray();

        brain = GetComponent<EnemyBrain>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector2 move = brain.MoveDirection;
        bool moving = move.sqrMagnitude > moveDeadzone * moveDeadzone;
        if (moving) facing = move;

        (string dir, bool flip) = CardinalDir.Resolve(facing);
        foreach (SpriteRenderer r in renderers) r.flipX = flip;

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

    void Play(string state)
    {
        if (state == currentState) return;
        currentState = state;
        animator.Play(state);
    }

    // Combat hook: play the attack clip for the aim direction. Locomotion is
    // suppressed until the clip finishes; `duration` is only a safety ceiling.
    // One clip per direction (se_attack/ne_attack) since this enemy swings a single
    // weapon, no hand split. The clip's Animation Event drives EnemyAttacker.OnAttackHit.
    public void PlayAttack(float duration, Vector2 aimDir)
    {
        facing = aimDir;
        (string dir, _) = CardinalDir.Resolve(facing);
        string state = $"{dir}_attack";

        // No clip for this direction yet (art pending) -- same guard as
        // PlayerAnimator.PlayAttack, and more important here: EnemyAttacker retries
        // every frame while in range (see warnedMissingStates above), not just once
        // per press like a player's attack button.
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            if (warnedMissingStates.Add(state))
                Debug.LogWarning($"[EnemyAnimator] No '{state}' clip yet — skipping swing.", this);
            return;
        }

        attackState = state;
        attacking = true;
        attackFailsafe = Time.time + Mathf.Max(duration, 3f);
        currentState = null;
    }
}

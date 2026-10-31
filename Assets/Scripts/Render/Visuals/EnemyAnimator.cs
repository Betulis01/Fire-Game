using System.Collections.Generic;
using UnityEngine;

// Drives the enemy's Animator from EnemyBrain.MoveDirection. Same directional-pose
// scheme and state naming as PlayerAnimator (NE/SE, NW/SW mirrored via flipX):
//   se_idle ne_idle  se_run ne_run  se_attack ne_attack  se_hit ne_hit  se_die ne_die
//   se_spawn ne_spawn
// The spawn pair plays once on Start, facing a random diagonal: the enemy rises out of
// the ground unhittable (its hurt collider is off) while EnemyBrain and EnemyAttacker
// hold still on IsSpawning; an enemy without spawn art starts right away.
// The hit pair plays from OnHit (a landed hit) and never interrupts a swing. The die
// pair plays once on Health.Died and then holds its last frame for good (an enemy
// without die art -- no such state -- just keeps its current pose).
// Attacks route through PlayAttack(duration, aimDir), the same latch-until-clip-done
// scheme as PlayerAnimator.PlayAttack, just without a HandSide (the enemy swings one
// weapon, not two hands) — see EnemyAttacker.
[RequireComponent(typeof(EnemyBrain))]
public class EnemyAnimator : MonoBehaviour, IHitReactor
{
    [Tooltip("Animator to drive. Falls back to one on this GameObject.")]
    public Animator animator;

    [Tooltip("Movement below this magnitude counts as idle (keeps the last facing).")]
    [SerializeField] float moveDeadzone = 0.05f;

    [Tooltip("How long the hit clip holds before locomotion resumes.")]
    [SerializeField] float hitDuration = 0.2f;

    // The skeleton's art is layered (Body, Head, Ground Shadow, Highlight) per
    // Skeleton.aseprite's Individual Layers import -- unlike the player's single
    // flattened sprite, flipping needs to apply to every art layer's renderer, not
    // just one on this GameObject. Named lookup (not GetComponentsInChildren) so this
    // can't also pick up unrelated child renderers that must never mirror.
    static readonly string[] layerNames = { "Body", "Head", "Ground Shadow", TargetHighlight.LayerName };
    SpriteRenderer[] renderers;
    EnemyBrain brain;
    Vector2 facing = Vector2.down;
    string currentState;

    bool attacking;
    public bool IsAttacking => attacking;
    float attackFailsafe;
    string attackState;

    float hitUntil;   // hit reaction holds until then
    bool dead;        // the die clip owns the Animator from here on

    bool spawning;    // the spawn clip owns the Animator until it finishes
    public bool IsSpawning => spawning;
    float spawnFailsafe;
    string spawnState;
    Hurtbox hurtbox;

    static readonly Vector2[] diagonals = { new(1f, 1f), new(1f, -1f), new(-1f, 1f), new(-1f, -1f) };

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
        if (TryGetComponent(out Health health)) health.Died += PlayDeath;
        hurtbox = GetComponentInChildren<Hurtbox>();
    }

    // Rise out of the ground facing a random diagonal (west mirrors via flipX).
    void Start()
    {
        facing = diagonals[Random.Range(0, diagonals.Length)];
        (string dir, bool flip) = CardinalDir.Resolve(facing);
        string state = $"{dir}_spawn";
        if (!animator.HasState(0, Animator.StringToHash(state))) return;

        foreach (SpriteRenderer r in renderers) r.flipX = flip;
        spawning = true;
        spawnState = state;
        currentState = state;
        animator.Play(state, 0, 0f);
        spawnFailsafe = Time.time + 3f;
        SetHittable(false);
    }

    void Update()
    {
        if (dead) return;

        if (spawning)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            bool clipDone = st.IsName(spawnState) && st.normalizedTime >= 1f;
            if (Time.time < spawnFailsafe && !clipDone) return;
            spawning = false;
            SetHittable(true);
        }

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

        Play(Time.time < hitUntil ? $"{dir}_hit" : $"{dir}_{(moving ? "run" : "idle")}");
    }

    // Play the die clip for the current facing (west mirrors via the flipX already
    // set), once. The clip doesn't loop, so the Animator holds its last frame.
    void PlayDeath()
    {
        (string dir, _) = CardinalDir.Resolve(facing);
        string state = $"{dir}_die";
        if (!animator.HasState(0, Animator.StringToHash(state))) return;

        dead = true;
        attacking = false;
        spawning = false;
        currentState = state;
        animator.Play(state, 0, 0f);
    }

    // A landed hit (Hurtbox fans it out to every IHitReactor on this entity).
    public void OnHit(in HitInfo hit)
    {
        if (!spawning) hitUntil = Time.time + hitDuration;
    }

    // Hits find an enemy through its hurt collider, so turning that off is what
    // makes it unhittable while it rises.
    void SetHittable(bool hittable)
    {
        if (hurtbox != null && hurtbox.HurtCollider != null) hurtbox.HurtCollider.enabled = hittable;
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

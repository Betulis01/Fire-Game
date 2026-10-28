using UnityEngine;

// Enemy AI state machine. Wanders until it takes a hit, then permanently chases
// whoever landed that hit (hit.attacker), not a hardcoded player reference. Hurtbox
// fans hits to every IHitReactor on the owner (same mechanism Knockback uses), so
// this just needs to implement IHitReactor to find out who attacked.
public class EnemyBrain : MonoBehaviour, IHitReactor
{
    public enum State { Wander, Combat }

    public WanderBehavior wander;
    public ChaseBehavior chase;

    [Tooltip("Distance to the player within which a wandering enemy aggros without needing to be hit first.")]
    public float detectionRadius = 6f;

    public State CurrentState { get; private set; } = State.Wander;
    public Vector2 MoveDirection { get; private set; }
    public Transform AggroTarget => aggroTarget;

    Transform aggroTarget;
    Transform player;
    bool lookedForPlayer;
    EnemyAnimator anim;
    Health health;

    void Awake()
    {
        anim = GetComponent<EnemyAnimator>();
        health = GetComponent<Health>();
    }

    // Still alive, thinking and chasing `target` (its CombatState polls this).
    public bool IsEngagedWith(Transform target) =>
        isActiveAndEnabled && CurrentState == State.Combat && aggroTarget == target
        && (health == null || !health.IsDead);

    void Aggro(Transform target)
    {
        aggroTarget = target;
        CurrentState = State.Combat;
        if (target.TryGetComponent(out CombatState combat)) combat.Engage(this);
    }

    void Update()
    {
        // still rising out of the ground (EnemyAnimator's spawn clip): stand still
        if (anim != null && anim.IsSpawning)
        {
            MoveDirection = Vector2.zero;
            return;
        }

        if (CurrentState == State.Wander && aggroTarget == null) CheckDetection();

        IEnemyMoveBehavior active = CurrentState == State.Wander ? wander : (IEnemyMoveBehavior)chase;
        MoveDirection = active.GetMoveDirection(transform, aggroTarget);
    }

    void CheckDetection()
    {
        if (!lookedForPlayer)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            player = found != null ? found.transform : null;
            lookedForPlayer = true;
        }
        if (player == null) return;

        if (Vector2.Distance(transform.position, player.position) <= detectionRadius)
            Aggro(player);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    public void OnHit(in HitInfo hit)
    {
        if (hit.attacker == null) return;
        Aggro(hit.attacker.transform);
    }
}

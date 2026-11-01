using UnityEngine;

// Enemy AI state machine. Wanders until it takes a hit, then permanently chases
// whoever landed that hit (hit.attacker), not a hardcoded player reference. Hurtbox
// fans hits to every IHitReactor on the owner (same mechanism Knockback uses), so
// this just needs to implement IHitReactor to find out who attacked.
// A Skittish animal (the boar) never hunts: it ignores the player, and a hit makes it
// flee from the attacker (FleeBehavior) until it's safe, then it wanders again.
public class EnemyBrain : MonoBehaviour, IHitReactor
{
    public enum State { Wander, Combat, Flee }
    public enum Temperament { Hostile, Skittish }

    [Tooltip("Hostile hunts the player (detection, hits); Skittish flees when struck.")]
    public Temperament temperament = Temperament.Hostile;

    public WanderBehavior wander;
    public ChaseBehavior chase;
    public FleeBehavior flee;

    [Tooltip("Distance to the player within which a wandering enemy aggros without needing to be hit first.")]
    public float detectionRadius = 6f;

    public State CurrentState { get; private set; } = State.Wander;
    public Vector2 MoveDirection { get; private set; }
    public Transform AggroTarget => aggroTarget;

    // Speed on top of EnemyMover.speed: a fleeing animal bolts.
    public float SpeedMultiplier => CurrentState == State.Flee && flee != null ? flee.speedMultiplier : 1f;

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

        if (CurrentState == State.Flee && flee.Done) CurrentState = State.Wander;
        if (CurrentState == State.Wander && aggroTarget == null && temperament == Temperament.Hostile) CheckDetection();

        IEnemyMoveBehavior active = CurrentState switch
        {
            State.Combat => chase,
            State.Flee => flee,
            _ => wander,
        };
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
        if (temperament == Temperament.Skittish)
        {
            // runs from whoever hit it last; never engages anyone in combat
            if (flee == null) return;
            flee.Begin(hit.attacker.transform);
            CurrentState = State.Flee;
            return;
        }
        Aggro(hit.attacker.transform);
    }
}

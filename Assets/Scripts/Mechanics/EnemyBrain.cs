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

    void Update()
    {
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
        {
            aggroTarget = player;
            CurrentState = State.Combat;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    public void OnHit(in HitInfo hit)
    {
        if (hit.attacker == null) return;
        aggroTarget = hit.attacker.transform;
        CurrentState = State.Combat;
    }
}

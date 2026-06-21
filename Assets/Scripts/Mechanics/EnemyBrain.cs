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

    public State CurrentState { get; private set; } = State.Wander;
    public Vector2 MoveDirection { get; private set; }

    Transform aggroTarget;

    void Update()
    {
        IEnemyMoveBehavior active = CurrentState == State.Wander ? wander : (IEnemyMoveBehavior)chase;
        MoveDirection = active.GetMoveDirection(transform, aggroTarget);
    }

    public void OnHit(in HitInfo hit)
    {
        if (hit.attacker == null) return;
        aggroTarget = hit.attacker.transform;
        CurrentState = State.Combat;
    }
}

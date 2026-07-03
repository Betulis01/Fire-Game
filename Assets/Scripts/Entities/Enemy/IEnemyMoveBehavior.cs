using UnityEngine;

// A strategy for producing a movement direction for an enemy. EnemyBrain swaps
// between implementations (WanderBehavior, ChaseBehavior) based on its state.
public interface IEnemyMoveBehavior
{
    // Returns a movement direction (not necessarily normalized; zero = stay put).
    // `target` is the current aggro target and may be null (e.g. while wandering).
    Vector2 GetMoveDirection(Transform self, Transform target);
}

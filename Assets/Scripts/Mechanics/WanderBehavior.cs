using UnityEngine;

// Out-of-combat movement: pick a random direction, walk it for a bit, idle for a
// bit, repeat. Ignores the aggro target entirely.
public class WanderBehavior : MonoBehaviour, IEnemyMoveBehavior
{
    public float minMoveTime = 1f;
    public float maxMoveTime = 3f;
    public float minIdleTime = 1f;
    public float maxIdleTime = 2f;

    Vector2 direction;
    float phaseEndTime;
    bool moving;

    public Vector2 GetMoveDirection(Transform self, Transform target)
    {
        if (Time.time >= phaseEndTime) StartNextPhase();
        return moving ? direction : Vector2.zero;
    }

    void StartNextPhase()
    {
        moving = !moving;
        if (moving)
        {
            direction = Random.insideUnitCircle.normalized;
            phaseEndTime = Time.time + Random.Range(minMoveTime, maxMoveTime);
        }
        else
        {
            phaseEndTime = Time.time + Random.Range(minIdleTime, maxIdleTime);
        }
    }
}

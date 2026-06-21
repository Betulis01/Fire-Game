using UnityEngine;

// Combat movement: close the distance to the aggro target, stopping short so the
// enemy doesn't jitter on top of it. After reaching the target, waits chaseDelay
// seconds once it moves away again before resuming, so jitter at the stop-distance
// boundary doesn't flicker the enemy between moving and idle.
public class ChaseBehavior : MonoBehaviour, IEnemyMoveBehavior
{
    public float stopDistance = 0.6f;
    public float chaseDelay = 1f;

    bool reached;
    float resumeAt;

    public Vector2 GetMoveDirection(Transform self, Transform target)
    {
        if (target == null) return Vector2.zero;

        Vector2 toTarget = (Vector2)target.position - (Vector2)self.position;
        if (toTarget.magnitude <= stopDistance)
        {
            reached = true;
            return Vector2.zero;
        }

        if (reached)
        {
            reached = false;
            resumeAt = Time.time + chaseDelay;
        }
        if (Time.time < resumeAt) return Vector2.zero;

        return toTarget.normalized;
    }
}

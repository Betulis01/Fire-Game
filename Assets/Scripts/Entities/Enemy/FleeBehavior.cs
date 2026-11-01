using UnityEngine;

// A skittish animal's reaction to being struck (EnemyBrain, Temperament.Skittish):
// run straight away from whoever hit it, weaving a little side to side so the path
// doesn't look ruler-straight, until it's safeDistance away or maxTime has passed.
// It also moves faster than it wanders (speedMultiplier, read through EnemyBrain).
public class FleeBehavior : MonoBehaviour, IEnemyMoveBehavior
{
    [Tooltip("Distance from the threat at which the animal feels safe and calms down.")]
    public float safeDistance = 6f;

    [Tooltip("Longest a flight lasts, in seconds, even if it's still close (cornered).")]
    public float maxTime = 4f;

    [Tooltip("Speed while fleeing, as a multiple of the wander speed.")]
    public float speedMultiplier = 3.6f;

    [Tooltip("How far the path weaves from straight away (0 = straight).")]
    [Range(0f, 1f)] public float weave = 0.35f;

    Transform threat;
    float endTime;
    float seed;

    // Flight is over: safe, timed out, or the threat is gone.
    public bool Done => threat == null || Time.time >= endTime ||
                        Vector2.Distance(transform.position, threat.position) >= safeDistance;

    public void Begin(Transform from)
    {
        threat = from;
        endTime = Time.time + maxTime;
        seed = Random.value * 100f;
    }

    public Vector2 GetMoveDirection(Transform self, Transform target)
    {
        if (threat == null) return Vector2.zero;

        Vector2 away = (Vector2)(self.position - threat.position);
        away = away.sqrMagnitude > 1e-6f ? away.normalized : Random.insideUnitCircle.normalized;

        float sway = (Mathf.PerlinNoise(seed, Time.time * 0.8f) * 2f - 1f) * weave;
        return away + new Vector2(-away.y, away.x) * sway;
    }
}

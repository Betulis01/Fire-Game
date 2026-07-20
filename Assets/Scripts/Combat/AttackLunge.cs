using UnityEngine;

// One-shot forward push for an attack's windup, structurally parallel to Knockback
// (same Velocity/SelfMove fold-in contract) but purpose-built for a single eased
// impulse instead of a linearly-decaying reactive shove. Begin() fires the lunge;
// its speed eases along `curve` from 1 down to 0 over `duration` so the stop is
// smooth rather than abrupt. A mover that owns rb.MovePosition (e.g. PlayerController)
// sets SelfMove = false and folds Velocity into its own move, same as Knockback.
[RequireComponent(typeof(Rigidbody2D))]
public class AttackLunge : MonoBehaviour
{
    public bool SelfMove = true;

    public bool IsLunging => t < duration;
    public Vector2 Velocity => velocity;

    Rigidbody2D rb;
    Vector2 dir;
    float speed = 1;
    float duration;
    AnimationCurve curve;
    float t;
    Vector2 velocity;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // Start a lunge along `dir` at `speed`, easing out over `duration` seconds
    // via `curve` (evaluated 0..1, expected to run from 1 down to 0). Returns
    // whether a lunge actually started, so callers can gate lunge-only effects.
    public bool Begin(Vector2 dir, float speed, float duration, AnimationCurve curve)
    {
        if (speed <= 0f || duration <= 0f || dir.sqrMagnitude < 1e-6f) return false;
        this.dir = dir.normalized;
        this.speed = speed;
        this.duration = duration;
        this.curve = curve;
        t = 0f;
        return true;
    }

    void FixedUpdate()
    {
        if (t >= duration) { velocity = Vector2.zero; return; }

        velocity = dir * speed * curve.Evaluate(t / duration);
        if (SelfMove) rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        t += Time.fixedDeltaTime;
    }
}

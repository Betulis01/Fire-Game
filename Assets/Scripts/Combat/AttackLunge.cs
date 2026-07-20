using UnityEngine;

// One-shot forward push for an attack's windup, structurally parallel to Knockback
// (same Velocity/SelfMove fold-in contract) but purpose-built for a single eased
// impulse instead of a linearly-decaying reactive shove. Begin() fires the lunge;
// its speed eases along `curve` from 1 down to 0 over `duration`. If the swing goes
// on to connect (IAttackReactor.OnDealtHit, same notification AttackRecoil/HitStopOnHit
// use), whatever velocity is left over is smoothed down to zero over `smoothTime`
// (SmoothDamp) instead of being cut off dead — a hand-off back to normal movement. A
// whiffed swing gets no such hand-off: velocity cuts to zero the frame `duration`
// elapses. A mover that owns rb.MovePosition (e.g. PlayerController) sets
// SelfMove = false and folds Velocity into its own move, same as Knockback.
[RequireComponent(typeof(Rigidbody2D))]
public class AttackLunge : MonoBehaviour, IAttackReactor
{
    public bool SelfMove = true;

    [Tooltip("Seconds to ease any velocity still left once `duration` elapses down to " +
             "zero, instead of cutting it off dead that frame.")]
    public float smoothTime = 0.1f;

    public bool IsLunging => t < duration || easingOut;
    public Vector2 Velocity => velocity;

    Rigidbody2D rb;
    Vector2 dir;
    float speed = 1;
    float duration;
    AnimationCurve curve;
    float t;
    Vector2 velocity;
    bool easingOut;
    Vector3 smoothVel;
    bool connected;

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
        easingOut = false;
        connected = false;
        return true;
    }

    // Notified once per connecting swing (see HitResolution.NotifyAttacker) — marks
    // this lunge as eligible for the smooth hand-off instead of a hard stop.
    public void OnDealtHit(in HitInfo hit) => connected = true;

    void FixedUpdate()
    {
        if (easingOut)
        {
            velocity = Vector3.SmoothDamp(velocity, Vector2.zero, ref smoothVel, smoothTime);
            if (velocity.sqrMagnitude < 0.0001f) { velocity = Vector2.zero; easingOut = false; }
            if (SelfMove) rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
            return;
        }

        if (t >= duration)
        {
            if (!connected)
            {
                // Hand off to the smooth-out phase starting next frame, from whatever
                // velocity the curve left us at (rather than snapping it to zero here).
                easingOut = true;
                smoothVel = Vector3.zero;
            }
            else
            {
                // Whiffed: no hit to smooth into, cut the push off dead.
                velocity = Vector2.zero;
            }
            return;
        }

        velocity = dir * speed * curve.Evaluate(t / duration);
        if (SelfMove) rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        t += Time.fixedDeltaTime;
    }
}

using UnityEngine;

// One-shot forward push for an attack's windup, structurally parallel to Knockback
// (same Velocity/SelfMove fold-in contract) but purpose-built for a single eased
// impulse instead of a linearly-decaying reactive shove. Begin() fires the lunge;
// its speed eases along `curve` from 1 down to 0 over `duration`. When `duration`
// elapses before the swing connects (IAttackReactor.OnDealtHit, same notification
// AttackRecoil/HitStopOnHit use) -- the usual case, the push runs during the windup --
// whatever velocity the curve left is smoothed down to zero over `smoothTime`
// (SmoothDamp) instead of being cut off dead. One that already connected cuts to
// zero that frame. A mover that owns rb.MovePosition (e.g. PlayerController) sets
// SelfMove = false and folds Velocity into its own move, same as Knockback.
//
// BeginStep() is the measured variant (WeaponUse's lunge grace window): it travels
// exactly a given offset along a sine ease-out, then stops dead -- no smoothing tail,
// so the swing lands right where the step was aimed.
[RequireComponent(typeof(Rigidbody2D))]
public class AttackLunge : MonoBehaviour, IAttackReactor
{
    public bool SelfMove = true;

    [Tooltip("Seconds to ease any velocity still left once `duration` elapses down to " +
             "zero, instead of cutting it off dead that frame.")]
    public float smoothTime = 0.1f;

    public bool IsLunging => stepping || t < duration || easingOut;
    public bool IsStepping => stepping;
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

    bool stepping;
    Vector2 stepOffset;
    float stepDuration;
    float stepT;

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
        stepping = false;
        return true;
    }

    // Travel exactly `offset` over `duration` seconds, fast start, gentle stop, then
    // stop dead. Replaces any push in progress. Returns whether a step started.
    public bool BeginStep(Vector2 offset, float duration)
    {
        if (offset.sqrMagnitude < 1e-8f) return false;
        stepOffset = offset;
        stepDuration = Mathf.Max(duration, Time.fixedDeltaTime);
        stepT = 0f;
        stepping = true;
        t = this.duration = 0f;
        easingOut = false;
        velocity = Vector2.zero;
        return true;
    }

    // Notified once per connecting swing (see HitResolution.NotifyAttacker) -- a lunge
    // still running then cuts off dead when it ends instead of smoothing out.
    public void OnDealtHit(in HitInfo hit) => connected = true;

    // How far along the step's offset (0..1) it is at time `at`: sine ease-out.
    float StepProgress(float at) => Mathf.Sin(Mathf.Clamp01(at / stepDuration) * Mathf.PI * 0.5f);

    void FixedUpdate()
    {
        if (stepping)
        {
            // The last stretch was handed out last frame (read by the mover this frame
            // or last, depending on order): now stop dead.
            if (stepT >= stepDuration)
            {
                velocity = Vector2.zero;
                stepping = false;
                return;
            }

            // Velocity for exactly this frame's slice of the curve, so the slices sum
            // to the offset however few physics frames the step spans.
            float dt = Time.fixedDeltaTime;
            float next = Mathf.Min(stepT + dt, stepDuration);
            velocity = stepOffset * ((StepProgress(next) - StepProgress(stepT)) / dt);
            stepT = next;
            if (SelfMove) rb.MovePosition(rb.position + velocity * dt);
            return;
        }

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
                // Already connected: cut the push off dead.
                velocity = Vector2.zero;
            }
            return;
        }

        velocity = dir * speed * curve.Evaluate(t / duration);
        if (SelfMove) rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        t += Time.fixedDeltaTime;
    }
}

using UnityEngine;

// Reusable shove: holds a velocity that decays to zero and slides the entity along
// it. Opt-in per prefab (enemies, the player). Apply() adds an impulse; the velocity
// eases down to zero over `smoothTime` (SmoothDamp) rather than bleeding off at a
// constant rate. By default the component integrates itself through rb.MovePosition
// (collision-aware, used by entities with no other mover). A script that owns
// rb.MovePosition (e.g. PlayerController) sets SelfMove = false and folds Velocity
// into its own move so there's only ever one MovePosition per step.
[RequireComponent(typeof(Rigidbody2D))]
public class Knockback : MonoBehaviour, IHitReactor
{
    [Tooltip("Seconds for the knockback velocity to ease down to zero. Lower = snappier.")]
    public float smoothTime = 0.15f;

    // Whether this component moves the body itself. A mover that owns rb.MovePosition
    // turns this off and reads Velocity instead (see class summary).
    public bool SelfMove = true;

    public Vector2 Velocity => velocity;

    Rigidbody2D rb;
    Vector2 velocity;
    Vector3 smoothVel;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // Push this entity along `dir` (need not be normalized) with the given force.
    public void Apply(Vector2 dir, float force)
    {
        if (force <= 0f || dir.sqrMagnitude < 1e-6f) return;
        velocity += dir.normalized * force;
    }

    // React to a landed hit by shoving away from the attacker (the weapon sets force).
    public void OnHit(in HitInfo hit) => Apply(hit.direction, hit.targetKnockback);

    void FixedUpdate()
    {
        if (velocity.sqrMagnitude < 1e-6f) { velocity = Vector2.zero; smoothVel = Vector3.zero; return; }

        if (SelfMove) rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        // We own the decay regardless of who does the moving, so consumers only read.
        velocity = Vector3.SmoothDamp(velocity, Vector2.zero, ref smoothVel, smoothTime);
    }
}

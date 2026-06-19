using UnityEngine;

// Reusable shove: holds a velocity that decays to zero and slides the entity along
// it. Opt-in per prefab (enemies, the player). Apply() adds an impulse; the velocity
// bleeds off at `deceleration`. By default the component integrates itself through
// rb.MovePosition (collision-aware, used by entities with no other mover). A script
// that owns rb.MovePosition (e.g. PlayerController) sets SelfMove = false and folds
// Velocity into its own move so there's only ever one MovePosition per step.
[RequireComponent(typeof(Rigidbody2D))]
public class Knockback : MonoBehaviour, IHitReactor
{
    [Tooltip("How fast the knockback velocity bleeds off (units/sec^2). Higher = snappier.")]
    public float deceleration = 30f;

    // Whether this component moves the body itself. A mover that owns rb.MovePosition
    // turns this off and reads Velocity instead (see class summary).
    public bool SelfMove = true;

    public Vector2 Velocity => velocity;

    Rigidbody2D rb;
    Vector2 velocity;

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
        if (velocity.sqrMagnitude < 1e-6f) { velocity = Vector2.zero; return; }

        if (SelfMove) rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        // We own the decay regardless of who does the moving, so consumers only read.
        velocity = Vector2.MoveTowards(velocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
    }
}

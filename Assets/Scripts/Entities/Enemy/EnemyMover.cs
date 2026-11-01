using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyBrain), typeof(SpriteRenderer))]
public class EnemyMover : MonoBehaviour
{
    public float speed = 3f;

    Rigidbody2D rb;
    EnemyBrain brain;
    SpriteRenderer spriteRenderer;
    Knockback knockback;
    GroundSensor ground;
    Vector2 moveVelocity;
    Vector3 moveSmoothVel;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<EnemyBrain>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Same reasoning as PlayerController: we own the single MovePosition call,
        // so fold knockback in here rather than letting it move the body separately.
        knockback = GetComponent<Knockback>();
        if (knockback != null) knockback.SelfMove = false;

        ground = GroundSensor.On(gameObject);
    }

    void FixedUpdate()
    {
        // the ground slows (snow) and, on ice, makes the steering slide (GroundSensor);
        // the brain speeds a fleeing animal up
        Vector2 target = brain.MoveDirection.normalized * speed * ground.SpeedMultiplier * brain.SpeedMultiplier;
        if (ground.SlipTime > 0f)
            moveVelocity = Vector3.SmoothDamp(moveVelocity, target, ref moveSmoothVel, ground.SlipTime);
        else
        {
            moveVelocity = target;
            moveSmoothVel = Vector3.zero;
        }

        Vector2 velocity = moveVelocity;
        if (knockback != null) velocity += knockback.Velocity;

        Vector2 newPos = rb.position + velocity * Time.fixedDeltaTime;
        if (WorldBounds.Instance != null)
            newPos = WorldBounds.Instance.ClampPoint(newPos, spriteRenderer.bounds.extents);
        rb.MovePosition(newPos);
    }
}

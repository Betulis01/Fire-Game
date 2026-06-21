using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyBrain), typeof(SpriteRenderer))]
public class EnemyMover : MonoBehaviour
{
    public float speed = 3f;

    Rigidbody2D rb;
    EnemyBrain brain;
    SpriteRenderer spriteRenderer;
    Knockback knockback;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        brain = GetComponent<EnemyBrain>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Same reasoning as PlayerController: we own the single MovePosition call,
        // so fold knockback in here rather than letting it move the body separately.
        knockback = GetComponent<Knockback>();
        if (knockback != null) knockback.SelfMove = false;
    }

    void FixedUpdate()
    {
        Vector2 velocity = brain.MoveDirection.normalized * speed;
        if (knockback != null) velocity += knockback.Velocity;

        Vector2 newPos = rb.position + velocity * Time.fixedDeltaTime;
        if (WorldBounds.Instance != null)
            newPos = WorldBounds.Instance.ClampPoint(newPos, spriteRenderer.bounds.extents);
        rb.MovePosition(newPos);
    }
}

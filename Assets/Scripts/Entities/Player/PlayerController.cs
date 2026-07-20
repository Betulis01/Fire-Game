using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Hands), typeof(PlayerTemperature))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    PlayerTemperature body;
    Rigidbody2D rb;
    Knockback knockback;
    AttackLunge lunge;

    public float speed = 4f;

    [Header("Temperature affects speed")]
    public float coldTemp = -10f;            // felt <= this -> slowest
    public float warmTemp = 0f;              // felt >= this -> full speed
    [Range(0f, 1f)] public float speedFloor = 0.4f;   // slowest fraction of base speed
    public float smoothTime = 0.5f;          // how quickly speed eases to the target

    float speedMultiplier = 1f;
    float smoothVel;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<PlayerTemperature>();
        rb = GetComponent<Rigidbody2D>();

        // We own the player's single MovePosition, so fold knockback in here rather
        // than letting it move the body separately (two MovePosition calls conflict).
        knockback = GetComponent<Knockback>();
        if (knockback != null) knockback.SelfMove = false;

        lunge = GetComponent<AttackLunge>();
        if (lunge != null) lunge.SelfMove = false;
    }

    void Update()
    {
        // cold drags speed toward the floor; warmth eases it back to full
        float t = Mathf.InverseLerp(coldTemp, warmTemp, body.Temp);   // 0 cold .. 1 warm
        float target = Mathf.Lerp(speedFloor, 1f, t);
        speedMultiplier = Mathf.SmoothDamp(speedMultiplier, target, ref smoothVel, smoothTime);
    }

    void FixedUpdate()
    {
        // lock movement input while a lunge is easing out, so the player isn't
        // fighting the push with held direction keys
        bool lunging = lunge != null && lunge.IsLunging;
        Vector2 input = lunging ? Vector2.zero : UserInput.Instance.Move;   // WASD/arrows or gamepad stick
        Vector2 velocity = input.normalized * speed * speedMultiplier;

        // add any active knockback/lunge (each decays itself); composes with input in one move
        if (knockback != null) velocity += knockback.Velocity;
        if (lunge != null) velocity += lunge.Velocity;

        // move through the physics engine so colliders stop us against trees, etc.
        Vector2 newPos = rb.position + velocity * Time.fixedDeltaTime;
        if (WorldBounds.Instance != null)
            newPos = WorldBounds.Instance.ClampPoint(newPos, spriteRenderer.bounds.extents);
        rb.MovePosition(newPos);
    }
}

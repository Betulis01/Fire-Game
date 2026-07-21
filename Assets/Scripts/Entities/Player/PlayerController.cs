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
    WeaponUse weaponUse;
    PlayerBuffs buffs;

    public float speed = 4f;

    [Header("Temperature affects speed")]
    public float coldTemp = -10f;            // felt <= this -> slowest
    public float warmTemp = 0f;              // felt >= this -> full speed
    [Range(0f, 1f)] public float speedFloor = 0.4f;   // slowest fraction of base speed
    public float smoothTime = 0.5f;          // how quickly speed eases to the target

    [Tooltip("Seconds for movement to ease to a stop after input is released. Redirecting " +
             "while still holding input stays instant — only releasing eases out.")]
    public float stopSmoothTime = 0.15f;

    float speedMultiplier = 1f;
    float smoothVel;
    Vector2 moveVelocity;
    Vector3 moveSmoothVel;

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

        weaponUse = GetComponent<WeaponUse>();
        buffs = GetComponent<PlayerBuffs>();
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
        // lock movement input for the whole attack (windup, any charge hold, swing,
        // recovery) so the player can't walk out from under it mid-swing — driven by
        // the attack animation itself, not just the brief lunge window.
        bool inputLocked = weaponUse != null && weaponUse.animator != null && weaponUse.animator.IsAttacking;
        Vector2 input = inputLocked ? Vector2.zero : UserInput.Instance.Move;   // WASD/arrows or gamepad stick
        float buffMultiplier = buffs != null ? buffs.SpeedMultiplier : 1f;
        Vector2 targetMoveVelocity = input.normalized * speed * speedMultiplier * buffMultiplier;

        if (input.sqrMagnitude > 0.0001f)
        {
            // actively steering: instant response, no lag while held
            moveVelocity = targetMoveVelocity;
            moveSmoothVel = Vector3.zero;
        }
        else
        {
            // released: ease the last velocity down to zero instead of cutting dead
            moveVelocity = Vector3.SmoothDamp(moveVelocity, Vector3.zero, ref moveSmoothVel, stopSmoothTime);
        }
        Vector2 velocity = moveVelocity;

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

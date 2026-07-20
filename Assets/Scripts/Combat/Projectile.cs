using UnityEngine;

// A fired arrow: travels in a straight line and delivers its hit the moment it
// touches a Hurtbox, via the same HitResolution rules a Hitbox uses (skip the
// owner, respect ToolDamageFilter). Unlike Hitbox this isn't a one-shot poll — it
// keeps moving and checking every frame until it lands or runs out of range. Speed
// eases down to 0 over the final third of that range rather than cutting off at
// full speed, so it reads as losing momentum rather than vanishing mid-flight.
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    // Speed starts easing toward 0 once this fraction of maxRange has been covered.
    const float DecelStartFraction = 2f / 3f;

    // Once eased speed decays below this, treat it as stopped and despawn. Without a
    // floor the ease (speed as a function of distance-remaining) approaches maxRange
    // asymptotically and would never actually reach it.
    const float MinSpeed = 0.05f;

    public LayerMask hurtboxLayers;

    AttackData attack;
    GameObject owner;
    Vector2 direction;
    float baseSpeed;
    Vector2 spawnPos;
    float maxRange;

    public void Launch(in AttackData attack, GameObject owner, Vector2 direction, float speed, float range)
    {
        this.attack = attack;
        this.owner = owner;
        this.direction = direction.normalized;
        baseSpeed = speed;
        spawnPos = transform.position;
        maxRange = range;

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void FixedUpdate()
    {
        float traveled = Vector2.Distance(transform.position, spawnPos);
        float decelStart = maxRange * DecelStartFraction;
        float speed = traveled <= decelStart
            ? baseSpeed
            : Mathf.Lerp(baseSpeed, 0f, Mathf.InverseLerp(decelStart, maxRange, traveled));

        if (traveled >= maxRange || speed <= MinSpeed)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & hurtboxLayers) == 0) return;
        if (!HitResolution.TryHit(col, owner, attack, transform.position, out HitInfo hit)) return;

        HitResolution.NotifyAttacker(owner, attack, hit.point);
        Destroy(gameObject);
    }
}

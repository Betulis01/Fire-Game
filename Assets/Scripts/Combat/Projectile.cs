using UnityEngine;

// A fired arrow: travels in a straight line and delivers its hit the moment it
// touches a Hurtbox, via the same HitResolution rules a Hitbox uses (skip the
// owner, respect ToolDamageFilter). Unlike Hitbox this isn't a one-shot poll — it
// keeps moving and checking every frame until it lands or runs out of range/life.
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public LayerMask hurtboxLayers;
    public float maxLifetime = 3f;

    AttackData attack;
    GameObject owner;
    Vector2 velocity;
    float spawnedAt;

    public void Launch(in AttackData attack, GameObject owner, Vector2 direction, float speed)
    {
        this.attack = attack;
        this.owner = owner;
        velocity = direction.normalized * speed;
        spawnedAt = Time.time;

        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    void FixedUpdate()
    {
        if (Time.time - spawnedAt >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & hurtboxLayers) == 0) return;
        if (!HitResolution.TryHit(col, owner, attack, transform.position, out HitInfo hit)) return;

        HitResolution.NotifyAttacker(owner, attack, hit.point);
        Destroy(gameObject);
    }
}

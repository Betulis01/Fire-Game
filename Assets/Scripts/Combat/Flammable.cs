using UnityEngine;

// Capability: this entity can be set on fire by a torch. Sits beside Health in the
// composition model (opt-in, like ToolDamageFilter). A landed ToolKind.Torch hit
// ignites it; while burning it takes tickDamage every tickInterval seconds for
// burnDuration seconds, with a fire VFX parented on top of the body. Re-igniting
// while already burning refreshes the duration (no stacking, one VFX, tick cadence
// kept). Burn damage goes straight to Health.TakeDamage, so ToolDamageFilter and
// ShieldBlock don't apply to it.
[RequireComponent(typeof(Health))]
public class Flammable : MonoBehaviour, IHitReactor
{
    [Tooltip("Seconds the fire lasts after the last ignition.")]
    public float burnDuration = 10f;

    [Tooltip("Seconds between burn damage ticks (first tick lands one interval after ignition).")]
    public float tickInterval = 3f;

    [Tooltip("Damage dealt per tick.")]
    public float tickDamage = 10f;

    [Tooltip("Looping fire VFX shown on the target while burning. Parented to this entity.")]
    public GameObject fireEffectPrefab;

    [Tooltip("Where the VFX sits relative to the entity. Leave at 0 to use the center of " +
             "the Hurtbox collider instead.")]
    public Vector2 effectOffset;

    Health health;
    Hurtbox hurtbox;
    GameObject fire;
    Fuel fireFuel;        // drives the spawned VFX's ember/medium/roaring stages
    float remaining;     // burn time left; > 0 means burning
    float tickTimer;     // time until the next tick

    public bool IsBurning => remaining > 0f;

    void Awake()
    {
        health = GetComponent<Health>();
        hurtbox = GetComponentInChildren<Hurtbox>();
    }

    void OnEnable() => health.Died += Extinguish;
    void OnDisable()
    {
        health.Died -= Extinguish;
        Extinguish();
    }

    public void OnHit(in HitInfo hit)
    {
        if (hit.kind == ToolKind.Torch) Ignite();
    }

    public void Ignite()
    {
        if (health.IsDead) return;

        if (!IsBurning)
        {
            tickTimer = tickInterval;
            SpawnFire();
        }
        else if (fireFuel != null)
        {
            fireFuel.Reset(1f, 1f / burnDuration);
        }
        remaining = burnDuration;
    }

    void Extinguish()
    {
        remaining = 0f;
        if (fire != null) Destroy(fire);
        fire = null;
        fireFuel = null;
    }

    void Update()
    {
        if (!IsBurning) return;

        float dt = Time.deltaTime;
        remaining -= dt;
        tickTimer -= dt;

        if (tickInterval > 0f && tickTimer <= 0f)
        {
            tickTimer += tickInterval;
            health.TakeDamage(tickDamage, DamageType.Fire, Center);
            if (health.IsDead) return;
        }

        if (remaining <= 0f) Extinguish();
    }

    Vector2 Center => effectOffset != Vector2.zero || hurtbox == null || hurtbox.HurtCollider == null
        ? (Vector2)transform.position + effectOffset
        : (Vector2)hurtbox.HurtCollider.bounds.center;

    void SpawnFire()
    {
        if (fireEffectPrefab == null) return;
        fire = Instantiate(fireEffectPrefab, Center, Quaternion.identity, transform);
        fireFuel = fire.GetComponent<Fuel>();
        if (fireFuel != null) fireFuel.Reset(1f, 1f / burnDuration);
    }
}

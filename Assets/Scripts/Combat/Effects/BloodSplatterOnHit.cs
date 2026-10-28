using UnityEngine;

// Leaves splatters on the ground under a creature as it takes blows: the first hit
// that deals damage always leaves one, every hit after that has extraChance to add
// another, and dying always leaves one more (whatever killed it). Like DropOnHit,
// only combat hits that actually hurt count (a filtered or fully blocked hit deals
// none, burn and cold ticks aren't hits), the killing blow included. The splatter
// is thrown along the attack (attacker -> creature) to land behind the creature,
// throwDistance past its feet; with no attacker to aim from (a death by burning)
// it lands around the feet. The prefab randomizes its own look (GroundSplatter).
[RequireComponent(typeof(Health))]
public class BloodSplatterOnHit : MonoBehaviour
{
    public GameObject splatterPrefab;

    [Tooltip("Chance for each hit after the first to leave another splatter.")]
    [Range(0f, 1f)] public float extraChance = 0.35f;

    [Tooltip("How far past the feet, along the attack, a splatter lands.")]
    public float throwDistance = 0.25f;

    [Tooltip("Random spread around that landing point.")]
    public float scatterRadius = 0.1f;

    Health health;
    bool bled;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    void OnDamaged(DamageInfo info)
    {
        if (info.type != DamageType.Combat) return;
        if (bled && Random.value >= extraChance) return;
        bled = true;
        Spawn(info.attacker);
    }

    // Killer is set before Died is raised (null for an environmental death)
    void OnDied() => Spawn(health.Killer);

    void Spawn(GameObject attacker)
    {
        if (splatterPrefab == null) return;

        Vector2 feet = transform.position;
        Vector2 pos = feet + Random.insideUnitCircle * scatterRadius;
        if (attacker != null)
        {
            Vector2 away = feet - (Vector2)attacker.transform.position;
            if (away.sqrMagnitude > 1e-6f) pos += away.normalized * throwDistance;
        }
        Instantiate(splatterPrefab, pos, Quaternion.identity);
    }
}

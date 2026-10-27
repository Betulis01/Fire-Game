using System;
using UnityEngine;

// Loot knocked loose by each blow -- DropOnDeath's per-hit sibling (wood chips off a
// tree, stone off an ore). Every combat hit that actually deals damage rolls each
// drop's chance and scatters `count` of it around the entity. Hits a target shrugs
// off (ToolDamageFilter: a fist on a tree, an axe on stone) deal no damage, so they
// drop nothing; burn and cold ticks aren't hits. The killing blow drops its hit loot
// too, before any DropOnDeath loot. maxDrops caps the total items all hits together
// can knock loose over the entity's life (DropOnDeath loot doesn't count).
[RequireComponent(typeof(Health))]
public class DropOnHit : MonoBehaviour
{
    [Serializable]
    public struct Drop
    {
        public GameObject prefab;
        public int count;
        [Range(0f, 1f)] public float chance;   // rolled once per hit for the whole count
    }

    public Drop[] drops;
    public float scatterRadius = 0.5f;

    [Tooltip("Most items hits can drop in total, across all entries. 0 = no limit.")]
    public int maxDrops = 0;

    Health health;
    int dropped;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
    }

    void OnDestroy()
    {
        if (health != null) health.Damaged -= OnDamaged;
    }

    void OnDamaged(DamageInfo info)
    {
        if (info.type != DamageType.Combat) return;

        foreach (var drop in drops)
        {
            if (drop.prefab == null || UnityEngine.Random.value >= drop.chance) continue;
            for (int i = 0; i < drop.count; i++)
            {
                if (maxDrops > 0 && dropped >= maxDrops) return;
                dropped++;
                Vector2 off = UnityEngine.Random.insideUnitCircle * scatterRadius;
                Instantiate(drop.prefab, transform.position + (Vector3)off, Quaternion.identity);
            }
        }
    }
}

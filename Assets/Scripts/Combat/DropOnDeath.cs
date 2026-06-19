using UnityEngine;

// When its Health is depleted, scatters drops at the entity's position (e.g. a tree
// dropping wood, an enemy dropping loot). Loot only — removing the entity is owned
// by EntityDeath, which reacts to the same Health.Died event. Works for any entity.
[RequireComponent(typeof(Health))]
public class DropOnDeath : MonoBehaviour
{
    public GameObject dropPrefab;     // e.g. the Wood prefab
    public int dropCount = 3;
    public float scatterRadius = 0.5f;

    void Awake() => GetComponent<Health>().Died += SpawnDrops;

    void SpawnDrops()
    {
        if (dropPrefab == null) return;

        for (int i = 0; i < dropCount; i++)
        {
            Vector2 off = Random.insideUnitCircle * scatterRadius;
            Instantiate(dropPrefab, transform.position + (Vector3)off, Quaternion.identity);
        }
    }
}

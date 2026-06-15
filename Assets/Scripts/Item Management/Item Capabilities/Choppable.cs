using UnityEngine;

// A resource that, when its Health is depleted, is replaced by scattered drops
// (e.g. a tree dropping wood). Health takes the damage; Choppable owns the yield.
[RequireComponent(typeof(Health))]
public class Choppable : MonoBehaviour
{
    public GameObject dropPrefab;     // e.g. the Wood prefab
    public int dropCount = 3;
    public float scatterRadius = 0.5f;

    void Awake() => GetComponent<Health>().Died += Fell;

    void Fell()
    {
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 off = Random.insideUnitCircle * scatterRadius;
            if (dropPrefab != null)
                Instantiate(dropPrefab, transform.position + (Vector3)off, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}

using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DropOnDeath : MonoBehaviour
{
    [Serializable]
    public struct Drop
    {
        public GameObject prefab;
        public int count;
    }

    public Drop[] drops;
    public float scatterRadius = 0.5f;

    // The death's loot spilling out: where, and how many items (for the audio layer).
    public event System.Action<Vector3, int> Dropped;

    void Awake() => GetComponent<Health>().Died += SpawnDrops;

    void SpawnDrops()
    {
        int spawned = 0;
        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;
            for (int i = 0; i < drop.count; i++)
            {
                Vector2 off = UnityEngine.Random.insideUnitCircle * scatterRadius;
                Instantiate(drop.prefab, transform.position + (Vector3)off, Quaternion.identity);
                spawned++;
            }
        }
        if (spawned > 0) Dropped?.Invoke(transform.position, spawned);
    }
}

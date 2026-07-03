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

    void Awake() => GetComponent<Health>().Died += SpawnDrops;

    void SpawnDrops()
    {
        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;
            for (int i = 0; i < drop.count; i++)
            {
                Vector2 off = UnityEngine.Random.insideUnitCircle * scatterRadius;
                Instantiate(drop.prefab, transform.position + (Vector3)off, Quaternion.identity);
            }
        }
    }
}

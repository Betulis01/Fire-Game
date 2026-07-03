using UnityEngine;

// Spawns enemies in a ring around this light source while it's night. Spawn rate
// scales with the light's fuel level (full fuel = fastest spawns, near-empty =
// slowest, empty = no spawns), so letting the fire die out makes the dark more
// dangerous. Enemy count is capped globally across all spawners.
[RequireComponent(typeof(Fuel))]
public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;

    [Header("Spawn ring")]
    public float minRadius = 5f;
    public float maxRadius = 10f;

    [Header("Spawn rate (scales with fuel level, 0..1)")]
    [Tooltip("Seconds between spawns at full fuel.")]
    public float minSpawnInterval = 2f;
    [Tooltip("Seconds between spawns as fuel approaches empty.")]
    public float maxSpawnInterval = 15f;

    public static int maxConcurrentEnemies = 50;
    static int activeCount;

    Fuel fuel;
    float timer;

    void Awake() => fuel = GetComponent<Fuel>();

    void Update()
    {
        if (DayNightCycle.Instance == null || !DayNightCycle.Instance.IsNight)
        {
            timer = 0f;
            return;
        }

        float fuelLevel = fuel.fuelLevel;
        if (fuelLevel <= 0f) return;
        if (activeCount >= maxConcurrentEnemies) return;
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        timer += Time.deltaTime;
        float interval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, fuelLevel);
        if (timer < interval) return;

        timer = 0f;
        Spawn();
    }

    void Spawn()
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector2 pos = RandomPointInRing();
        if (WorldBounds.Instance != null) pos = WorldBounds.Instance.ClampPoint(pos);

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        activeCount++;

        Health health = enemy.GetComponent<Health>();
        if (health != null) health.Died += () => activeCount--;
    }

    Vector2 RandomPointInRing()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(minRadius, maxRadius);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        return (Vector2)transform.position + offset;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }
}

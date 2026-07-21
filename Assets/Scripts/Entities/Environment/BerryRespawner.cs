using UnityEngine;

// Owns a bush's berries: spawns one as a child on start, and once it's gone
// (picked up -- either destroyed via a hand-stack merge or reparented onto a
// hand) waits respawnTime before growing a new one. The Bush sprite itself
// never leaves this GameObject; only the pickupable Berries child does.
public class BerryRespawner : MonoBehaviour
{
    public GameObject berriesPrefab;
    public float respawnTime = 60f;

    GameObject current;
    float timer;

    void Start() => SpawnBerries();

    void Update()
    {
        if (current != null && current.transform.parent == transform) return;

        timer += Time.deltaTime;
        if (timer < respawnTime) return;

        timer = 0f;
        SpawnBerries();
    }

    void SpawnBerries() =>
        current = Instantiate(berriesPrefab, transform.position, Quaternion.identity, transform);
}

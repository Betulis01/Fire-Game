using System;
using UnityEngine;

// Despawns this enemy when the game clock crosses despawnHour (9:00 by default),
// so the night's monsters vanish in the morning instead of lingering all day.
// Raises Despawned before destroying, so EnemySpawner can release its slot in the
// global enemy cap (a despawn never fires Health.Died, which the spawner also counts).
public class EnemyDespawner : MonoBehaviour
{
    [Range(0f, 24f)] public float despawnHour = 9f;

    // raised just before the object is destroyed by a despawn (not by death)
    public event Action Despawned;

    float lastHour;
    bool hasLastHour;

    void Update()
    {
        DayNightCycle clock = DayNightCycle.Instance;
        if (clock == null) return;

        float hour = clock.Hour;

        // seed on the first tick so an enemy never despawns the frame it spawns
        if (!hasLastHour)
        {
            lastHour = hour;
            hasLastHour = true;
            return;
        }

        // the clock only moves forward, so we crossed despawnHour exactly when the
        // wrap-aware distance past it shrank between frames (24 -> 0 rollover)
        bool crossed = Mathf.Repeat(hour - despawnHour, 24f)
                     < Mathf.Repeat(lastHour - despawnHour, 24f);
        lastHour = hour;
        if (!crossed) return;

        Despawned?.Invoke();
        Destroy(gameObject);
    }
}

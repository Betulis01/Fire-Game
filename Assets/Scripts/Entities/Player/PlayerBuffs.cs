using System.Collections.Generic;
using UnityEngine;

// Tracks timed stat modifiers from food (and anything else that wants a
// temporary buff/debuff): movement-speed multipliers and max-health deltas.
// Speed modifiers stack multiplicatively; PlayerController folds
// SpeedMultiplier into its own temperature-driven speed calc. Max-health
// deltas are a stat source: CharacterStats adds them to max health while they
// last (it owns Health.maxHealth).
[RequireComponent(typeof(CharacterStats))]
public class PlayerBuffs : MonoBehaviour, IStatSource
{
    class SpeedBuff
    {
        public float multiplier;
        public float remaining;
    }

    class MaxHealthBuff
    {
        public float amount;
        public float remaining;
    }

    readonly List<SpeedBuff> speedBuffs = new();
    readonly List<MaxHealthBuff> healthBuffs = new();

    public float SpeedMultiplier { get; private set; } = 1f;
    public event System.Action StatsChanged;

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        speedBuffs.Add(new SpeedBuff { multiplier = multiplier, remaining = duration });
        Recalculate();
    }

    public void ApplyMaxHealthBuff(float amount, float duration)
    {
        healthBuffs.Add(new MaxHealthBuff { amount = amount, remaining = duration });
        StatsChanged?.Invoke();
    }

    public void AddStats(ref StatBlock total)
    {
        foreach (MaxHealthBuff b in healthBuffs) total.maxHealth += b.amount;
    }

    void Update()
    {
        if (speedBuffs.Count > 0)
        {
            for (int i = speedBuffs.Count - 1; i >= 0; i--)
            {
                speedBuffs[i].remaining -= Time.deltaTime;
                if (speedBuffs[i].remaining <= 0f) speedBuffs.RemoveAt(i);
            }
            Recalculate();
        }

        for (int i = healthBuffs.Count - 1; i >= 0; i--)
        {
            healthBuffs[i].remaining -= Time.deltaTime;
            if (healthBuffs[i].remaining <= 0f)
            {
                healthBuffs.RemoveAt(i);
                StatsChanged?.Invoke();
            }
        }
    }

    void Recalculate()
    {
        float m = 1f;
        foreach (SpeedBuff b in speedBuffs) m *= b.multiplier;
        SpeedMultiplier = m;
    }
}

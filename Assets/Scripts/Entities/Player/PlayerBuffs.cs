using System.Collections.Generic;
using UnityEngine;

// Tracks timed stat modifiers from food (and anything else that wants a
// temporary buff/debuff): movement-speed multipliers and max-health deltas.
// Speed modifiers stack multiplicatively; PlayerController folds
// SpeedMultiplier into its own temperature-driven speed calc. Max-health
// deltas are applied directly to Health.maxHealth and reverted exactly when
// they expire.
[RequireComponent(typeof(Health))]
public class PlayerBuffs : MonoBehaviour
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

    Health health;

    readonly List<SpeedBuff> speedBuffs = new();
    readonly List<MaxHealthBuff> healthBuffs = new();

    public float SpeedMultiplier { get; private set; } = 1f;

    void Awake() => health = GetComponent<Health>();

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        speedBuffs.Add(new SpeedBuff { multiplier = multiplier, remaining = duration });
        Recalculate();
    }

    public void ApplyMaxHealthBuff(float amount, float duration)
    {
        healthBuffs.Add(new MaxHealthBuff { amount = amount, remaining = duration });
        health.maxHealth += amount;
        health.current = Mathf.Min(health.current, health.maxHealth);
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
                health.maxHealth -= healthBuffs[i].amount;
                health.current = Mathf.Min(health.current, health.maxHealth);
                healthBuffs.RemoveAt(i);
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

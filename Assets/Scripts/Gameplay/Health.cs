using System;
using UnityEngine;

// General hit-point health for entities (trees, mobs, ...). Player freezing lives
// in PlayerHealth instead. Consumers react to Died (e.g. Choppable spawns drops).
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float current;

    public event Action Died;
    public bool IsDead => current <= 0f;
    public float Normalized => maxHealth > 0f ? current / maxHealth : 0f;

    void Awake() => current = maxHealth;

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead) return;

        current = Mathf.Max(0f, current - amount);
        if (current <= 0f) Died?.Invoke();
    }
}

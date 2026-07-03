using System;
using UnityEngine;

// General hit-point health for entities (trees, mobs, ...). Player freezing lives
// in PlayerHealth instead. Consumers react to Died (e.g. DropOnDeath spawns drops).
// As an IHitReactor it turns a landed hit into damage.
public class Health : MonoBehaviour, IHitReactor
{
    public float maxHealth = 100f;
    public float current;

    public event Action Died;
    public event Action<DamageInfo> Damaged;
    public bool IsDead => current <= 0f;
    public float Normalized => maxHealth > 0f ? current / maxHealth : 0f;

    protected virtual void Awake() => current = maxHealth;

    public void TakeDamage(float amount, DamageType type = DamageType.Combat, Vector2? point = null)
    {
        if (amount <= 0f || IsDead) return;

        // debug: the player can't lose health while invincibility is on
        if (DebugInvincibility.Enabled && GetComponent<PlayerHealth>() != null) return;

        float before = current;
        current = Mathf.Max(0f, current - amount);
        Damaged?.Invoke(new DamageInfo(before - current, type, point));
        if (current <= 0f) Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        current = Mathf.Min(maxHealth, current + amount);
    }

    // React to a landed hit by taking its damage (kind-gating already happened in Hitbox).
    public void OnHit(in HitInfo hit) => TakeDamage(hit.damage, DamageType.Combat, hit.point);
}

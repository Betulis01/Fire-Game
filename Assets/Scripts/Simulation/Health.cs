using System;
using UnityEngine;

// General hit-point health for entities (trees, mobs, ...). Player freezing lives
// in PlayerHealth instead. Consumers react to Died (e.g. DropOnDeath spawns drops).
// As an IHitReactor it turns a landed hit into damage. Damage carries its attacker
// so the attacker's IDamageDealtReactor/IKillReactor components (XP, ...) hear
// about it without subscribing to every victim.
public class Health : MonoBehaviour, IHitReactor
{
    public float maxHealth = 100f;
    public float current;

    public event Action Died;
    public event Action<DamageInfo> Damaged;
    public event Action<float> Healed;
    public bool IsDead => current <= 0f;
    public GameObject Killer { get; private set; }   // attacker of the killing blow; null if environmental
    public float Normalized => maxHealth > 0f ? current / maxHealth : 0f;

    protected virtual void Awake() => current = maxHealth;

    public void TakeDamage(float amount, DamageType type = DamageType.Combat, Vector2? point = null,
                           GameObject attacker = null, ToolKind kind = ToolKind.None, bool crit = false)
    {
        if (amount <= 0f || IsDead) return;

        // debug: the player can't lose health while invincibility is on
        if (DebugInvincibility.Enabled && GetComponent<PlayerHealth>() != null) return;

        float before = current;
        current = Mathf.Max(0f, current - amount);
        DamageInfo info = new(before - current, type, point, attacker, kind, crit);
        Damaged?.Invoke(info);

        if (attacker != null)
            foreach (IDamageDealtReactor r in attacker.GetComponents<IDamageDealtReactor>())
                r.OnDealtDamage(this, info);

        if (current > 0f) return;

        // kill reactors run before Died: Died handlers disable/destroy the victim
        Killer = attacker;
        if (attacker != null)
            foreach (IKillReactor r in attacker.GetComponents<IKillReactor>())
                r.OnKilled(this, info);
        Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || IsDead) return;
        float before = current;
        current = Mathf.Min(maxHealth, current + amount);
        if (current > before) Healed?.Invoke(current - before);
    }

    // React to a landed hit by taking its damage -- already resolved for this victim
    // (weapon roll, attack power, ability scaling, crit, armor; DamageCalculator) --
    // unless a ToolDamageFilter on this entity rejects the striking tool's kind (the
    // hit itself still registered — knockback/VFX/recoil already ran off
    // Hurtbox.TakeHit — only damage is skipped). A ShieldBlock that's actively
    // blocking scales the damage down the same way, after armor — HitInfo/AttackData
    // are immutable, so this is the one place to gate it.
    public void OnHit(in HitInfo hit)
    {
        ToolDamageFilter gate = GetComponent<ToolDamageFilter>();
        if (gate != null && !gate.Accepts(hit.kind)) return;

        ShieldBlock block = GetComponent<ShieldBlock>();
        float damage = block != null && block.IsBlocking ? hit.damage * (1f - block.DamageReduction) : hit.damage;

        TakeDamage(damage, DamageType.Combat, hit.point, hit.attacker, hit.kind, hit.isCrit);
    }
}

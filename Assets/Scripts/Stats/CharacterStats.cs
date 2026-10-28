using System;
using UnityEngine;

// A character's stats (player and enemies alike). Tier 1: the base StatBlock plus
// every IStatSource on this object (level growth, worn gear, passives, buffs).
// Tier 2: the derived combat stats, recalculated whenever a source changes:
//   Attack Power = STR x2 + AGI x1         (scales weapon damage)
//   Crit Chance  = base % + AGI x0.05 %
//   Spell Power  = INT x2                   (no spell uses it yet)
//   Max Health   = baseHealth + STA x10 + flat bonuses
// It owns Health.maxHealth. Things without CharacterStats (trees, ore) aren't
// characters: hits on them skip the stat pipeline (DamageCalculator).
[RequireComponent(typeof(Health))]
public class CharacterStats : MonoBehaviour
{
    [Tooltip("Attributes and armor before any source (level, gear, passives).")]
    [SerializeField] StatBlock baseStats;

    [Tooltip("Max health before stamina and flat bonuses.")]
    [SerializeField] float baseHealth = 100f;

    [Tooltip("Crit chance in percent before agility.")]
    [SerializeField] float baseCritChance = 5f;

    [Tooltip("Damage multiplier of a critical hit.")]
    [SerializeField] float critMultiplier = 1.5f;

    public StatBlock Attributes { get; private set; }
    public float AttackPower { get; private set; }
    public float CritChance { get; private set; }       // percent
    public float CritMultiplier => critMultiplier;
    public float SpellPower { get; private set; }
    public float Armor { get; private set; }
    public float MaxHealth { get; private set; }

    public event Action Changed;

    Health health;
    IStatSource[] sources;
    bool initialized;

    void Awake()
    {
        health = GetComponent<Health>();
        sources = GetComponents<IStatSource>();
        foreach (IStatSource s in sources) s.StatsChanged += Recalculate;
    }

    // After every source's Awake (PlayerExperience builds its track there).
    void Start() => Recalculate();

    void OnDestroy()
    {
        if (sources == null) return;
        foreach (IStatSource s in sources) s.StatsChanged -= Recalculate;
    }

    public void Recalculate()
    {
        if (health == null) return;   // a source changed before Awake
        StatBlock total = baseStats;
        foreach (IStatSource s in sources) s.AddStats(ref total);

        Attributes = total;
        AttackPower = total.strength * StatFormulas.AttackPowerPerStrength
                    + total.agility * StatFormulas.AttackPowerPerAgility;
        CritChance = baseCritChance + total.agility * StatFormulas.CritPercentPerAgility;
        SpellPower = total.intelligence * StatFormulas.SpellPowerPerIntelligence;
        Armor = Mathf.Max(0f, total.armor);
        MaxHealth = Mathf.Max(1f, baseHealth + total.stamina * StatFormulas.HealthPerStamina + total.maxHealth);

        ApplyMaxHealth();
        Changed?.Invoke();
    }

    // Spawn full; afterwards a higher max heals by the gain, a lower one clamps.
    void ApplyMaxHealth()
    {
        float previous = health.maxHealth;
        health.maxHealth = MaxHealth;

        if (!initialized)
        {
            initialized = true;
            health.current = MaxHealth;
            return;
        }
        if (health.IsDead) return;
        if (MaxHealth > previous) health.current += MaxHealth - previous;
        health.current = Mathf.Min(health.current, MaxHealth);
    }
}

using System;

// A bundle of Tier 1 stats: the core attributes plus the flat bonuses that don't
// come from an attribute (armor, max health). Authored on a character (its base),
// on gear (Equippable.stats), per skill-node rank, per level of growth -- and
// summed by CharacterStats into the character's totals.
[Serializable]
public struct StatBlock
{
    public float strength;
    public float agility;
    public float intelligence;
    public float stamina;
    public float armor;
    public float maxHealth;   // flat, on top of what stamina gives (e.g. a food buff)

    public static StatBlock operator +(StatBlock a, StatBlock b) => new()
    {
        strength = a.strength + b.strength,
        agility = a.agility + b.agility,
        intelligence = a.intelligence + b.intelligence,
        stamina = a.stamina + b.stamina,
        armor = a.armor + b.armor,
        maxHealth = a.maxHealth + b.maxHealth,
    };

    public static StatBlock operator *(StatBlock a, float k) => new()
    {
        strength = a.strength * k,
        agility = a.agility * k,
        intelligence = a.intelligence * k,
        stamina = a.stamina * k,
        armor = a.armor * k,
        maxHealth = a.maxHealth * k,
    };
}

// Implemented by components on a character that contribute stats (level growth,
// worn gear, learned passives, buffs). CharacterStats on the same object sums them
// and recalculates whenever one raises StatsChanged.
public interface IStatSource
{
    void AddStats(ref StatBlock total);
    event Action StatsChanged;
}

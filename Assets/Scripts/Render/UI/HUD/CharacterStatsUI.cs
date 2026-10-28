using TMPro;
using UnityEngine;

// Player page: the player's attributes and derived combat stats (CharacterStats),
// one label/value row each. Refreshed whenever they change -- a level up, gear
// worn or removed, a passive bought, a buff starting or ending.
public class CharacterStatsUI : MonoBehaviour
{
    public enum Row { Strength, Agility, Intelligence, Stamina, AttackPower, CritChance, CritDamage, SpellPower, Armor, MaxHealth }

    // Row labels, indexed by Row; HudBuilder lays the rows out from these, with a
    // gap before AttackPower (attributes above, what they turn into below).
    public static readonly string[] Labels =
    {
        "Strength", "Agility", "Intelligence", "Stamina",
        "Attack Power", "Crit Chance", "Crit Damage", "Spell Power", "Armor", "Max Health",
    };

    [SerializeField] HudRoot root;
    [Tooltip("Value text per Row, in Row order.")]
    [SerializeField] TMP_Text[] values;

    CharacterStats stats;

    void OnEnable()
    {
        stats = root != null ? root.Stats : null;
        if (stats != null) stats.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (stats != null) stats.Changed -= Refresh;
    }

    void Refresh()
    {
        if (stats == null) return;
        StatBlock a = stats.Attributes;

        Set(Row.Strength, a.strength.ToString("0"));
        Set(Row.Agility, a.agility.ToString("0"));
        Set(Row.Intelligence, a.intelligence.ToString("0"));
        Set(Row.Stamina, a.stamina.ToString("0"));
        Set(Row.AttackPower, stats.AttackPower.ToString("0"));
        Set(Row.CritChance, stats.CritChance.ToString("0.##") + "%");
        Set(Row.CritDamage, (stats.CritMultiplier * 100f).ToString("0") + "%");
        Set(Row.SpellPower, stats.SpellPower.ToString("0"));
        Set(Row.Armor, $"{stats.Armor:0} ({StatFormulas.DamageReduction(stats.Armor) * 100f:0}%)");
        Set(Row.MaxHealth, stats.MaxHealth.ToString("0"));
    }

    void Set(Row row, string text)
    {
        int i = (int)row;
        if (values != null && i < values.Length && values[i] != null && values[i].text != text) values[i].text = text;
    }
}

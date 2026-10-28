using System;
using UnityEngine;

// Attacker-side reactor: the player's level. Health tells it (IKillReactor) when
// one of the player's blows finishes a victim; only enemies (EnemyBrain) are worth
// XP -- trees, ore and the target dummy die to the player too but give nothing.
// Each kill floats a small purple "+N" at the enemy. Read by the HUD (PlayerStatsUI).
// As an IStatSource each level past the first adds growthPerLevel to the player's
// CharacterStats.
public class PlayerExperience : MonoBehaviour, IKillReactor, IStatSource
{
    [SerializeField] int xpPerKill = 2;
    [SerializeField] int xpPerLevel = 10;
    [SerializeField] int startLevel = 1;
    [Tooltip("Attributes gained per level past the first.")]
    [SerializeField] StatBlock growthPerLevel = new() { strength = 1f, agility = 1f, intelligence = 1f, stamina = 1f };
    [Tooltip("World offset of the XP popup from the enemy's position (above the damage numbers).")]
    [SerializeField] Vector3 popupOffset = new(0f, 1.3f, 0f);

    ExperienceTrack track;

    public int Level => track.level;
    public int Xp => track.xp;
    public int XpPerLevel => track.xpPerLevel;
    public float Normalized => track.Normalized;

    public event Action Changed;
    public event Action<int> LeveledUp;   // new level
    public event Action StatsChanged;

    void Awake() => track = new ExperienceTrack(startLevel, xpPerLevel);

    public void OnKilled(Health victim, in DamageInfo killingBlow)
    {
        if (victim.GetComponent<EnemyBrain>() == null) return;

        Add(xpPerKill);
        CombatText.SpawnText(victim.transform.position + popupOffset, "+" + xpPerKill, PopupStyle.Experience);
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;
        int gained = track.Add(amount);
        Changed?.Invoke();
        if (gained > 0)
        {
            LeveledUp?.Invoke(track.level);
            StatsChanged?.Invoke();
        }
    }

    public void AddStats(ref StatBlock total)
    {
        if (track != null && track.level > startLevel) total += growthPerLevel * (track.level - startLevel);
    }
}

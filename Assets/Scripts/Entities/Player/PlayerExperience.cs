using System;
using UnityEngine;

// Attacker-side reactor: the player's level. Health tells it (IKillReactor) when
// one of the player's blows finishes a victim; only enemies (EnemyBrain) are worth
// XP -- trees, ore and the target dummy die to the player too but give nothing.
// Each kill floats a small purple "+N" at the enemy. Read by the HUD (PlayerStatsUI).
public class PlayerExperience : MonoBehaviour, IKillReactor
{
    [SerializeField] int xpPerKill = 2;
    [SerializeField] int xpPerLevel = 10;
    [SerializeField] int startLevel = 1;
    [Tooltip("World offset of the XP popup from the enemy's position (above the damage numbers).")]
    [SerializeField] Vector3 popupOffset = new(0f, 1.3f, 0f);

    ExperienceTrack track;

    public int Level => track.level;
    public int Xp => track.xp;
    public int XpPerLevel => track.xpPerLevel;
    public float Normalized => track.Normalized;

    public event Action Changed;
    public event Action<int> LeveledUp;   // new level

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
        if (gained > 0) LeveledUp?.Invoke(track.level);
    }
}

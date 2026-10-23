using System;

// One level/XP counter with a flat cost per level: XP fills toward xpPerLevel,
// then wraps into the next level (the HUD bar empties again). Shared by
// PlayerExperience (player level) and WeaponExperience (one per weapon kind).
[Serializable]
public class ExperienceTrack
{
    public int level;
    public int xp;
    public int xpPerLevel;

    public ExperienceTrack(int startLevel, int xpPerLevel)
    {
        level = startLevel;
        this.xpPerLevel = Math.Max(1, xpPerLevel);
    }

    public float Normalized => (float)xp / xpPerLevel;

    // Add XP; returns how many levels were gained (0 most of the time).
    public int Add(int amount)
    {
        if (amount <= 0) return 0;
        xp += amount;
        int gained = 0;
        while (xp >= xpPerLevel)
        {
            xp -= xpPerLevel;
            level++;
            gained++;
        }
        return gained;
    }
}

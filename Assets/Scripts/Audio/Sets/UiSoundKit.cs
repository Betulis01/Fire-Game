using UnityEngine;

// The HUD and menus' small set of shared sounds (sound.md §10.1 and the few unique UI
// ones), by role: every UI handler asks for a role (UiSfx.Play(UiSound.Place)) and this
// maps it to its event, so a click sounds the same wherever it is and retuning the UI is
// one asset. On AudioConfig.
public enum UiSound
{
    Click, Hover, Denied,
    Pickup, Place, Swap, Return, Toss,
    Open, Close, Tab, Subtab, SliderTick,
    SpellAssign, SkillBuy, SkillMaxed, SpellLearn,
    MainPlay,
}

[CreateAssetMenu(menuName = "Audio/UI Sound Kit", fileName = "UiSoundKit")]
public class UiSoundKit : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public UiSound role;
        public SoundEvent sound;
    }

    public Entry[] entries = new Entry[0];

    public SoundEvent Get(UiSound role)
    {
        foreach (Entry e in entries)
            if (e.role == role) return e.sound;
        return null;
    }
}

// The UI's one call: a role from the kit, non-positional, on the UI bus (the events'
// own routing), heard while paused.
public static class UiSfx
{
    public static void Play(UiSound role, float volume = 1f, float pitch = 1f)
    {
        AudioConfig config = AudioSystem.ActiveConfig;
        if (config == null || config.ui == null) return;
        Sfx.Play2D(config.ui.Get(role), volume, pitch);
    }
}

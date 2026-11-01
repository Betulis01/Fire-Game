using UnityEngine;
using UnityEngine.Audio;

public enum AudioChannel { Master, Music, Ambience, Sfx, Ui }

// The player's volume settings: one 0..1 slider value per channel in PlayerPrefs,
// applied to the mixer's exposed group volumes (MasterVolume, MusicVolume, ...).
// Those five parameters belong to the settings alone -- snapshots never touch them,
// since a parameter set from code stops following snapshots (SoundSystemPlan §3.6).
// Sliders are linear; the mixer is in dB: 20 x log10(value), floored at -80 dB.
public static class AudioSettingsStore
{
    static readonly string[] Parameters = { "MasterVolume", "MusicVolume", "AmbienceVolume", "SfxVolume", "UiVolume" };
    const string MuteKey = "audio.muteUnfocused";
    static bool warned;

    public static string Parameter(AudioChannel channel) => Parameters[(int)channel];

    public static float Get(AudioChannel channel) => PlayerPrefs.GetFloat("audio." + channel, 1f);

    public static void Set(AudioChannel channel, float value)
    {
        PlayerPrefs.SetFloat("audio." + channel, Mathf.Clamp01(value));
        Apply(channel);
    }

    // Silence the game while its window isn't focused (AudioSystem.OnApplicationFocus).
    public static bool MuteWhenUnfocused
    {
        get => PlayerPrefs.GetInt(MuteKey, 1) == 1;
        set => PlayerPrefs.SetInt(MuteKey, value ? 1 : 0);
    }

    public static float ToDb(float value) => value <= 0.0001f ? -80f : 20f * Mathf.Log10(value);

    public static void ApplyAll()
    {
        for (int i = 0; i < Parameters.Length; i++) Apply((AudioChannel)i);
    }

    static void Apply(AudioChannel channel)
    {
        AudioMixer mixer = AudioSystem.Instance != null ? AudioSystem.Instance.Mixer : null;
        if (mixer == null) return;
        if (!mixer.SetFloat(Parameter(channel), ToDb(Get(channel))) && !warned)
        {
            warned = true;
            Debug.LogWarning($"Audio mixer '{mixer.name}' has no exposed parameter '{Parameter(channel)}' " +
                             "(see SoundSystemPlan.md, the mixer checklist).");
        }
    }
}

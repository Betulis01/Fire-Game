using UnityEngine;
using UnityEngine.Audio;

public enum AudioChannel { Master, Music, Ambience, Sfx, Ui }

// The player's volume settings: one 0..1 slider value per channel in PlayerPrefs,
// applied to the mixer's exposed group volumes (MasterVolume, MusicVolume, ...).
// Those parameters belong to the settings alone -- snapshots never touch them, since a
// parameter set from code stops following snapshots (SoundSystemPlan §3.6).
// Sliders are linear; the mixer is in dB: 20 x log10(value), floored at -80 dB, plus the
// bus's trim from AudioConfig (music and ambience sit under the effects at full slider).
//
// Ambience is the exception: the snapshots dip its fader (paused, the menu, the end), so
// the slider and trim can't take it over. They're applied in code instead -- AudioSystem
// multiplies every voice on the Ambience group by AmbienceGain.
public static class AudioSettingsStore
{
    static readonly string[] Parameters = { "MasterVolume", "MusicVolume", "AmbienceVolume", "SfxVolume", "UiVolume" };
    const string MuteKey = "audio.muteUnfocused";
    static bool warned;

    // the Ambience slider and trim as a linear gain (AudioSystem reads it every frame)
    public static float AmbienceGain { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { AmbienceGain = 1f; warned = false; }

    public static string Parameter(AudioChannel channel) => Parameters[(int)channel];

    // whether the channel is a mixer parameter (the rest are applied in code)
    public static bool OnMixer(AudioChannel channel) => channel != AudioChannel.Ambience;

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
        AudioSystem sys = AudioSystem.Instance;
        // the bus's trim (AudioConfig: its place in the mix) under the player's slider
        float trim = sys != null && sys.Config != null ? sys.Config.TrimDb(channel) : 0f;
        float db = Mathf.Max(-80f, trim + ToDb(Get(channel)));
        if (!OnMixer(channel)) { AmbienceGain = AudioSystem.DbToLinear(db); return; }

        AudioMixer mixer = sys != null ? sys.Mixer : null;
        if (mixer == null) return;
        if (!mixer.SetFloat(Parameter(channel), db) && !warned)
        {
            warned = true;
            Debug.LogWarning($"Audio mixer '{mixer.name}' has no exposed parameter '{Parameter(channel)}' " +
                             "(see SoundSystemPlan.md, the mixer checklist).");
        }
    }
}

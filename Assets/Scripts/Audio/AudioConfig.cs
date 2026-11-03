using UnityEngine;
using UnityEngine.Audio;

// The audio layer's global tuning, loaded by AudioSystem from Resources
// (Assets/Audio/Resources/AudioConfig.asset; Tools/Audio/Sync Events From sound.md
// creates it). Read live, so the hearing curve can be tuned in play mode.
[CreateAssetMenu(menuName = "Audio/Audio Config", fileName = "AudioConfig")]
public class AudioConfig : ScriptableObject
{
    public const string ResourcePath = "AudioConfig";

    [Tooltip("Assets/Audio/FireGame.mixer. Empty = everything plays straight to Master, no snapshots or volume settings.")]
    public AudioMixer mixer;

    [Header("Voices")]
    [Min(4)] public int oneShotVoices = 28;
    [Min(2)] public int loopVoices = 16;
    [Tooltip("New one-shots allowed per frame; the rest are dropped (Critical ones always start).")]
    [Min(1)] public int maxStartsPerFrame = 10;

    [Header("Hearing (2D, from the camera's view)")]
    [Tooltip("Hearing radius = max(minRadius, this x half the visible width) x the event's rangeScale.")]
    [Min(0.5f)] public float radiusPerHalfWidth = 1.6f;
    [Min(0.5f)] public float minRadius = 6f;
    [Tooltip("Gain by distance / radius (0 = at the camera's centre, 1 = the radius).")]
    public AnimationCurve falloff = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f), new Keyframe(0.25f, 1f, 0f, 0f), new Keyframe(1f, 0f, 0f, 0f));
    [Tooltip("How far a sound at the screen's edge pans (0 = mono, 1 = hard left/right).")]
    [Range(0f, 1f)] public float panWidth = 0.55f;
    [Tooltip("A one-shot quieter than this when it would start isn't started at all.")]
    [Range(-80f, -20f)] public float cullDb = -40f;
    [Tooltip("A loop gives its voice back beyond this many radii (and takes one again inside 1).")]
    [Min(1f)] public float loopReleaseRadii = 1.2f;

    [Header("Shared sounds")]
    [Tooltip("The ground's steps and landings, per GroundType (FootstepAudio, landings).")]
    public GroundSounds groundSounds;
    [Tooltip("Layered on any material's impact when the hit crits (SurfaceAudio).")]
    public SoundEvent critLayer;
    [Tooltip("Item handling and building layers (ItemAudioDirector, BuildAudio).")]
    public WorldSounds world;
    [Tooltip("The HUD and menus' sounds by role (UiSfx).")]
    public UiSoundKit ui;
    [Tooltip("Wind beds, scattered ambience, time-of-day stingers (AmbienceDirector).")]
    public AmbienceSounds ambience;
    [Tooltip("Base loops, layers, end stings (MusicDirector).")]
    public MusicSounds music;

    [Header("Bus trims (dB, added to the player's volume sliders)")]
    [Tooltip("The buses the settings sliders own can't be levelled in the mixer (code sets them), so their place " +
             "in the mix lives here. The other buses (Player, Combat, Creatures, World, Stingers) are set in the mixer's snapshots.")]
    [Range(-30f, 0f)] public float masterTrimDb = 0f;
    [Range(-30f, 0f)] public float musicTrimDb = -8f;
    [Range(-30f, 0f)] public float ambienceTrimDb = -10f;
    [Range(-30f, 0f)] public float sfxTrimDb = 0f;
    [Range(-30f, 0f)] public float uiTrimDb = -6f;

    [Header("Danger duck (SoundEvent.duckOthers)")]
    [Tooltip("Mixer groups, by name, that dip while a ducking one-shot plays (the player hurt, an aggro).")]
    public string[] duckGroups = { "World", "Creatures" };
    [Range(-12f, 0f)] public float duckDb = -3f;
    [Min(0.001f)] public float duckAttack = 0.05f;
    [Min(0.001f)] public float duckRelease = 0.6f;

    [Header("Continuous mix (MixDirector; exposed mixer parameters, never in a snapshot)")]
    [Tooltip("Cutoff of the Ambience group's second Lowpass: closes as heat reaches the player.")]
    public string heatCutoffParam = "AmbienceHeatCutoff";
    [Tooltip("Cutoff of the SFX group's Lowpass: closes below lowHealth.")]
    public string lowHealthCutoffParam = "SfxLowHealthCutoff";
    [Tooltip("Cutoff of the World group's Highpass: opens while freezing.")]
    public string freezeCutoffParam = "WorldFreezeCutoff";
    [Tooltip("Warmth (HeatSource units) at which the wind is fully sheltered.")]
    [Min(0.1f)] public float fullShelterWarmth = 15f;
    [Tooltip("Ambience cutoff at full shelter, Hz.")]
    [Min(200f)] public float shelterCutoff = 1600f;
    [Tooltip("SFX cutoff at the edge of death, Hz.")]
    [Min(200f)] public float lowHealthCutoff = 1800f;
    [Range(0f, 1f)] public float lowHealth = 0.25f;
    [Tooltip("World highpass while freezing, Hz (the world thins out).")]
    [Min(10f)] public float freezeCutoff = 350f;

    [Header("Bark budget (CreatureAudio idle sounds)")]
    [Tooltip("At most one idle bark in the whole world per this many seconds.")]
    [Min(0f)] public float barkGap = 1.2f;
    [Tooltip("At most one idle bark per creature kind (voice) per this many seconds.")]
    [Min(0f)] public float barkGapPerKind = 3f;

    [Header("Snapshots (by name in the mixer)")]
    public string gameplaySnapshot = "Gameplay";
    public string pausedSnapshot = "Paused";
    public string menuSnapshot = "Menu";
    public string endSnapshot = "End";
    [Min(0f)] public float snapshotFade = 0.3f;
    [Min(0f)] public float endFade = 1.5f;

    [Header("Debug overlay (F9)")]
    [Tooltip("F10 plays the selected one at the cursor (a loop toggles), F11 fires it 100 times, F8 selects the next.")]
    public SoundEvent[] debugTestEvents = new SoundEvent[0];

    public float TrimDb(AudioChannel channel) => channel switch
    {
        AudioChannel.Master => masterTrimDb,
        AudioChannel.Music => musicTrimDb,
        AudioChannel.Ambience => ambienceTrimDb,
        AudioChannel.Sfx => sfxTrimDb,
        AudioChannel.Ui => uiTrimDb,
        _ => 0f,
    };

    // trims tuned in play mode are heard at once
    void OnValidate()
    {
        if (Application.isPlaying && AudioSystem.Instance != null && AudioSystem.Instance.Config == this)
            AudioSettingsStore.ApplyAll();
    }

    public static AudioConfig LoadOrDefault()
    {
        AudioConfig config = Resources.Load<AudioConfig>(ResourcePath);
        if (config != null) return config;
        Debug.LogWarning("AudioConfig not found in Resources; using defaults (no mixer). " +
                         "Run Tools/Audio/Sync Events From sound.md to create it.");
        return CreateInstance<AudioConfig>();
    }
}

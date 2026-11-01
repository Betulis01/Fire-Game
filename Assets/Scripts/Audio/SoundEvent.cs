using UnityEngine;
using UnityEngine.Audio;

// One sound the game makes -- an ID from sound.md -- and how it plays: its variants,
// level, jitter, bus, hearing range and limits. Pure data: everything that changes
// while playing (the last start, the shuffle bag, the live voices) lives in
// AudioSystem, so nothing carries over between Play sessions in the editor. Played
// through Sfx. An event with no clips is legal and plays silence, so the whole sound
// map can be wired before any audio exists (Tools/Audio/Validate lists what's empty).
//
// The Catalogue fields are written by Tools/Audio/Sync Events From sound.md: what the
// sound should be, its priority and section, so the asset tells whoever records it
// what to make.
[CreateAssetMenu(menuName = "Audio/Sound Event", fileName = "sfx_new")]
public class SoundEvent : ScriptableObject
{
    public enum Selection { ShuffleNoRepeat, Random, Sequential }
    public enum SoundSpace { World, Screen }
    public enum StealMode { Oldest, Quietest, None }
    public enum SoundPriority { Low, Normal, High, Critical }

    [Tooltip("The variants; one is picked per play.")]
    public AudioClip[] clips = new AudioClip[0];
    [Tooltip("ShuffleNoRepeat: every variant plays before any repeats, never the same twice in a row.")]
    public Selection selection = Selection.ShuffleNoRepeat;

    [Header("Level")]
    [Tooltip("Base level. No boost above 0 dB: balance on the mixer instead.")]
    [Range(-40f, 0f)] public float volumeDb = 0f;
    [Tooltip("± random level per play, in dB.")]
    [Range(0f, 6f)] public float volumeJitterDb = 1.5f;
    [Tooltip("± random pitch per play, in semitones (0.8 is about ±5 %).")]
    [Range(0f, 4f)] public float pitchJitterSemitones = 0.8f;

    [Header("Routing")]
    [Tooltip("The mixer bus. Empty = Master.")]
    public AudioMixerGroup output;
    [Tooltip("World: attenuated and panned from where it plays. Screen: non-positional (UI, player status, music).")]
    public SoundSpace space = SoundSpace.World;
    [Tooltip("Multiplies the hearing radius (AudioConfig): footsteps ~0.6, a falling tree ~1.6.")]
    [Min(0.05f)] public float rangeScale = 1f;
    [Tooltip("A loop: played with Sfx.PlayLoop, held until its handle stops it.")]
    public bool loop;
    [Tooltip("Keeps playing while the game is paused (UI, music, menu stingers).")]
    public bool ignorePause;

    [Header("Limits")]
    [Tooltip("Minimum seconds between two starts of this event (unscaled time).")]
    [Min(0f)] public float cooldown = 0.04f;
    [Tooltip("At most this many voices of this event at once.")]
    [Min(1)] public int maxVoices = 4;
    [Tooltip("At the voice cap: stop the oldest or quietest of this event, or refuse the new one.")]
    public StealMode steal = StealMode.Oldest;
    [Tooltip("When the whole pool is full, lower priorities are stolen first.")]
    public SoundPriority priority = SoundPriority.Normal;

    [Header("Extras")]
    [Tooltip("Events started together with this one (each keeps its own limits and bus).")]
    public SoundEvent[] layers = new SoundEvent[0];
    [Min(0f)] public float startDelay;
    [Tooltip("Up to this much extra random delay.")]
    [Min(0f)] public float startDelayJitter;

    [Header("Catalogue (from sound.md)")]
    [TextArea(2, 6)] public string brief;
    public string catalogPriority;   // "P1", "P2", "P3"
    public string catalogSection;    // e.g. "3.1 Movement"
    public int catalogVariants;      // how many variants sound.md asks for

    public bool HasClips
    {
        get
        {
            if (clips == null) return false;
            foreach (AudioClip c in clips) if (c != null) return true;
            return false;
        }
    }
}

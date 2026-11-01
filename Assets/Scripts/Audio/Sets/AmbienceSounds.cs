using UnityEngine;

// The world's air (sound.md §11.1, §11.2), played by AmbienceDirector: the wind beds
// (day, night) crossfaded by the hour, and the scattered one-shots on their own clocks --
// gusts (more often at night), trunks creaking near trees, the first birds of dawn,
// owls and far howls at night -- plus the time-of-day stingers. On AudioConfig.
[CreateAssetMenu(menuName = "Audio/Ambience Sounds", fileName = "AmbienceSounds")]
public class AmbienceSounds : ScriptableObject
{
    [System.Serializable]
    public struct Scatter
    {
        public SoundEvent sound;
        [Tooltip("Seconds between plays, random in this range.")]
        public Vector2 interval;
    }

    [Header("Beds")]
    public SoundEvent windDay;
    public SoundEvent windNight;

    [Header("Scattered")]
    public Scatter gust;
    [Tooltip("At night gusts come this much more often (interval x this).")]
    [Range(0.1f, 1f)] public float nightGustFactor = 0.6f;
    [Tooltip("Played at a random tree within hearing.")]
    public Scatter forestCreak;
    [Tooltip("Dawn into morning.")]
    public Scatter dawnBirds;
    [Tooltip("Night only.")]
    public Scatter owl;
    [Tooltip("Night only (far off, non-positional).")]
    public Scatter howl;

    [Header("Time of day")]
    public SoundEvent duskSting;
    [Tooltip("The Night phase (22:00): the skeleton hours.")]
    public SoundEvent nightSting;
    public SoundEvent dawnSting;
}

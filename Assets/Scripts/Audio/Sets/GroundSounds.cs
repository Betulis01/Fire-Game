using UnityEngine;

// The ground's own sounds, per GroundType (sound.md §3.1, §9.4): the step every walker
// plays for the surface under its feet, and the soft layer under anything landing on it.
// One asset, on AudioConfig. A ground with nothing set falls back to Snow's.
[CreateAssetMenu(menuName = "Audio/Ground Sounds", fileName = "GroundSounds")]
public class GroundSounds : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public GroundType ground;
        public SoundEvent step;
        public SoundEvent land;
    }

    public Entry[] grounds = new Entry[0];

    public SoundEvent Step(GroundType ground) => Find(ground, land: false);
    public SoundEvent Land(GroundType ground) => Find(ground, land: true);

    SoundEvent Find(GroundType ground, bool land)
    {
        SoundEvent snow = null;
        foreach (Entry e in grounds)
        {
            SoundEvent s = land ? e.land : e.step;
            if (e.ground == ground && s != null) return s;
            if (e.ground == GroundType.Snow) snow = s;
        }
        return snow;
    }
}

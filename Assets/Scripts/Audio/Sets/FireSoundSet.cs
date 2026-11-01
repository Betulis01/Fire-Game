using UnityEngine;

// A fire's sound (sound.md §8.1, §8.2, §4.3), played by FireAudio from its Fuel:
//   layers   loops crossfaded by fuel level (equal power between neighbouring centres),
//            so the fire swells and dies smoothly instead of switching stage by stage;
//            all play in sync from one playhead (cut them from one recording, same length)
//   feed     the catch when fuel goes in, by what it was (a stick, a log, sap)
//   stageUp  a breathy swell when the fuel passes into a higher stage
//   low      a sputter crossing `lowAt` on the way down (once, until it's back above)
//   out      the fire dying at zero; relight when fuel brings it back
//   pops     ember pops at random, more often the bigger the fire
[CreateAssetMenu(menuName = "Audio/Fire Sound Set", fileName = "Fire_")]
public class FireSoundSet : ScriptableObject
{
    [System.Serializable]
    public struct Layer
    {
        public SoundEvent loop;
        [Tooltip("The fuel level (0..1) where this layer is at full volume.")]
        [Range(0f, 1f)] public float centre;
    }

    [System.Serializable]
    public struct Feed
    {
        public FuelType fuel;
        public SoundEvent sound;
    }

    [Tooltip("Ordered by centre, lowest first. One layer = a single loop at any fuel (a torch).")]
    public Layer[] layers = new Layer[0];
    public Feed[] feeds = new Feed[0];
    [Tooltip("Fuel added that isn't in `feeds` (or with no item).")]
    public SoundEvent feedDefault;

    [Tooltip("Fuel stages for stageUp (FireRender's stageCount: the campfire's 4).")]
    [Min(1)] public int stageCount = 4;
    public SoundEvent stageUp;

    public SoundEvent low;
    [Range(0f, 1f)] public float lowAt = 0.15f;
    public SoundEvent outSound;
    public SoundEvent relight;

    public SoundEvent pop;
    [Tooltip("Seconds between pops: x at a nearly empty fire, y at a full one.")]
    public Vector2 popInterval = new(6f, 1f);

    [Tooltip("A carried fire (a torch): louder while it's swung or run with. 0 = off.")]
    [Range(0f, 1f)] public float motionBoost;

    public SoundEvent FeedFor(FuelType? fuel)
    {
        if (fuel != null)
            foreach (Feed f in feeds)
                if (f.fuel == fuel.Value && f.sound != null) return f.sound;
        return feedDefault;
    }
}

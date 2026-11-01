using UnityEngine;

// How one kind of walker's feet sound (SoundSystemPlan §4): every step plays the shared
// ground step for the surface under it (AudioConfig.groundSounds) at groundVolume, and a
// creature adds its own foot layer (bone clack, paw, hoof) on top. The player's boots
// are the ground step itself, with a load creak while carrying.
[CreateAssetMenu(menuName = "Audio/Footstep Profile", fileName = "Footsteps_")]
public class FootstepProfile : ScriptableObject
{
    [Tooltip("The creature's own layer on each step. Empty for the player.")]
    public SoundEvent foot;
    [Tooltip("Level of the shared ground step under the foot layer (a light wolf leaves less of a crunch).")]
    [Range(0f, 1f)] public float groundVolume = 1f;
    [Tooltip("Layered on each step while the hands carry something (the player).")]
    public SoundEvent carryLayer;
    [Tooltip("World units per step when there are no footstep events to go by (a clip without them, played backwards).")]
    [Min(0.05f)] public float strideLength = 0.5f;
    [Tooltip("A loop while sliding on ice (the player). Empty = none.")]
    public SoundEvent iceSlide;
}

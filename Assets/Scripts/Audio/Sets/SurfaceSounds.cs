using UnityEngine;

// What a material sounds like when struck (sound.md §13): flesh, bone, wood, stone, ...
// One asset per material, shared by everything made of it; SurfaceAudio on the victim
// picks it, so a hit sounds like what was struck whatever the weapon.
[CreateAssetMenu(menuName = "Audio/Surface Sounds", fileName = "Surface_")]
public class SurfaceSounds : ScriptableObject
{
    [Tooltip("A hit that deals damage.")]
    public SoundEvent impact;
    [Tooltip("A hit the striking tool can't damage (ToolDamageFilter): an axe on stone. Empty = only the weapon's own glance plays.")]
    public SoundEvent glance;
}

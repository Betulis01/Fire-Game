using UnityEngine;

// A tool or weapon's own sounds (sound.md §4): the swing (the impact is the victim's,
// SurfaceAudio), its charge, a glance off something it can't damage, and for the bow
// and the shield their own moments. Empty fields fall back to WeaponAudio's defaults
// (the shared charge-ready ting, glance thud, draw and stow).
[CreateAssetMenu(menuName = "Audio/Tool Sounds", fileName = "Tool_")]
public class ToolSounds : ScriptableObject
{
    [Header("Swing")]
    public SoundEvent swing;
    [Tooltip("Empty = the light swing.")]
    public SoundEvent swingHeavy;
    [Tooltip("A loop rising while the attack is held (a bow's draw creak).")]
    public SoundEvent chargeLoop;
    public SoundEvent chargeReady;
    public SoundEvent glance;

    [Header("Bow")]
    public SoundEvent fire;

    [Header("Shield")]
    public SoundEvent raise;
    public SoundEvent lower;
    public SoundEvent block;

    [Header("Handling")]
    public SoundEvent draw;
    public SoundEvent stow;
}

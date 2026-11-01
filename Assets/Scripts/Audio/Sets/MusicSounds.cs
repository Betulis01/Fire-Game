using UnityEngine;

// The music (sound.md §11.3), played by MusicDirector: one base loop per state --
// the main menu, day, night -- crossfaded as the state changes; layers on top while
// their condition holds (combat, freezing, the fire burning low); and the end stings
// (game over; won, which carries on as a loop). On AudioConfig.
[CreateAssetMenu(menuName = "Audio/Music Sounds", fileName = "MusicSounds")]
public class MusicSounds : ScriptableObject
{
    [Header("Bases")]
    public SoundEvent mainMenu;
    public SoundEvent day;
    public SoundEvent night;

    [Header("Layers")]
    public SoundEvent combat;
    public SoundEvent freezing;
    public SoundEvent lowFire;
    [Tooltip("The nearest campfire within this range counts for the low-fire layer.")]
    [Min(1f)] public float lowFireRange = 15f;
    [Range(0f, 1f)] public float lowFireAt = 0.15f;

    [Header("Ends")]
    public SoundEvent gameOver;
    public SoundEvent gameWon;

    [Header("Fades")]
    [Min(0f)] public float baseCrossfade = 3f;
    [Min(0f)] public float layerFadeIn = 2f;
    [Min(0f)] public float layerFadeOut = 4f;
}

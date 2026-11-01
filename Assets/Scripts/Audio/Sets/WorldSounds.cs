using UnityEngine;

// The world-wide sounds the audio directors play (ItemAudioDirector, BuildAudio), on
// AudioConfig: the layers under item handling, and building. Item-specific sounds
// are the items' own (ItemSounds); these sit on top or stand in.
[CreateAssetMenu(menuName = "Audio/World Sounds", fileName = "WorldSounds")]
public class WorldSounds : ScriptableObject
{
    [Header("Items")]
    [Tooltip("Under a deliberate pickup into the bag (E).")]
    public SoundEvent pocket;
    [Tooltip("Under a pickup the bag tops up by itself (walking over a stack).")]
    public SoundEvent autoPickup;
    [Tooltip("Under lifting a Big item (a log, the big stone) into the hands.")]
    public SoundEvent liftHeavy;
    public SoundEvent thrown;
    [Tooltip("A bag stack tipped out into the world.")]
    public SoundEvent spill;
    [Tooltip("Wearing a new bag (the contents carry over).")]
    public SoundEvent bagSwap;
    [Tooltip("Loot knocked loose (a tree's logs, an ore's stone), before it lands.")]
    public SoundEvent lootPop;
    [Tooltip("An item with no ItemSounds of its own.")]
    public ItemSounds fallback;

    [Header("Building")]
    public SoundEvent ghostBegin;
    public SoundEvent ghostPlace;
    public SoundEvent ghostCancel;
    [Tooltip("One ingredient of a build site fully supplied.")]
    public SoundEvent ingredientDone;
    [Tooltip("Any structure finished (each also plays its own, StructureAudio).")]
    public SoundEvent buildComplete;
}

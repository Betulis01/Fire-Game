using UnityEngine;

// What an item sounds like being handled (sound.md §7): picked up, landing in the world,
// worn and taken off (gear), eaten (food). One asset per material (logs, sticks, small
// stones, leather, ...), shared by every item made of it; an ItemDefinition points at
// its set. Played by ItemAudioDirector from ItemEvents. Empty fields stay silent.
[CreateAssetMenu(menuName = "Audio/Item Sounds", fileName = "Item_")]
public class ItemSounds : ScriptableObject
{
    public SoundEvent pickup;
    public SoundEvent land;
    public SoundEvent equip;
    public SoundEvent unequip;
    [Tooltip("Eating it (food).")]
    public SoundEvent use;
}

using System;
using UnityEngine;

// Draws worn equipment over the body. Creates one SpriteLayerFollower renderer per
// EquipSlot as a child of the player (at the body's origin -- every layer shares
// the Aseprite canvas pivot) and hands it the worn item's frames whenever
// Equipment changes. An empty slot draws nothing.
[RequireComponent(typeof(Equipment), typeof(SpriteRenderer))]
public class PaperDoll : MonoBehaviour
{
    [Tooltip("Sorting order above the body per slot, in EquipSlot order " +
             "(Head, Shoulders, Chest, Hands, Legs). The body's Head layer is +1, " +
             "held items in front are +6 (HeldItemSorter).")]
    [SerializeField] int[] slotOrder = { 5, 4, 3, 4, 2 };

    Equipment equipment;
    SpriteLayerFollower[] slots;

    void Awake()
    {
        equipment = GetComponent<Equipment>();

        EquipSlot[] all = (EquipSlot[])Enum.GetValues(typeof(EquipSlot));
        slots = new SpriteLayerFollower[all.Length];
        foreach (EquipSlot slot in all)
        {
            var go = new GameObject($"Gear_{slot}", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            SpriteLayerFollower f = go.AddComponent<SpriteLayerFollower>();   // source = parent body
            f.SetOrderOffset((int)slot < slotOrder.Length ? slotOrder[(int)slot] : 1);
            slots[(int)slot] = f;
        }
    }

    void OnEnable() { equipment.Changed += Refresh; Refresh(); }
    void OnDisable() => equipment.Changed -= Refresh;

    void Refresh()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            Equippable worn = equipment.WornEquippable((EquipSlot)i);
            slots[i].SetFrames(worn != null ? worn.frames : null);
        }
    }
}

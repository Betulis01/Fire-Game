using UnityEngine;

// Player tab: one EquipSlotUI per worn slot. Click an empty box to wear the
// fitting item from your hands (selected hand first); click a filled box to take
// it off into a free hand (dropped at your feet if both are full).
public class EquipmentTabUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] EquipSlotUI[] slots;

    Equipment equipment;

    void Awake()
    {
        foreach (EquipSlotUI s in slots) s.Clicked += OnClickSlot;
    }

    void OnEnable()
    {
        equipment = root != null ? root.Equipment : null;
        if (equipment != null) equipment.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (equipment != null) equipment.Changed -= Refresh;
    }

    void Refresh()
    {
        foreach (EquipSlotUI s in slots)
            s.Set(equipment != null ? equipment.Worn(s.Slot) : null);
    }

    void OnClickSlot(EquipSlot slot)
    {
        if (equipment == null) return;
        HandSide active = root.Interactor != null ? root.Interactor.ActiveHand : HandSide.Left;

        if (equipment.Worn(slot) != null) equipment.Unequip(slot, active);
        else equipment.TryEquipFromHands(slot, active);
    }
}

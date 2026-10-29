using UnityEngine;

// Player tab's gear boxes: one EquipSlotUI per worn slot and one LoadoutSlotUI per
// loadout slot (sets A and B, Main Hand and Off Hand). Gear goes on from the bag
// below (click or drag a bag slot); clicking a filled box (left or right) takes the
// item off into the bag, or does nothing when the bag has no room. Loadout items also drag between the
// loadout boxes and back into the bag (LoadoutSlotUI). The worn bag can't be taken off,
// only swapped by wearing another bag.
public class EquipmentTabUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] EquipSlotUI[] slots;
    [SerializeField] LoadoutSlotUI[] loadoutSlots;

    Equipment equipment;
    Loadout loadout;
    Inventory inventory;

    void Awake()
    {
        foreach (EquipSlotUI s in slots) s.Clicked += OnClickSlot;
        foreach (LoadoutSlotUI s in loadoutSlots) s.Clicked += OnClickLoadout;
    }

    void OnEnable()
    {
        equipment = root != null ? root.Equipment : null;
        loadout = root != null ? root.Loadout : null;
        inventory = root != null ? root.Inventory : null;
        if (equipment != null) equipment.Changed += Refresh;
        if (loadout != null) loadout.Changed += Refresh;
        foreach (LoadoutSlotUI s in loadoutSlots) s.Bind(loadout);
        Refresh();
    }

    void OnDisable()
    {
        if (equipment != null) equipment.Changed -= Refresh;
        if (loadout != null) loadout.Changed -= Refresh;
    }

    void Refresh()
    {
        foreach (EquipSlotUI s in slots)
            s.Set(equipment != null ? equipment.Worn(s.Slot) : null);
        foreach (LoadoutSlotUI s in loadoutSlots)
            s.Show(loadout != null ? loadout.Item(s.Set, s.Side) : null, loadout == null || loadout.ActiveSet == s.Set);
    }

    void OnClickSlot(EquipSlot slot)
    {
        if (equipment != null) equipment.Unequip(slot);
    }

    void OnClickLoadout(int set, HandSide side)
    {
        if (inventory != null) inventory.UnequipLoadout(set, side);
    }
}

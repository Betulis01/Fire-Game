using System;
using UnityEngine;

// What the player is wearing, one item per EquipSlot. Gear is worn out of the bag
// (Inventory.UseSlot / EquipToBody) or straight off the ground with a Hold E
// (Inventory.TryEquipFromWorld), and taking it off puts it back into the bag -- never
// onto the ground; with no room in the bag it stays on. A bag is never taken off,
// only swapped by wearing another one (the contents carry over). The player starts
// wearing startingGear (the Pouch).
// A worn item is kept as an inactive child of the player, so it's the same object
// the whole way -- world, bag, worn, and back. PaperDoll draws what's worn. As an
// IStatSource the worn items' stats count toward the wearer's CharacterStats.
public class Equipment : MonoBehaviour, IStatSource
{
    [Tooltip("Gear prefabs spawned and worn at the start (the 2-slot Pouch).")]
    [SerializeField] GameObject[] startingGear;

    readonly GameObject[] worn = new GameObject[Enum.GetValues(typeof(EquipSlot)).Length];
    Inventory inventory;

    // raised after any equip/unequip so the UI and PaperDoll can redraw
    public event Action Changed;
    public event Action StatsChanged;

    void Awake() => inventory = GetComponent<Inventory>();

    void Start()
    {
        if (startingGear == null) return;
        foreach (GameObject prefab in startingGear)
        {
            if (prefab == null || !prefab.TryGetComponent(out Equippable e) || Worn(e.slot) != null) continue;
            Wear(Instantiate(prefab));
        }
    }

    public GameObject Worn(EquipSlot slot) => worn[(int)slot];
    public Equippable WornEquippable(EquipSlot slot) => Worn(slot) != null ? Worn(slot).GetComponent<Equippable>() : null;

    // Whether `item` is gear whose slot is free (E focus prefers such gear).
    public bool CanWearInEmptySlot(GameObject item) =>
        item != null && item.TryGetComponent(out Equippable e) && Worn(e.slot) == null;

    // Wear an Equippable in its slot. Returns what was worn there before (or null); the
    // caller decides where that goes. A bag swap resizes the Inventory on Changed.
    public GameObject Wear(GameObject item)
    {
        if (item == null || !item.TryGetComponent(out Equippable e)) return null;

        GameObject previous = worn[(int)e.slot];
        worn[(int)e.slot] = item;
        item.transform.SetParent(transform, false);
        item.GetComponent<WorldItem>().SetHeld(true);
        item.SetActive(false);   // stays "held" (pickup collider off) while worn

        RaiseChanged();
        return previous;
    }

    // Take the item off into the bag. False with no room there, and always for the
    // bag itself (swap it for another instead).
    public bool Unequip(EquipSlot slot)
    {
        GameObject item = worn[(int)slot];
        if (item == null || slot == EquipSlot.Bag || inventory == null) return false;
        if (!inventory.HasEmptySlot) return false;

        worn[(int)slot] = null;
        inventory.TryAdd(item.GetComponent<WorldItem>());
        RaiseChanged();
        return true;
    }

    public void AddStats(ref StatBlock total)
    {
        foreach (GameObject item in worn)
            if (item != null && item.TryGetComponent(out Equippable e)) total += e.stats;
    }

    void RaiseChanged()
    {
        Changed?.Invoke();
        StatsChanged?.Invoke();
    }
}

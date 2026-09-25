using System;
using UnityEngine;

// What the player is wearing, one item per EquipSlot. Items move between the hands
// and the slots: equipping takes an Equippable out of a hand, unequipping puts it
// back into a free hand (or drops it at the player's feet when both are full).
// A worn item is kept as an inactive child of the player, so it's the same object
// the whole way -- world, hand, worn, and back. PaperDoll draws what's worn.
[RequireComponent(typeof(Hands))]
public class Equipment : MonoBehaviour
{
    readonly GameObject[] worn = new GameObject[Enum.GetValues(typeof(EquipSlot)).Length];
    Hands hands;

    // raised after any equip/unequip so the UI and PaperDoll can redraw
    public event Action Changed;

    void Awake() => hands = GetComponent<Hands>();

    public GameObject Worn(EquipSlot slot) => worn[(int)slot];
    public Equippable WornEquippable(EquipSlot slot) => Worn(slot) != null ? Worn(slot).GetComponent<Equippable>() : null;

    // The hand holding something that fits `slot`, checking `preferred` first.
    public bool TryFindInHands(EquipSlot slot, HandSide preferred, out HandSide side)
    {
        foreach (HandSide s in new[] { preferred, Other(preferred) })
        {
            GameObject held = hands.Held(s);
            if (held != null && held.TryGetComponent(out Equippable e) && e.slot == slot)
            {
                side = s;
                return true;
            }
        }
        side = preferred;
        return false;
    }

    // Wear the fitting item from the hands (preferred hand first). Anything already
    // worn in that slot goes back into the hand the new item came from -- a swap.
    public bool TryEquipFromHands(EquipSlot slot, HandSide preferred)
    {
        if (!TryFindInHands(slot, preferred, out HandSide side)) return false;

        GameObject item = hands.Release(side);
        GameObject previous = worn[(int)slot];

        worn[(int)slot] = item;
        item.transform.SetParent(transform, false);
        item.SetActive(false);   // stays "held" (pickup collider off) while worn

        if (previous != null) ReturnToHands(previous, side);

        Changed?.Invoke();
        return true;
    }

    // Take the item off: into `preferred` if empty, else the other hand, else drop.
    public bool Unequip(EquipSlot slot, HandSide preferred)
    {
        GameObject item = worn[(int)slot];
        if (item == null) return false;

        worn[(int)slot] = null;
        ReturnToHands(item, preferred);
        Changed?.Invoke();
        return true;
    }

    void ReturnToHands(GameObject item, HandSide preferred)
    {
        item.SetActive(true);
        foreach (HandSide s in new[] { preferred, Other(preferred) })
            if (!hands.IsHolding(s) && hands.TryHold(item, s)) return;

        // Both hands full: drop it where the player stands.
        item.transform.SetParent(null);
        item.transform.position = transform.position;
        item.GetComponent<WorldItem>().SetHeld(false);
    }

    static HandSide Other(HandSide s) => s == HandSide.Left ? HandSide.Right : HandSide.Left;
}

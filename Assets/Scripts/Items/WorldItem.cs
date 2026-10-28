using System.Collections.Generic;
using UnityEngine;

// A physical item in the world that can be held in a hand. It's the same object
// whether lying on the ground or attached to the player's body — only its parent
// and whether its pickup collider is active change. Behaviours (pickup, fuel, ...)
// are separate capability components (Pickupable, Burnable).
// Pickup collider lives on a child (see Awake's GetComponentInChildren), so no
// root collider is required.
//
// Putting an item back into the world (SetHeld(false): every drop, swap and spill)
// marks it Dropped. The bag's auto-pickup (Inventory) leaves a dropped item alone
// until the player has stepped clear of it once, so it isn't snatched straight back.
// Fresh spawns (tree and ore drops, smelted output, loot) are never marked.
public class WorldItem : MonoBehaviour
{
    public ItemDefinition item;

    // every item currently marked Dropped, so Inventory can clear them cheaply
    public static readonly List<WorldItem> Dropped = new();

    Collider2D pickupCollider;
    YSort ysort;
    bool held;

    public bool IsHeld => held;
    public bool IsDropped { get; private set; }

    void Awake()
    {
        pickupCollider = GetComponentInChildren<Collider2D>();
        ysort = GetComponent<YSort>();
    }

    void OnDestroy() => ClearDropped();

    public void SetHeld(bool value)
    {
        held = value;
        pickupCollider.enabled = !value;   // off while held, on while in the world
        // While held, the player's HeldItemSorter owns the item's sortingOrder;
        // re-enable its own world Y-sort once it's dropped.
        if (ysort != null) ysort.enabled = !value;

        if (value) ClearDropped();
        else if (!IsDropped)
        {
            IsDropped = true;
            Dropped.Add(this);
        }
    }

    public void ClearDropped()
    {
        if (!IsDropped) return;
        IsDropped = false;
        Dropped.Remove(this);
    }
}

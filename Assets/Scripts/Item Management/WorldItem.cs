using UnityEngine;

// A physical item in the world that can be held in a hand. It's the same object
// whether lying on the ground or attached to the player's body — only its parent
// and whether its pickup collider is active change. Behaviours (pickup, fuel, ...)
// are separate capability components (Pickupable, Burnable).
[RequireComponent(typeof(Collider2D))]
public class WorldItem : MonoBehaviour
{
    public ItemDefinition item;

    Collider2D pickupCollider;
    YSort ysort;
    bool held;

    public bool IsHeld => held;

    void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        ysort = GetComponent<YSort>();
    }

    public void SetHeld(bool value)
    {
        held = value;
        pickupCollider.enabled = !value;   // off while held, on while in the world
        // While held, the player's HeldItemSorter owns the item's sortingOrder;
        // re-enable its own world Y-sort once it's dropped.
        if (ysort != null) ysort.enabled = !value;
    }
}

using UnityEngine;

// Sits on the campfire with a trigger collider over its feeding area. Any dropped
// item with a Burnable component is consumed into Fuel, by that item's fuelPerItem.
// Trigger callbacks need a Rigidbody2D on the pair: put a Kinematic Rigidbody2D
// on this campfire object (not on items, so held items aren't driven by physics).
[RequireComponent(typeof(Fuel))]
public class FuelReceiver : MonoBehaviour
{
    public FuelType[] acceptedFuelTypes;   // empty = accept all

    Fuel fuel;

    void Awake()
    {
        fuel = GetComponent<Fuel>();
    }

    // Stay (not Enter) so it also catches an item whose collider re-enables on
    // drop while already overlapping the fire.
    void OnTriggerStay2D(Collider2D other)
    {
        // colliders live on a child of the item, so resolve the item from the parent.
        // !enabled marks an item we've already consumed this frame (items can have
        // several child colliders, so this callback can fire more than once for one item).
        WorldItem worldItem = other.GetComponentInParent<WorldItem>();
        if (worldItem == null || !worldItem.enabled) return;

        // a held item isn't dropped fuel, even if one of its colliders is still live
        if (worldItem.IsHeld) return;

        // an item is fuel if its prefab has a Burnable component
        Burnable burnable = worldItem.GetComponent<Burnable>();
        if (burnable == null) return;

        if (acceptedFuelTypes.Length > 0 &&
            System.Array.IndexOf(acceptedFuelTypes, burnable.fuelType) < 0) return;

        worldItem.enabled = false;   // ignore the rest of this item's colliders this frame
        fuel.Add(burnable.fuelPerItem, burnable.burnRate);
        Destroy(worldItem.gameObject);

        Debug.Log($"Fire consumed 1 {worldItem.item.displayName} (+{burnable.fuelPerItem} fuel @ {burnable.burnRate}/s)");
    }
}

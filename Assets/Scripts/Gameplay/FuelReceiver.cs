using UnityEngine;

// Sits on the campfire with a trigger collider over its feeding area. Any dropped
// item with a Burnable component is consumed into Fuel, by that item's fuelPerItem.
// Trigger callbacks need a Rigidbody2D on the pair: put a Kinematic Rigidbody2D
// on this campfire object (not on items, so held items aren't driven by physics).
[RequireComponent(typeof(Fuel))]
public class FuelReceiver : MonoBehaviour
{
    Fuel fuel;

    void Awake()
    {
        fuel = GetComponent<Fuel>();
    }

    // Stay (not Enter) so it also catches an item whose collider re-enables on
    // drop while already overlapping the fire.
    void OnTriggerStay2D(Collider2D other)
    {
        WorldItem worldItem = other.GetComponent<WorldItem>();
        if (worldItem == null) return;

        // an item is fuel if its prefab has a Burnable component
        Burnable burnable = other.GetComponent<Burnable>();
        if (burnable == null) return;

        // held items have their collider disabled, so anything we see here is dropped
        Destroy(worldItem.gameObject);
        fuel.Add(burnable.fuelPerItem, burnable.burnRate);

        Debug.Log($"Fire consumed 1 {worldItem.item.displayName} (+{burnable.fuelPerItem} fuel @ {burnable.burnRate}/s)");
    }
}

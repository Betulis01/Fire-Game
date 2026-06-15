using UnityEngine;

// Sits on the campfire with a trigger collider over its feeding area. When the
// player drops an accepted item onto the fire, it's consumed into Fuel.
// Trigger callbacks need a Rigidbody2D on the pair: put a Kinematic Rigidbody2D
// on this campfire object (not on items, so held items aren't driven by physics).
[RequireComponent(typeof(Fuel))]
public class FuelReceiver : MonoBehaviour
{
    public ItemDefinition acceptedFuel;     // what the fire eats (e.g. Wood)
    public float fuelPerItem = 10f;

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
        if (worldItem == null || worldItem.item != acceptedFuel) return;

        // held items have their collider disabled, so anything we see here is dropped
        Destroy(worldItem.gameObject);
        fuel.Add(fuelPerItem);

        Debug.Log($"Fire consumed 1 {acceptedFuel.displayName}");
    }
}

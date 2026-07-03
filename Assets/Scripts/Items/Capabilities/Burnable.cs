using UnityEngine;

// Marks an item prefab as fuel and how much Fuel it yields when burned.
// A FuelReceiver consumes dropped items that have this component.
public class Burnable : MonoBehaviour
{
    public FuelType fuelType = FuelType.Wood;
    public float fuelPerItem = 10f;   // total fuel this item yields
    public float burnRate = 1f;       // how fast it burns while it's the active fuel
}

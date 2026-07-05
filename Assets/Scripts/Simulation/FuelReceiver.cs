using UnityEngine;

// Sits on the campfire and owns which items may be burned as fuel. Feeding is
// intentional: the player aims at the fire and presses the feed key (routed by
// PlayerInteractor), which calls Feed here. There is no drop-to-consume physics,
// so a log lying near the fire is never silently eaten.
[RequireComponent(typeof(Fuel))]
public class FuelReceiver : MonoBehaviour
{
    public FuelType[] acceptedFuelTypes;   // empty = accept all

    Fuel fuel;

    void Awake()
    {
        fuel = GetComponent<Fuel>();
    }

    // True if this fire will burn the given item (has a Burnable of an accepted type).
    public bool Accepts(Burnable burnable)
    {
        if (burnable == null) return false;
        return acceptedFuelTypes.Length == 0 ||
               System.Array.IndexOf(acceptedFuelTypes, burnable.fuelType) >= 0;
    }

    // Burn one item's worth of fuel. Caller is responsible for removing the item
    // from the hand/world. Raises Fuel.FuelAdded via Add (drives the light boost).
    public void Feed(Burnable burnable)
    {
        if (!Accepts(burnable)) return;

        fuel.Add(burnable.fuelPerItem, burnable.burnRate);
        Debug.Log($"Fire fed +{burnable.fuelPerItem} fuel @ {burnable.burnRate}/s");
    }
}

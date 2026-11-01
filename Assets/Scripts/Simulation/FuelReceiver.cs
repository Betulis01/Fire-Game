using UnityEngine;

// A fire the player can feed. Accepts held items with a Burnable of an allowed
// fuel type and turns them into Fuel. Deposits are intentional (routed by
// PlayerInteractor via ItemReceiver); there is no drop-to-consume physics.
// A torch has one too, fed by dragging fuel from the bag onto it (Inventory.TryFeed).
[RequireComponent(typeof(Fuel))]
public class FuelReceiver : ItemReceiver
{
    public FuelType[] acceptedFuelTypes;   // empty = accept all

    Fuel fuel;

    // fetched on first use: a torch crafted straight into the bag was never active,
    // so its Awake hasn't run
    Fuel Fuel => fuel != null ? fuel : fuel = GetComponent<Fuel>();

    public override bool Accepts(WorldItem item)
    {
        if (item == null) return false;
        Burnable burnable = item.GetComponent<Burnable>();
        if (burnable == null) return false;
        return acceptedFuelTypes.Length == 0 ||
               System.Array.IndexOf(acceptedFuelTypes, burnable.fuelType) >= 0;
    }

    public override bool Deposit(WorldItem item)
    {
        Burnable burnable = item != null ? item.GetComponent<Burnable>() : null;
        if (!Accepts(item)) return false;

        Fuel.Add(burnable.fuelPerItem, burnable.burnRate, item.item);
        return true;
    }

    protected override string PromptLabel(WorldItem held) => $"Feed {held.item.displayName}";
}

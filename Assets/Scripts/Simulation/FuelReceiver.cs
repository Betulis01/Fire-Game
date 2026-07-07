using UnityEngine;

// A fire the player can feed. Accepts held items with a Burnable of an allowed
// fuel type and turns them into Fuel. Deposits are intentional (routed by
// PlayerInteractor via ItemReceiver); there is no drop-to-consume physics.
[RequireComponent(typeof(Fuel))]
public class FuelReceiver : ItemReceiver
{
    public FuelType[] acceptedFuelTypes;   // empty = accept all

    Fuel fuel;

    protected override void Awake()
    {
        base.Awake();
        fuel = GetComponent<Fuel>();
    }

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

        fuel.Add(burnable.fuelPerItem, burnable.burnRate);
        return true;
    }

    protected override string PromptLabel(WorldItem held) => $"Feed {held.item.displayName}";
}

using UnityEngine;

// Player-side crafting: press F near a crafting station to convert a held item
// into its recipe output, in the same hand. Converts one item per press,
// preferring the left hand.
[RequireComponent(typeof(Hands))]
public class Crafter : MonoBehaviour
{
    public float reach = 1.5f;   // how close to a station you must be

    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            TryCraft();
    }

    void TryCraft()
    {
        CraftingStation station = FindStation();
        if (station == null) return;

        // left hand first, then right; convert the first match and stop
        if (TryConvert(station, HandSide.Left)) return;
        TryConvert(station, HandSide.Right);
    }

    bool TryConvert(CraftingStation station, HandSide side)
    {
        GameObject held = hands.Held(side);
        if (held == null) return false;

        WorldItem worldItem = held.GetComponent<WorldItem>();
        if (worldItem == null) return false;

        ItemDefinition output = station.OutputFor(worldItem.item);
        if (output == null || output.prefab == null) return false;

        GameObject crafted = Instantiate(output.prefab);
        hands.Replace(side, crafted);
        return true;
    }

    CraftingStation FindStation()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, reach);
        foreach (Collider2D hit in hits)
        {
            CraftingStation station = hit.GetComponentInParent<CraftingStation>();
            if (station != null) return station;
        }
        return null;
    }
}

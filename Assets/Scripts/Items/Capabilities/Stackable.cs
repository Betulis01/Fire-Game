using UnityEngine;

// Capability: this item may stack in a bag slot, up to maxStack. Stacking exists only
// in the bag (Inventory owns the count); world items are always single, and the
// hands carry one Big item each.
[RequireComponent(typeof(WorldItem))]
public class Stackable : MonoBehaviour
{
    public int maxStack = 10;
}

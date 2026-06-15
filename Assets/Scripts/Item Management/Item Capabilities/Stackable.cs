using UnityEngine;

// Capability: this item may stack in a hand, up to maxStack. Stacking exists only
// in-hand (Hands owns the count); world items are always single.
[RequireComponent(typeof(WorldItem))]
public class Stackable : MonoBehaviour
{
    public int maxStack = 10;
}

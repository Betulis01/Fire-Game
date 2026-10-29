using UnityEngine;

// Capability: this Equippable is a bag. Worn in the Bag slot, it gives the player's
// Inventory `capacity` slots for Small items and gear. A bag is only ever swapped for
// another (the contents carry over; a smaller one spills what no longer fits).
[RequireComponent(typeof(Equippable))]
public class Bag : MonoBehaviour
{
    [Min(1)] public int capacity = 6;
}

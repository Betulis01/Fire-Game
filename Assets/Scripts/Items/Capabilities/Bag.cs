using UnityEngine;

// Capability: this Equippable is a bag. Worn in the Bag slot, it gives the player's
// Inventory `capacity` slots, which pick up items walked over. Taking it off spills
// what it held (Inventory).
[RequireComponent(typeof(Equippable))]
public class Bag : MonoBehaviour
{
    [Min(1)] public int capacity = 6;
}

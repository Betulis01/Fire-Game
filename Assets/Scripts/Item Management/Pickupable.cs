using UnityEngine;

// Capability: this world item can be picked up into a hand. PlayerInteractor
// finds it as an Interactable and routes the Q/E press here.
[RequireComponent(typeof(WorldItem))]
public class Pickupable : Interactable
{
    WorldItem worldItem;

    void Awake()
    {
        worldItem = GetComponent<WorldItem>();
    }

    // can't pick up something that's already held
    public override bool CanInteract(PlayerController player) => !worldItem.IsHeld;

    // pick up into the hand the player used (Q = left, E = right)
    public override void Interact(PlayerController player, HandSide hand)
    {
        player.GetComponent<Hands>().TryHold(worldItem.gameObject, hand);
    }
}

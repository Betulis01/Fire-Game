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

    // pick up into the hand the player used (Q = left, E = right); true if the
    // item was taken or merged into a stack
    public override bool Interact(PlayerController player, HandSide hand)
    {
        return player.GetComponent<Hands>().TryHold(worldItem.gameObject, hand);
    }

    // Both hands were full: drop the used hand's contents where this item lies and
    // take this item into that hand -- a literal switch of places.
    public override bool Swap(PlayerController player, HandSide hand)
    {
        return player.GetComponent<Hands>()
            .SwapHold(worldItem.gameObject, hand, transform.position);
    }
}

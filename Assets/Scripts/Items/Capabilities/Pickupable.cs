using UnityEngine;

// Capability: this world item can be picked up. PlayerInteractor finds it as an
// Interactable and routes the E press here; the player's Inventory decides where it
// goes (a Big item into a free hand, the rest into the bag). Wielding or wearing gear
// takes a Hold E (PlayerInteractor).
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

    // true if the item was taken (or merged into a stack); with no room it stays put
    public override bool Interact(PlayerController player)
    {
        return player.TryGetComponent(out Inventory inventory) && inventory.TryTake(worldItem);
    }
}

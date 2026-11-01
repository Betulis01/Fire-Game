using UnityEngine;

// Capability: this world item can be picked up. PlayerInteractor finds it as an
// Interactable and routes the E press here; the player's Inventory decides where it
// goes (a Big item into a free hand, the rest into the bag). Wielding or wearing gear
// takes a Hold E (PlayerInteractor). With no room for it the player takes it anyway and
// fumbles it: it drops beside them (PlayerInteractor.Fumble).
// An item with Highlight art (a torch) gets its TargetHighlight on waking, so the
// outline starts hidden instead of showing until the player first aims at it.
[RequireComponent(typeof(WorldItem))]
public class Pickupable : Interactable
{
    WorldItem worldItem;

    void Awake()
    {
        worldItem = GetComponent<WorldItem>();
        TargetHighlight.Attach(gameObject);
    }

    // can't pick up something that's already held
    public override bool CanInteract(PlayerController player) => !worldItem.IsHeld;

    // true if the item was taken (or merged into a stack), or fumbled for want of room
    public override bool Interact(PlayerController player)
    {
        ItemDefinition item = worldItem.item;
        Vector3 at = transform.position;   // taking it may destroy it (merged into a stack)
        if (player.TryGetComponent(out Inventory inventory) && inventory.TryTake(worldItem))
        {
            ItemEvents.RaisePickedUp(item, at, auto: false);
            return true;
        }
        return player.TryGetComponent(out PlayerInteractor interactor) && interactor.Fumble(worldItem);
    }
}

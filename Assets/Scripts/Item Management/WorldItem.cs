using UnityEngine;

// A physical item in the world that can be held in a hand. It's the same object
// whether lying on the ground or attached to the player's body — only its parent
// and whether its pickup collider is active change.
[RequireComponent(typeof(Collider2D))]
public class WorldItem : Interactable
{
    public ItemDefinition item;

    Collider2D pickupCollider;
    bool held;

    void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
    }

    // a held item is not pickupable (and the fire ignores it)
    public override bool CanInteract(PlayerController player) => !held;

    // picked up into the hand the player used (Q = left, E = right)
    public override void Interact(PlayerController player, HandSide hand)
    {
        player.GetComponent<Hands>().TryHold(gameObject, hand);
    }

    public void SetHeld(bool value)
    {
        held = value;
        pickupCollider.enabled = !value;   // off while held, on while in the world
    }
}

using UnityEngine;

// A world item the player can pick up with E. The inventory key is the
// GameObject's tag (e.g. tag "Wood" -> stored as "Wood"), so one component
// works for every pickup type. Set itemName to override the tag if needed.
public class InventoryPickup : Interactable
{
    [Tooltip("Inventory key. Leave empty to use this object's tag.")]
    public string itemName = "";

    string ItemName => string.IsNullOrEmpty(itemName) ? tag : itemName;

    public override void Interact(PlayerController player)
    {
        Inventory inventory = player.GetComponent<Inventory>();
        inventory.Add(ItemName, gameObject);

        // deactivate instead of destroy: the Inventory keeps this GameObject as the
        // carried item and destroys it later when the item is consumed.
        gameObject.SetActive(false);

        Debug.Log($"Picked up {ItemName}");
    }
}

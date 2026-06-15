using UnityEngine;

// The campfire: press E to feed it one wood from the player's inventory.
[RequireComponent(typeof(Fuel))]
public class CampfireFeed : Interactable
{
    public float fuelPerWood = 10f;

    Fuel fuel;

    void Awake()
    {
        fuel = GetComponent<Fuel>();
    }

    // only show the prompt / allow the press when the player has wood to give
    public override bool CanInteract(PlayerController player)
    {
        return player.GetComponent<Inventory>().Count("Wood") > 0;
    }

    public override void Interact(PlayerController player)
    {
        Inventory inventory = player.GetComponent<Inventory>();
        inventory.Remove("Wood");
        fuel.Add(fuelPerWood);

        Debug.Log("Fed the fire with 1 wood");
    }
}

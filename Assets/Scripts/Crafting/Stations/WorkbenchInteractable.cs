using UnityEngine;

// E at the workbench opens the F menu on Crafting -> Workbench (or closes it again).
// Being near it is what unlocks the workbench recipes (Station, CraftingController).
public class WorkbenchInteractable : Interactable
{
    // E opening the crafting menu here, or closing it again (for the audio layer).
    public event System.Action Opened;
    public event System.Action Closed;

    public override bool Interact(PlayerController player)
    {
        HudMenu menu = HudMenu.Instance;
        if (menu == null) return false;
        if (menu.IsOpen)
        {
            menu.Close();
            Closed?.Invoke();
        }
        else
        {
            menu.OpenCrafting(atWorkbench: true);
            Opened?.Invoke();
        }
        return true;
    }
}

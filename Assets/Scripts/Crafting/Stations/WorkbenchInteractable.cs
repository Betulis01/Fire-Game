using UnityEngine;

// E at the workbench opens the F menu on Crafting -> Workbench (or closes it again).
// Being near it is what unlocks the workbench recipes (Station, CraftingController).
public class WorkbenchInteractable : Interactable
{
    public override bool Interact(PlayerController player)
    {
        HudMenu menu = HudMenu.Instance;
        if (menu == null) return false;
        if (menu.IsOpen) menu.Close();
        else menu.OpenCrafting(atWorkbench: true);
        return true;
    }
}

using UnityEngine;

// What kind of crafting context a station provides. None means a recipe needs
// no station at all (craftable bare-handed).
[System.Flags]
public enum StationType { None = 0, Campfire = 1, Workbench = 2, Furnace = 4 }

// Marks a world object as a crafting station of a given type (e.g. the campfire
// is a Fire station). CraftingController checks nearby stations against each recipe's
// requiredStation. Recipes themselves live in a central RecipeBook, not here.
public class Station : MonoBehaviour
{
    public StationType type = StationType.Campfire;
    public float reach = 1.5f;     // how close a crafter must be to use this station
}

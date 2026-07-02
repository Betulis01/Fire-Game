using UnityEngine;

// What kind of crafting context a station provides. None means a recipe needs
// no station at all (craftable bare-handed).
public enum StationType { None, Fire, Workbench }

// Marks a world object as a crafting station of a given type (e.g. the campfire
// is a Fire station). CraftingController checks nearby stations against each recipe's
// requiredStation. Recipes themselves live in a central RecipeBook, not here.
public class Station : MonoBehaviour
{
    public StationType type = StationType.Fire;
    public float reach = 1.5f;     // how close a crafter must be to use this station
}

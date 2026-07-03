using UnityEngine;

// Instant recipes hand their output straight into the player's hands (axe, torch).
// Placeable recipes spawn a transparent ghost of the output structure that the
// player positions in the world and then feeds materials into over time (campfire,
// workbench) — see PlacementGhost / BuildRequirement.
public enum RecipeKind { Instant, Placeable }

// One ingredient slot: `amount` of `item`, e.g. 2 Stick.
[System.Serializable]
public class Ingredient
{
    public ItemDefinition item;
    public int amount = 1;
}

// One conversion: gather `ingredients` and, if `requiredStation` is satisfied, get
// `output`. requiredStation = None means it crafts bare-handed/anywhere.
// Ingredients span at most two distinct item types: Instant recipes draw them from
// the player's two hands, Placeable recipes draw them from items dropped on the
// ghost, and there are only two hands' worth of distinct stacks to match against.
[System.Serializable]
public class Recipe
{
    public string displayName;
    public Ingredient[] ingredients;
    public ItemDefinition output;
    public StationType requiredStation = StationType.None;
    public RecipeKind kind = RecipeKind.Instant;
}

// Central list of every recipe in the game. Assigned on the player's
// CraftingController and read by the Journal UI.
[CreateAssetMenu(menuName = "Items/Recipe Book")]
public class RecipeBook : ScriptableObject
{
    public Recipe[] recipes;
}

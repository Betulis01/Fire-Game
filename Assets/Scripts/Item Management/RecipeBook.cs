using UnityEngine;

// One conversion: hold all of `inputs` (one per hand) and, if `requiredStation`
// is satisfied, get `output`. requiredStation = None means it crafts bare-handed.
[System.Serializable]
public class Recipe
{
    public ItemDefinition[] inputs;
    public ItemDefinition output;
    public StationType requiredStation = StationType.None;
}

// Central list of every recipe in the game. Assigned on the player's Crafter.
// Author recipes most-specific-first: the first satisfiable one wins, so a
// {stone, wood} recipe should sit above a {wood} recipe.
[CreateAssetMenu(menuName = "Items/Recipe Book")]
public class RecipeBook : ScriptableObject
{
    public Recipe[] recipes;
}

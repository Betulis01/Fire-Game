using UnityEngine;

// A single conversion: hold the input item here and you get the output.
[System.Serializable]
public class Recipe
{
    public ItemDefinition input;
    public ItemDefinition output;
}

// Put this on a crafting station (e.g. the campfire). It just holds the recipes
// it offers; the player's Crafter looks them up when nearby.
public class CraftingStation : MonoBehaviour
{
    public Recipe[] recipes;

    // the output for a held input, or null if this station can't craft it
    public ItemDefinition OutputFor(ItemDefinition input)
    {
        if (input == null) return null;

        foreach (Recipe r in recipes)
            if (r.input == input) return r.output;

        return null;
    }
}

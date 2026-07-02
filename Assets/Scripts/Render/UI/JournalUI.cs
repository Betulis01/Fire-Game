using System.Collections.Generic;
using UnityEngine;

public class JournalUI : MonoBehaviour
{
    public GameObject panel;
    public RecipeGrid rowPrefab;
    public Transform rowParent;
    public CraftingController crafting;

    bool open;
    readonly List<(RecipeGrid row, Recipe recipe)> rows = new();

    void Update()
    {
        // ignore Tab while a placement ghost is active; it cancels the ghost instead
        if (UserInput.Instance.Journal && !PlacementGhost.AnyActive)
        {
            open = !open;
            panel.SetActive(open);
            if (open) Rebuild();
        }

        if (open) Refresh();
    }

    void Rebuild()
    {
        foreach (Transform child in rowParent)
            Destroy(child.gameObject);
        rows.Clear();

        if (crafting == null || crafting.book == null) return;

        foreach (Recipe recipe in crafting.book.recipes)
        {
            if (recipe == null) continue;
            RecipeGrid row = Instantiate(rowPrefab, rowParent);
            rows.Add((row, recipe));
        }

        Refresh();
    }

    void Refresh()
    {
        if (crafting == null || crafting.book == null) return;

        HashSet<StationType> stations = crafting.NearbyStationTypes();

        foreach ((RecipeGrid row, Recipe recipe) in rows)
        {
            bool craftable = crafting.StationSatisfied(recipe, stations) && crafting.HasIngredients(recipe);
            row.Set(recipe, craftable, () => OnClickRecipe(recipe));
        }
    }

    void OnClickRecipe(Recipe recipe)
    {
        if (!crafting.TryCraft(recipe)) return;

        open = false;
        panel.SetActive(false);
    }
}

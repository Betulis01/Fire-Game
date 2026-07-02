using UnityEngine;

// Tab toggles the Survival Journal: a panel listing every recipe in the player's
// CraftingController.book, greyed out when its station/ingredients aren't met. The
// list is small and static, so it's simplest to just rebuild it from scratch each
// time the panel opens rather than tracking incremental redraws (cf. InventoryBar).
public class JournalUI : MonoBehaviour
{
    public GameObject panel;
    public RecipeGrid rowPrefab;
    public Transform rowParent;
    public CraftingController crafting;

    bool open;

    void Update()
    {
        // ignore Tab while a placement ghost is active; it cancels the ghost instead
        if (!UserInput.Instance.Journal || PlacementGhost.AnyActive) return;

        open = !open;
        panel.SetActive(open);
        if (open) Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform child in rowParent)
            Destroy(child.gameObject);

        if (crafting == null || crafting.book == null) return;

        System.Collections.Generic.HashSet<StationType> stations = crafting.NearbyStationTypes();

        foreach (Recipe recipe in crafting.book.recipes)
        {
            if (recipe == null) continue;

            RecipeGrid row = Instantiate(rowPrefab, rowParent);
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

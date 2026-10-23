using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Crafting tab: sub-tabs "Hands" (recipes that need no workbench) and "Workbench",
// over a grid of blueprint boxes built from the player's RecipeBook. Hovering a box
// shows the recipe's name and ingredients to the right of the grid; clicking it
// crafts through CraftingController (a Placeable recipe closes the menu so the
// placement ghost can be aimed). Craftability is re-checked a few times a second
// while the tab is open, since it depends on what's held and what's nearby.
public class CraftingTabUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] TabButtonUI handsTab;
    [SerializeField] TabButtonUI workbenchTab;
    [SerializeField] RectTransform grid;              // GridLayoutGroup
    [SerializeField] BlueprintCellUI cellTemplate;    // inactive child of the grid

    [Header("Details (hovered blueprint)")]
    [SerializeField] GameObject details;
    [SerializeField] TMP_Text nameText;
    [SerializeField] RectTransform ingredientList;
    [SerializeField] IngredientRowUI rowTemplate;     // inactive child of ingredientList
    [SerializeField] float rowHeight = 20f;

    [SerializeField] float refreshInterval = 0.25f;

    readonly List<BlueprintCellUI> cells = new();
    readonly List<IngredientRowUI> rows = new();
    bool workbench;
    float nextRefresh;

    void Awake()
    {
        handsTab.Clicked += () => SelectLocation(false);
        workbenchTab.Clicked += () => SelectLocation(true);
        cellTemplate.gameObject.SetActive(false);
        rowTemplate.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        SelectLocation(workbench);
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + refreshInterval;
        RefreshAvailability();
    }

    void SelectLocation(bool atWorkbench)
    {
        workbench = atWorkbench;
        handsTab.SetSelected(!workbench);
        workbenchTab.SetSelected(workbench);
        Rebuild();
    }

    void Rebuild()
    {
        foreach (BlueprintCellUI c in cells) Destroy(c.gameObject);
        cells.Clear();
        ShowDetails(null);

        CraftingController crafting = root != null ? root.Crafting : null;
        if (crafting == null || crafting.book == null) return;

        foreach (Recipe recipe in crafting.book.recipes)
        {
            if (recipe == null) continue;
            bool needsWorkbench = (recipe.requiredStation & StationType.Workbench) != 0;
            if (needsWorkbench != workbench) continue;

            BlueprintCellUI cell = Instantiate(cellTemplate, grid);
            cell.gameObject.SetActive(true);
            cell.Set(recipe);
            cell.Clicked += OnClickCell;
            cell.HoverStarted += c => ShowDetails(c.Recipe);
            cell.HoverEnded += _ => ShowDetails(null);
            cells.Add(cell);
        }

        nextRefresh = 0f;
        RefreshAvailability();
    }

    void RefreshAvailability()
    {
        CraftingController crafting = root != null ? root.Crafting : null;
        if (crafting == null) return;

        StationType stations = crafting.NearbyStationTypes();
        foreach (BlueprintCellUI cell in cells)
            cell.SetAvailable(crafting.StationSatisfied(cell.Recipe, stations) && crafting.HasIngredients(cell.Recipe));
    }

    void OnClickCell(BlueprintCellUI cell)
    {
        Recipe recipe = cell.Recipe;
        if (!root.Crafting.TryCraft(recipe)) return;

        if (recipe.kind == RecipeKind.Placeable && HudMenu.Instance != null) HudMenu.Instance.Close();
        else RefreshAvailability();
    }

    void ShowDetails(Recipe recipe)
    {
        details.SetActive(recipe != null);
        if (recipe == null) return;

        nameText.text = recipe.displayName;

        int count = recipe.ingredients != null ? recipe.ingredients.Length : 0;
        while (rows.Count < count)
        {
            IngredientRowUI row = Instantiate(rowTemplate, ingredientList);
            var rt = (RectTransform)row.transform;
            rt.anchoredPosition = new Vector2(0f, -rows.Count * rowHeight);
            rows.Add(row);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < count && recipe.ingredients[i].item != null;
            rows[i].gameObject.SetActive(used);
            if (used) rows[i].Set(recipe.ingredients[i]);
        }
    }
}

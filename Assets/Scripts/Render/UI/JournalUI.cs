using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    enum Tab { Crafting, Character }

    public GameObject panel;
    public RecipeGrid rowPrefab;
    public Transform rowParent;
    public CraftingController crafting;

    [Header("Tabs")]
    public GameObject craftingPanel;
    public GameObject characterPanel;
    public Toggle craftingTabToggle;
    public Toggle characterTabToggle;

    [Header("Craft location")]
    public Toggle anywhereToggle;
    public Toggle workbenchToggle;
    public TMP_Text locationLabel;

    [Header("Blueprint details")]
    public BlueprintPanelUI blueprintPanel;

    bool open;
    Tab tab = Tab.Crafting;
    StationType selectedLocation = StationType.None;
    readonly List<(RecipeGrid row, Recipe recipe)> rows = new();

    void Awake()
    {
        if (craftingTabToggle != null) craftingTabToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectTab(Tab.Crafting); });
        if (characterTabToggle != null) characterTabToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectTab(Tab.Character); });

        if (anywhereToggle != null) anywhereToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectLocation(StationType.None); });
        if (workbenchToggle != null) workbenchToggle.onValueChanged.AddListener(isOn => { if (isOn) SelectLocation(StationType.Workbench); });
    }

    void Update()
    {
        // ignore Tab while a placement ghost is active; it cancels the ghost instead
        if (UserInput.Instance.Journal && !PlacementGhost.AnyActive)
        {
            open = !open;
            panel.SetActive(open);
            if (open)
            {
                SelectTab(Tab.Crafting);
                Rebuild();
            }
        }

        if (open) Refresh();
    }

    void SelectTab(Tab newTab)
    {
        tab = newTab;
        if (craftingPanel != null) craftingPanel.SetActive(tab == Tab.Crafting);
        if (characterPanel != null) characterPanel.SetActive(tab == Tab.Character);

        if (craftingTabToggle != null) craftingTabToggle.SetIsOnWithoutNotify(tab == Tab.Crafting);
        if (characterTabToggle != null) characterTabToggle.SetIsOnWithoutNotify(tab == Tab.Character);
    }

    void SelectLocation(StationType location)
    {
        selectedLocation = location;
        if (locationLabel != null)
            locationLabel.text = location == StationType.None ? "Anywhere" : "Workbench";

        if (anywhereToggle != null) anywhereToggle.SetIsOnWithoutNotify(location == StationType.None);
        if (workbenchToggle != null) workbenchToggle.SetIsOnWithoutNotify(location == StationType.Workbench);

        Rebuild();
    }

    void Rebuild()
    {
        foreach (Transform child in rowParent)
            Destroy(child.gameObject);
        rows.Clear();

        if (blueprintPanel != null) blueprintPanel.Hide();

        if (crafting == null || crafting.book == null) return;

        foreach (Recipe recipe in crafting.book.recipes)
        {
            if (recipe == null || recipe.requiredStation != selectedLocation) continue;
            RecipeGrid row = Instantiate(rowPrefab, rowParent);
            rows.Add((row, recipe));
        }

        Refresh();
    }

    void Refresh()
    {
        if (crafting == null || crafting.book == null) return;

        StationType stations = crafting.NearbyStationTypes();

        foreach ((RecipeGrid row, Recipe recipe) in rows)
        {
            bool craftable = crafting.StationSatisfied(recipe, stations) && crafting.HasIngredients(recipe);
            row.Set(recipe, craftable, () => OnClickRecipe(recipe), ShowBlueprint);
        }
    }

    void ShowBlueprint(Recipe recipe)
    {
        if (blueprintPanel == null) return;
        blueprintPanel.gameObject.SetActive(true);
        blueprintPanel.Show(recipe);
    }

    void OnClickRecipe(Recipe recipe)
    {
        if (!crafting.TryCraft(recipe)) return;

        open = false;
        panel.SetActive(false);
    }
}

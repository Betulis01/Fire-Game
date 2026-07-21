using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows the details of whichever Blueprint the player is hovering in the
// Crafting grid: name, large icon, and one Required Materials cell per
// ingredient. JournalUI controls when this is shown/hidden.
public class BlueprintPanelUI : MonoBehaviour
{
    public TMP_Text nameText;
    public Image icon;
    public Transform materialsParent;
    public MaterialRequirementUI materialPrefab;

    public void Show(Recipe recipe)
    {
        nameText.text = recipe.displayName;
        icon.sprite = recipe.output != null ? recipe.output.ResolveIcon() : null;

        foreach (Transform child in materialsParent)
            Destroy(child.gameObject);

        if (recipe.ingredients == null) return;

        foreach (Ingredient ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;
            MaterialRequirementUI cell = Instantiate(materialPrefab, materialsParent);
            cell.Set(ingredient.item.ResolveIcon(), ingredient.amount);
        }
    }

    public void Hide() => gameObject.SetActive(false);
}

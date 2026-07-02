using UnityEngine;
using UnityEngine.UI;

// One icon cell in the Survival Journal grid. The root GameObject is both the
// Image (icon) and the Button — no child Button needed. Add Image + Button +
// CanvasGroup + this script to the prefab root and wire icon = self, button = self.
// Dims via CanvasGroup.alpha when the recipe isn't currently craftable.
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class RecipeGrid : MonoBehaviour
{
    public Image icon;
    public Button button;

    public void Set(Recipe recipe, bool craftable, System.Action onClick)
    {
        if (icon != null)
        {
            SpriteRenderer sr = recipe.output != null && recipe.output.prefab != null
                ? recipe.output.prefab.GetComponent<SpriteRenderer>()
                : null;
            icon.sprite = sr != null ? sr.sprite : null;
        }

        CanvasGroup group = GetComponent<CanvasGroup>();
        group.alpha = craftable ? 1f : 0.4f;

        button.interactable = craftable;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick());
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One line of a hovered blueprint's ingredient list: item icon + "2 Stick".
public class IngredientRowUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text label;
    [SerializeField] Vector2 iconBox = new(16f, 16f);

    public void Set(Ingredient ingredient)
    {
        UiPixel.FitIntegerScale(icon, ingredient.item.ResolveIcon(), iconBox);
        label.text = ingredient.amount + " " + ingredient.item.displayName;
    }
}

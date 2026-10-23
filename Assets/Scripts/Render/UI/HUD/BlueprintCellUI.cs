using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One blueprint box in the Crafting tab grid: the recipe's output icon. Dims while
// the recipe can't be crafted (missing ingredients / no station nearby); hovering
// shows its details beside the grid, clicking crafts it.
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class BlueprintCellUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image icon;
    [SerializeField] Vector2 iconBox = new(56f, 56f);
    [SerializeField] float unavailableAlpha = 0.4f;

    public Recipe Recipe { get; private set; }

    public event Action<BlueprintCellUI> Clicked;
    public event Action<BlueprintCellUI> HoverStarted;
    public event Action<BlueprintCellUI> HoverEnded;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    public void Set(Recipe recipe)
    {
        Recipe = recipe;
        UiPixel.FitIntegerScale(icon, recipe.output != null ? recipe.output.ResolveIcon() : null, iconBox);
    }

    public void SetAvailable(bool available) =>
        GetComponent<CanvasGroup>().alpha = available ? 1f : unavailableAlpha;

    public void OnPointerEnter(PointerEventData eventData) => HoverStarted?.Invoke(this);
    public void OnPointerExit(PointerEventData eventData) => HoverEnded?.Invoke(this);
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One box of the station panel (StationPanelUI): the item's icon and count, and its
// tooltip on hover (ItemHoverTooltip on this object). A bag stack dropped on the fuel
// or input box goes to that receiver; clicking the output box takes what's in it.
public class StationSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public enum Kind { Fuel, Input, Output }

    [SerializeField] Kind kind;
    [SerializeField] StationPanelUI panel;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [Tooltip("Room for the icon, in reference pixels: the 64x46 box's inside, less a margin.")]
    [SerializeField] Vector2 iconBox = new(56f, 38f);

    Sprite shown;

    // The count only shows with an icon (the start fuel has none) and above 1.
    public void Set(Sprite sprite, int count, GameObject tooltipItem)
    {
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(tooltipItem);
        if (sprite != shown || icon.enabled != (sprite != null))
        {
            shown = sprite;
            UiPixel.FitIntegerScale(icon, sprite, iconBox);
        }

        string text = sprite != null && count > 1 ? count.ToString() : "";
        if (countText.text != text) countText.text = text;
    }

    public void OnDrop(PointerEventData eventData) => panel.Drop(kind);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (kind == Kind.Output && eventData.button == PointerEventData.InputButton.Left) panel.TakeOutput();
    }
}

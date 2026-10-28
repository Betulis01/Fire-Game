using UnityEngine;
using UnityEngine.EventSystems;

// Shows the ItemTooltip for whatever item this UI box currently displays, while the
// pointer is over it. The box (HandSlotUI, EquipSlotUI) calls SetItem whenever its
// content changes, so the tooltip follows an item swapped or removed under the
// pointer. Needs a raycast-target Graphic on this object to receive the hover.
public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] ItemTooltip tooltip;

    GameObject item;
    bool hovered;

    public void SetItem(GameObject newItem)
    {
        if (newItem == item) return;
        if (hovered && tooltip != null) tooltip.Hide(item);
        item = newItem;
        if (hovered && tooltip != null) tooltip.Show(item);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (tooltip != null) tooltip.Show(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if (tooltip != null) tooltip.Hide(item);
    }

    void OnEnable()
    {
        if (UserInput.Instance != null) UserInput.Instance.ModeChanged += OnModeChanged;
    }

    // e.g. the F menu closing under the pointer
    void OnDisable()
    {
        if (UserInput.Instance != null) UserInput.Instance.ModeChanged -= OnModeChanged;
        if (hovered && tooltip != null) tooltip.Hide(item);
        hovered = false;
    }

    // switching to the gamepad hides the cursor, so it isn't hovering anything anymore
    void OnModeChanged(UserInput.InputMode mode)
    {
        if (mode != UserInput.InputMode.Gamepad || !hovered) return;
        if (tooltip != null) tooltip.Hide(item);
        hovered = false;
    }
}

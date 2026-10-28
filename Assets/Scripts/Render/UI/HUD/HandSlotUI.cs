using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One hand frame on the hand bar: the held item's icon plus its stack count. The
// selected hand (Tab / scroll) uses the taller frame; the rect's pivot sits at the
// bottom, so it grows upward and the mouse hint anchored to its top rides along.
// Hovering a held item shows its tooltip (ItemHoverTooltip on the same object), and
// a bag slot dragged onto the frame goes into this hand (swapped or merged).
public class HandSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] HandSide side;
    [SerializeField] Image frame;
    [SerializeField] Sprite normalSprite;      // 96x64
    [SerializeField] Sprite selectedSprite;    // 96x84
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [Tooltip("Box the item icon is integer-scaled to fit (the frame's inside).")]
    [SerializeField] Vector2 iconBox = new(56f, 56f);

    public RectTransform Rect => (RectTransform)transform;

    public void SetSelected(bool selected)
    {
        frame.sprite = selected ? selectedSprite : normalSprite;
        Rect.sizeDelta = frame.sprite.rect.size;
    }

    public void Set(GameObject item, int count)
    {
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(item);
        UiPixel.FitIntegerScale(icon, UiPixel.IconOf(item), iconBox);
        countText.text = count > 1 ? count.ToString() : "";
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (BagDrag.Claim(out Inventory inv, out int slot)) inv.SwapWithHand(slot, side);
    }
}

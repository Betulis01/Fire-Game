using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One slot on the Bag tab: the stack's icon and count. Click it to swap with the
// selected hand (BagTabUI); drag it onto another slot, a hand, an open station menu
// or the world. Hovering shows the item's tooltip (ItemHoverTooltip on this object).
[RequireComponent(typeof(Button))]
public class BagSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [SerializeField] Vector2 iconBox = new(56f, 56f);

    BagTabUI tab;
    int index;
    bool hasItem;
    bool dragging;

    public event Action<int> Clicked;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(index));

    public void Bind(BagTabUI owner, int slotIndex)
    {
        tab = owner;
        index = slotIndex;
    }

    public void Set(GameObject item, int count)
    {
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(item);
        UiPixel.FitIntegerScale(icon, UiPixel.IconOf(item), iconBox);
        countText.text = count > 1 ? count.ToString() : "";
        hasItem = item != null;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!hasItem || tab == null || tab.Inventory == null)
        {
            e.pointerDrag = null;   // nothing to drag: cancel
            return;
        }
        dragging = true;
        BagDrag.Begin(tab.Inventory, index);
        tab.ShowDragIcon(icon.sprite, e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragging) tab.MoveDragIcon(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        tab.HideDragIcon();

        // unclaimed and released over no UI: into the world under the cursor
        bool claimed = BagDrag.End();
        if (!claimed && e.pointerCurrentRaycast.gameObject == null) tab.DropInWorld(index);
    }

    public void OnDrop(PointerEventData e)
    {
        if (BagDrag.Claim(out Inventory inv, out int from)) inv.Move(from, index);
    }

    // the menu closing mid-drag
    void OnDisable()
    {
        if (!dragging) return;
        dragging = false;
        BagDrag.End();
        if (tab != null) tab.HideDragIcon();
    }
}

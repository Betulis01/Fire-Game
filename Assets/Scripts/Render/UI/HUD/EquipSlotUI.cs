using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One equipment box on the Player tab. Shows what's worn in `slot`, or a faint
// silhouette of the slot (head, chest, ring, ...) while it's empty. Hovering a worn
// item shows its tooltip (ItemHoverTooltip on the same object); a bag slot dragged
// onto it wears that item here, if it's gear for this slot. A left or right click
// raises Clicked (EquipmentTabUI takes the item off into the bag).
[RequireComponent(typeof(Button))]
public class EquipSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] EquipSlot slot;
    [SerializeField] Image placeholder;
    [SerializeField] Image icon;
    [SerializeField] Vector2 iconBox = new(56f, 56f);

    public EquipSlot Slot => slot;
    public event Action<EquipSlot> Clicked;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(slot));

    // Can run before Awake (EquipmentTabUI refreshes from its own OnEnable).
    public void Set(GameObject worn)
    {
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(worn);
        Sprite sprite = UiPixel.IconOf(worn);
        UiPixel.FitIntegerScale(icon, sprite, iconBox);
        placeholder.enabled = sprite == null;
    }

    // the Button covers the left click
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right) Clicked?.Invoke(slot);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (BagDrag.Claim(out Inventory inv, out int from)) inv.EquipToBody(from, slot);
    }
}

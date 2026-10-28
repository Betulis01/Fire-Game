using System;
using UnityEngine;
using UnityEngine.UI;

// One equipment box on the Player tab. Shows what's worn in `slot`, or a faint
// silhouette of the slot (head, chest, ring, ...) while it's empty. Hovering a worn
// item shows its tooltip (ItemHoverTooltip on the same object).
[RequireComponent(typeof(Button))]
public class EquipSlotUI : MonoBehaviour
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
}

using System;
using UnityEngine;
using UnityEngine.UI;

// One equipment box on the Player tab. Shows what's worn in `slot`, or a faint
// silhouette of the slot (head, chest, ring, ...) while it's empty.
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

    public void Set(GameObject worn)
    {
        Sprite sprite = UiPixel.IconOf(worn);
        UiPixel.FitIntegerScale(icon, sprite, iconBox);
        placeholder.enabled = sprite == null;
    }
}

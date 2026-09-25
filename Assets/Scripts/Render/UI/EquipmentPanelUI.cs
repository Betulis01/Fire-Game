using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Character tab: one box per EquipSlot showing what's worn there. Click an empty
// box to wear the fitting item from your hands (active hand first); click a filled
// box to take it off into a free hand (dropped at your feet if both are full).
// Boxes are built in code, so the panel only needs this component.
public class EquipmentPanelUI : MonoBehaviour
{
    [Tooltip("Player equipment. Found in the scene if left empty.")]
    [SerializeField] Equipment equipment;

    [Tooltip("Source of the active hand (1/2 keys). Found in the scene if left empty.")]
    [SerializeField] PlayerInteractor interactor;

    [Header("Layout")]
    [SerializeField] Vector2 boxSize = new(72f, 72f);
    [SerializeField] float spacing = 12f;
    [SerializeField] Vector2 topLeft = new(40f, -40f);
    [SerializeField] Color emptyColor = new(0f, 0f, 0f, 0.45f);
    [SerializeField] Color filledColor = new(0.25f, 0.2f, 0.12f, 0.85f);
    [SerializeField] Color fitsHeldColor = new(0.35f, 0.3f, 0.1f, 0.85f);   // empty, but a held item fits

    struct Box { public Image background; public Image icon; }
    Box[] boxes;
    Hands hands;

    void Awake()
    {
        if (equipment == null) equipment = FindFirstObjectByType<Equipment>();
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (equipment != null) hands = equipment.GetComponent<Hands>();
        BuildBoxes();
    }

    void OnEnable()
    {
        if (equipment != null) equipment.Changed += Refresh;
        if (hands != null) hands.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (equipment != null) equipment.Changed -= Refresh;
        if (hands != null) hands.Changed -= Refresh;
    }

    void BuildBoxes()
    {
        EquipSlot[] all = (EquipSlot[])Enum.GetValues(typeof(EquipSlot));
        boxes = new Box[all.Length];

        for (int i = 0; i < all.Length; i++)
        {
            EquipSlot slot = all[i];

            var box = new GameObject($"Slot_{slot}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)box.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = boxSize;
            rt.anchoredPosition = topLeft + new Vector2(0f, -i * (boxSize.y + spacing));
            box.GetComponent<Button>().onClick.AddListener(() => OnClickSlot(slot));

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = (RectTransform)icon.transform;
            iconRt.SetParent(rt, false);
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(8f, 8f);
            iconRt.offsetMax = new Vector2(-8f, -8f);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = (RectTransform)label.transform;
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = new Vector2(1f, 0.5f);
            labelRt.anchorMax = new Vector2(1f, 0.5f);
            labelRt.pivot = new Vector2(0f, 0.5f);
            labelRt.anchoredPosition = new Vector2(spacing, 0f);
            labelRt.sizeDelta = new Vector2(200f, boxSize.y);
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            text.text = slot.ToString();
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;

            boxes[i] = new Box { background = box.GetComponent<Image>(), icon = iconImage };
        }
    }

    void Refresh()
    {
        if (boxes == null || equipment == null) return;
        HandSide active = interactor != null ? interactor.ActiveHand : HandSide.Left;

        for (int i = 0; i < boxes.Length; i++)
        {
            EquipSlot slot = (EquipSlot)i;
            GameObject worn = equipment.Worn(slot);
            Sprite icon = worn != null ? worn.GetComponent<WorldItem>().item.ResolveIcon() : null;

            boxes[i].icon.sprite = icon;
            boxes[i].icon.enabled = icon != null;
            boxes[i].background.color = worn != null ? filledColor
                : equipment.TryFindInHands(slot, active, out _) ? fitsHeldColor
                : emptyColor;
        }
    }

    void OnClickSlot(EquipSlot slot)
    {
        if (equipment == null) return;
        HandSide active = interactor != null ? interactor.ActiveHand : HandSide.Left;

        if (equipment.Worn(slot) != null) equipment.Unequip(slot, active);
        else equipment.TryEquipFromHands(slot, active);
    }
}

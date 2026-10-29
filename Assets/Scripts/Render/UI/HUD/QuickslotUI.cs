using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The action bar's R slot (Quickslot): the assigned food's icon -- dimmed with none
// left in the bag -- its count, and the bite cooldown draining from the top. Drop a
// bag stack of food onto it to assign that food (the stack stays in the bag); click it
// to eat one, like R.
public class QuickslotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] HudRoot root;
    [SerializeField] Image icon;
    [SerializeField] Image cooldown;   // Filled, vertical, from the top
    [SerializeField] TMP_Text countText;
    [SerializeField] Vector2 iconBox = new(56f, 56f);
    [SerializeField, Range(0f, 1f)] float emptyAlpha = 0.4f;

    Quickslot quickslot;

    void OnEnable()
    {
        quickslot = root != null ? root.Quickslot : null;
        if (quickslot != null) quickslot.Changed += Redraw;
        Redraw();
    }

    void OnDisable()
    {
        if (quickslot != null) quickslot.Changed -= Redraw;
    }

    void Update()
    {
        float cd = quickslot != null ? quickslot.Cooldown01 : 0f;
        cooldown.enabled = cd > 0f;
        cooldown.fillAmount = cd;
    }

    void Redraw()
    {
        ItemDefinition food = quickslot != null ? quickslot.Assigned : null;
        int count = quickslot != null ? quickslot.Count : 0;

        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(food != null ? food.prefab : null);
        UiPixel.FitIntegerScale(icon, food != null ? food.ResolveIcon() : null, iconBox);
        icon.color = new Color(1f, 1f, 1f, count > 0 ? 1f : emptyAlpha);
        countText.text = food != null ? count.ToString() : "";
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (quickslot == null || !BagDrag.Active) return;
        if (BagDrag.Claim(out Inventory inv, out int slot)) quickslot.Assign(inv.Item(slot));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (quickslot != null) quickslot.TryEat();
    }
}

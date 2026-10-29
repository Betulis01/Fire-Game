using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One loadout box on the Player tab: a set's (A or B) Main Hand or Off Hand. Shows the
// item in it, or a faint mouse-button hint while it's empty; the inactive set's boxes
// are dimmed. Hovering shows the item's tooltip (ItemHoverTooltip on the same
// object). Drag the item onto another loadout box (either set) to swap the two, onto
// a bag slot to put it away, or into the world to drop it there (like a bag slot); a
// bag slot dragged onto this box equips that item here. A left or right click raises
// Clicked (EquipmentTabUI puts the item back into the bag).
[RequireComponent(typeof(Button))]
public class LoadoutSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
                             IPointerClickHandler
{
    [SerializeField] int set;
    [SerializeField] HandSide side;
    [SerializeField] Image placeholder;
    [SerializeField] Image icon;
    [SerializeField] Vector2 iconBox = new(56f, 56f);
    [Tooltip("Alpha of the whole box while its set isn't the active one.")]
    [SerializeField, Range(0f, 1f)] float inactiveAlpha = 0.5f;

    Loadout loadout;
    bool hasItem;
    bool dragging;

    public int Set => set;
    public HandSide Side => side;
    public event Action<int, HandSide> Clicked;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(set, side));

    public void Bind(Loadout owner) => loadout = owner;

    // Can run before Awake (EquipmentTabUI refreshes from its own OnEnable).
    public void Show(GameObject item, bool activeSet)
    {
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(item);
        Sprite sprite = UiPixel.IconOf(item);
        UiPixel.FitIntegerScale(icon, sprite, iconBox);
        placeholder.enabled = sprite == null;
        hasItem = item != null;

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = activeSet ? 1f : inactiveAlpha;
    }

    // the Button covers the left click
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right) Clicked?.Invoke(set, side);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (BagDrag.Claim(out Inventory inv, out int from)) inv.EquipToLoadout(from, set, side);
        else if (BagDrag.ClaimLoadout(out Loadout owner, out int fromSet, out HandSide fromSide))
            owner.TryMove(fromSet, fromSide, set, side);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!hasItem || loadout == null)
        {
            e.pointerDrag = null;   // nothing to drag: cancel
            return;
        }
        dragging = true;
        BagDrag.BeginLoadout(loadout, set, side);
        if (DragIconUI.Instance != null) DragIconUI.Instance.Show(icon.sprite, e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragging && DragIconUI.Instance != null) DragIconUI.Instance.Move(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        if (DragIconUI.Instance != null) DragIconUI.Instance.Hide();

        // unclaimed and released over no UI: dropped into the world under the cursor
        bool claimed = BagDrag.End();
        if (!claimed && e.pointerCurrentRaycast.gameObject == null && loadout.TryGetComponent(out Inventory inv))
            inv.DropLoadoutAt(set, side, inv.WorldDropPoint());
    }

    // the menu closing mid-drag
    void OnDisable()
    {
        if (!dragging) return;
        dragging = false;
        BagDrag.End();
        if (DragIconUI.Instance != null) DragIconUI.Instance.Hide();
    }
}

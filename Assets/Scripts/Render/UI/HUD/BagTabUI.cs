using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Bag tab: one BagSlotUI per slot of the worn bag (the rest hidden; all of them
// without a bag). Clicking a slot swaps it with the selected hand. A dragged slot shows
// its icon under the cursor; released over the world it goes where the Q drop ghost
// would -- the aimed point, at most the interactor's drop distance away -- into a
// fire or station there, else onto the ground (Inventory.DropAt).
public class BagTabUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] BagSlotUI[] slots;
    [SerializeField] GameObject grid;           // the slots; hidden without a bag
    [SerializeField] Image dragIcon;            // top of the HUD, follows the cursor
    [SerializeField] Vector2 dragIconBox = new(56f, 56f);

    public Inventory Inventory { get; private set; }

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Bind(this, i);
            slots[i].Clicked += OnClickSlot;
        }
        HideDragIcon();
    }

    void OnEnable()
    {
        Inventory = root != null ? root.Inventory : null;
        if (Inventory != null) Inventory.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (Inventory != null) Inventory.Changed -= Refresh;
    }

    void Refresh()
    {
        int capacity = Inventory != null ? Inventory.Capacity : 0;
        grid.SetActive(capacity > 0);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].gameObject.SetActive(i < capacity);
            slots[i].Set(Inventory != null ? Inventory.Item(i) : null, Inventory != null ? Inventory.Count(i) : 0);
        }
    }

    void OnClickSlot(int index)
    {
        if (Inventory == null) return;
        HandSide active = root.Interactor != null ? root.Interactor.ActiveHand : HandSide.Left;
        Inventory.SwapWithHand(index, active);
    }

    public void DropInWorld(int index)
    {
        if (Inventory == null || root.Hands == null) return;
        float range = root.Interactor != null ? root.Interactor.maxDropDistance : 1.5f;
        Vector3 pos = UserInput.Instance.AimPoint(root.Player.transform.position, root.Hands.cam, range);
        Inventory.DropAt(index, pos);
    }

    public void ShowDragIcon(Sprite sprite, PointerEventData e)
    {
        UiPixel.FitIntegerScale(dragIcon, sprite, dragIconBox);
        dragIcon.transform.SetAsLastSibling();
        MoveDragIcon(e);
    }

    public void MoveDragIcon(PointerEventData e)
    {
        var parent = (RectTransform)dragIcon.transform.parent;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, e.position, e.pressEventCamera, out Vector3 world))
            dragIcon.transform.position = world;
    }

    public void HideDragIcon()
    {
        if (dragIcon != null) dragIcon.enabled = false;
    }
}

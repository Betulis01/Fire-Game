using UnityEngine;

// The bag at the bottom of the Player tab: one BagSlotUI per slot of the worn bag (the
// rest hidden). Clicking a slot uses it (Inventory.UseSlot: wield, wear or eat). A
// dragged slot shows its icon under the cursor (DragIconUI); dropped on a loadout or
// armour box it equips there, and released over the world it goes to the aimed
// point, at most the interactor's drop distance away -- into a fire or station there,
// else onto the ground (Inventory.DropAt).
public class BagTabUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] BagSlotUI[] slots;
    [SerializeField] GameObject grid;           // the slots

    public Inventory Inventory { get; private set; }

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Bind(this, i);
            slots[i].Clicked += OnClickSlot;
        }
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
        if (Inventory != null) Inventory.UseSlot(index);
    }

    public void DropInWorld(int index)
    {
        if (Inventory != null) Inventory.DropAt(index, Inventory.WorldDropPoint());
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

// What the player carries in a worn bag: Capacity slots (the worn Bag's capacity, 0
// with no bag), each holding one stack the way a hand does -- a real item object plus
// a count, kept as an inactive child of the player like worn gear. While a bag is
// worn, anything pickupable the player walks over goes straight in (onto a matching
// stack first); an item the player just dropped is left alone until they've stepped
// off it (WorldItem.IsDropped). Taking the bag off spills everything at their feet.
//
// Crafting draws ingredients from the bag as well as the hands (CraftingController);
// Q-feeding only from the hands. Otherwise the bag reaches the world through the
// HUD's Bag tab: click a slot to swap it with a hand, or drag it onto a hand,
// another slot, an open station menu or a spot in the world (BagSlotUI).
[RequireComponent(typeof(Hands), typeof(Equipment))]
public class Inventory : MonoBehaviour
{
    struct Stack
    {
        public GameObject item;
        public int count;
    }

    [Tooltip("Radius around the player's feet that picks items up (it overlaps their interact areas).")]
    [SerializeField] float pickupRadius = 0.15f;

    [Tooltip("How far the extra items of a spilled stack scatter around the drop point.")]
    [SerializeField] float spillScatter = 0.3f;

    Stack[] slots = new Stack[0];
    Hands hands;
    Equipment equipment;
    ContactFilter2D filter;
    readonly List<Collider2D> overlap = new();
    readonly HashSet<WorldItem> underFeet = new();

    // raised after anything moves in or out, so the Bag tab can redraw
    public event Action Changed;

    public int Capacity => slots.Length;
    public GameObject Item(int slot) => InRange(slot) ? slots[slot].item : null;
    public int Count(int slot) => InRange(slot) ? slots[slot].count : 0;

    void Awake()
    {
        hands = GetComponent<Hands>();
        equipment = GetComponent<Equipment>();
        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(LayerMask.GetMask("Interactable"));
    }

    void OnEnable()
    {
        equipment.Changed += Resize;
        Resize();
    }

    void OnDisable() => equipment.Changed -= Resize;

    // The worn bag decides the slot count; slots that no longer fit (the bag came
    // off) spill at the player's feet.
    void Resize()
    {
        Equippable worn = equipment.WornEquippable(EquipSlot.Bag);
        int capacity = worn != null && worn.TryGetComponent(out Bag bag) ? bag.capacity : 0;
        if (capacity == slots.Length) return;

        for (int i = capacity; i < slots.Length; i++) Spill(i, transform.position);
        Array.Resize(ref slots, capacity);
        Changed?.Invoke();
    }

    void Update()
    {
        // pickup areas of the items under the player's feet
        overlap.Clear();
        Physics2D.OverlapCircle(transform.position, pickupRadius, filter, overlap);
        underFeet.Clear();
        foreach (Collider2D c in overlap)
        {
            WorldItem wi = c.GetComponentInParent<WorldItem>();
            if (wi != null) underFeet.Add(wi);
        }

        // a dropped item is fair game again once the player isn't standing on it
        for (int i = WorldItem.Dropped.Count - 1; i >= 0; i--)
        {
            WorldItem wi = WorldItem.Dropped[i];
            if (wi == null) WorldItem.Dropped.RemoveAt(i);
            else if (!underFeet.Contains(wi)) wi.ClearDropped();
        }

        if (Capacity == 0) return;
        foreach (WorldItem wi in underFeet)
            if (!wi.IsHeld && !wi.IsDropped && wi.TryGetComponent(out Pickupable _)) TryAdd(wi);
    }

    // Put a world item into the bag: onto a matching stack with room, else into the
    // first empty slot. False when the bag is full (or there's no bag).
    public bool TryAdd(WorldItem wi)
    {
        if (wi == null) return false;

        if (wi.TryGetComponent(out Stackable stack))
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].item != null && SameItem(slots[i].item, wi.gameObject) && slots[i].count < stack.maxStack)
                {
                    slots[i].count++;
                    Destroy(wi.gameObject);   // world items are single; the stack absorbs it
                    Changed?.Invoke();
                    return true;
                }

        int empty = Array.FindIndex(slots, s => s.item == null);
        if (empty < 0) return false;

        Store(empty, wi.gameObject, 1);
        Changed?.Invoke();
        return true;
    }

    // Click on a slot: its stack goes into the hand (merged onto a matching stack),
    // or swaps with what the hand holds; an empty slot takes the hand's stack.
    public void SwapWithHand(int slot, HandSide side)
    {
        if (!InRange(slot)) return;
        Stack s = slots[slot];
        GameObject held = hands.Held(side);
        if (s.item == null && held == null) return;

        if (s.item == null)
        {
            int count = hands.Count(side);
            Store(slot, hands.Release(side), count);
        }
        else if (held == null || CanMerge(s.item, held))
        {
            if (!hands.TryHoldStack(s.item, s.count, side, out int leftover)) return;
            if (leftover == 0) slots[slot] = default;
            else slots[slot].count = leftover;
        }
        else
        {
            int count = hands.Count(side);
            GameObject fromHand = hands.Release(side);
            slots[slot] = default;
            hands.TryHoldStack(s.item, s.count, side, out _);
            Store(slot, fromHand, count);
        }
        Changed?.Invoke();
    }

    // Drag between slots: move into an empty slot, merge onto a matching stack, or swap.
    public void Move(int from, int to)
    {
        if (!InRange(from) || !InRange(to) || from == to || slots[from].item == null) return;
        Stack a = slots[from];
        Stack b = slots[to];

        if (b.item != null && CanMerge(a.item, b.item))
        {
            int take = Mathf.Min(a.count, b.item.GetComponent<Stackable>().maxStack - b.count);
            if (take <= 0) return;
            slots[to].count += take;
            if (take == a.count)
            {
                slots[from] = default;
                Destroy(a.item);
            }
            else slots[from].count -= take;
        }
        else
        {
            slots[from] = b;
            slots[to] = a;
        }
        Changed?.Invoke();
    }

    // Use up part of a slot's stack (a crafting ingredient); the slot empties at zero.
    public void Consume(int slot, int amount)
    {
        if (!InRange(slot) || slots[slot].item == null || amount <= 0) return;
        slots[slot].count -= amount;
        if (slots[slot].count <= 0)
        {
            Destroy(slots[slot].item);
            slots[slot] = default;
        }
        Changed?.Invoke();
    }

    // Feed a slot's stack into a receiver one item at a time, until it refuses or (a
    // fire) is full; the rest stays in the slot. Returns how many went in.
    public int Deposit(int slot, ItemReceiver receiver)
    {
        if (!InRange(slot) || receiver == null || slots[slot].item == null) return 0;
        WorldItem wi = slots[slot].item.GetComponent<WorldItem>();
        Fuel fuel = receiver is FuelReceiver ? receiver.GetComponent<Fuel>() : null;

        int fed = 0;
        while (slots[slot].count > 0 && (fuel == null || fuel.fuelLevel < 1f)
               && receiver.Accepts(wi) && receiver.Deposit(wi))
        {
            slots[slot].count--;
            fed++;
        }

        if (slots[slot].count == 0)
        {
            Destroy(slots[slot].item);
            slots[slot] = default;
        }
        if (fed > 0) Changed?.Invoke();
        return fed;
    }

    // Drop onto a station's menu: every receiver on it gets a go (fuel, then input).
    public int DepositInto(int slot, GameObject station)
    {
        int fed = 0;
        foreach (ItemReceiver r in station.GetComponents<ItemReceiver>()) fed += Deposit(slot, r);
        return fed;
    }

    // Release a dragged stack at a world point: into a receiver whose zone holds the
    // point (a fire, the furnace, a build site), else onto the ground there.
    public void DropAt(int slot, Vector3 pos)
    {
        if (!InRange(slot) || slots[slot].item == null) return;
        ItemReceiver receiver = ItemReceiver.FindInZone(pos, slots[slot].item.GetComponent<WorldItem>());
        if (receiver != null) Deposit(slot, receiver);
        else Spill(slot, pos);
    }

    // The whole stack into the world: the item itself at pos, the rest scattered around it.
    public void Spill(int slot, Vector3 pos)
    {
        if (!InRange(slot) || slots[slot].item == null) return;
        Stack s = slots[slot];
        slots[slot] = default;

        Place(s.item, pos);
        GameObject prefab = s.item.GetComponent<WorldItem>().item.prefab;
        for (int i = 1; i < s.count; i++)
            Place(Instantiate(prefab), pos + (Vector3)(UnityEngine.Random.insideUnitCircle * spillScatter));
        Changed?.Invoke();
    }

    // In the bag an item is "held" (pickup collider off, never Dropped) and inactive.
    void Store(int slot, GameObject item, int count)
    {
        item.transform.SetParent(transform, false);
        item.GetComponent<WorldItem>().SetHeld(true);
        item.SetActive(false);
        slots[slot] = new Stack { item = item, count = count };
    }

    static void Place(GameObject item, Vector3 pos)
    {
        item.transform.SetParent(null);
        item.transform.SetPositionAndRotation(pos, Quaternion.identity);
        item.SetActive(true);
        foreach (SpriteRenderer sr in item.GetComponentsInChildren<SpriteRenderer>(true))
            sr.flipX = false;
        item.GetComponent<WorldItem>().SetHeld(false);
    }

    bool InRange(int slot) => slot >= 0 && slot < slots.Length;

    static bool SameItem(GameObject a, GameObject b) =>
        a.GetComponent<WorldItem>().item == b.GetComponent<WorldItem>().item;

    static bool CanMerge(GameObject a, GameObject b) => SameItem(a, b) && a.TryGetComponent(out Stackable _);
}

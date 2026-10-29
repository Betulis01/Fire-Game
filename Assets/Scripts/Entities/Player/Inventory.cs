using System;
using System.Collections.Generic;
using UnityEngine;

// What the player carries in the worn bag: Capacity slots (the worn Bag's capacity;
// the player starts with the 2-slot Pouch), each holding one stack -- a real item
// object plus a count, kept as an inactive child of the player like worn gear. The
// bag takes Small items only (ItemSize); gear sits in it one per slot.
//
// It also routes everything the player takes (TryTake: an E press, a crafted item):
// Big items into a free hand, anything else into the bag (onto a matching stack
// first) -- gear included: tools, weapons and armour only go on on purpose (Hold E,
// the Player tab). With no room the item stays where it was -- nothing the
// player owns is ever dropped as a side effect.
//
// Walking over a Small item only tops up a stack the bag already has room on; it
// never takes a slot of its own, so the bag doesn't fill with things the player
// didn't choose. An item the player just dropped is left alone until they've stepped
// off it (WorldItem.IsDropped).
//
// Crafting draws ingredients from the bag as well as the hands (CraftingController).
// The bag reaches the world through the HUD's Player tab: click a slot to use it
// (wield, wear, eat), or drag it onto a loadout or armour box, another slot, an open
// station menu or a spot in the world (BagSlotUI).
[RequireComponent(typeof(Hands), typeof(Equipment), typeof(Loadout))]
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
    Loadout loadout;
    FoodUse food;
    ContactFilter2D filter;
    readonly List<Collider2D> overlap = new();
    readonly HashSet<WorldItem> underFeet = new();

    // raised after anything moves in or out, so the Player tab can redraw
    public event Action Changed;

    public int Capacity => slots.Length;
    public GameObject Item(int slot) => InRange(slot) ? slots[slot].item : null;
    public int Count(int slot) => InRange(slot) ? slots[slot].count : 0;
    public bool HasEmptySlot => Array.FindIndex(slots, s => s.item == null) >= 0;

    void Awake()
    {
        hands = GetComponent<Hands>();
        equipment = GetComponent<Equipment>();
        loadout = GetComponent<Loadout>();
        food = GetComponent<FoodUse>();
        filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(LayerMask.GetMask("Interactable"));
    }

    void OnEnable()
    {
        equipment.Changed += Resize;
        Resize();
    }

    void OnDisable() => equipment.Changed -= Resize;

    // The worn bag decides the slot count; slots that no longer fit (a smaller bag was
    // swapped in) spill at the player's feet -- the one last-resort drop.
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

        foreach (WorldItem wi in underFeet)
            if (!wi.IsHeld && !wi.IsDropped && wi.TryGetComponent(out Pickupable _)) TryTopUp(wi);
    }

    public static bool IsBig(WorldItem wi) => wi != null && wi.item != null && wi.item.IsBig;

    // An E press or a crafted item: Big into a free hand, else into the bag (gear
    // too: wielding or wearing it takes a Hold E or the Player tab). False when there's
    // no room for it.
    public bool TryTake(WorldItem wi)
    {
        if (wi == null) return false;
        if (IsBig(wi)) return hands.TryHoldFree(wi.gameObject);
        return TryAdd(wi);
    }

    // Hold E on gear lying in the world: wield it (a free slot of the active set, else in
    // place of the Main Hand) or wear it, a bag included. What it replaces goes into
    // the bag, else onto the ground at the player's feet -- the last resort, since the
    // hands only carry Big items. False when it isn't gear (or mid-swing).
    public bool TryEquipFromWorld(WorldItem wi)
    {
        if (wi == null) return false;
        GameObject item = wi.gameObject;

        if (Loadout.Fits(item))
        {
            HandSide side = loadout.AutoSlot(item);
            if (!loadout.TryPut(item, loadout.ActiveSet, side, out GameObject previous, out GameObject displaced))
                return false;
            StoreOrDrop(previous);
            StoreOrDrop(displaced);
            Changed?.Invoke();
            return true;
        }

        if (!item.TryGetComponent(out Equippable _)) return false;
        StoreOrDrop(equipment.Wear(item));   // a bag swap resizes the slots first
        Changed?.Invoke();
        return true;
    }

    // Gear the player is missing: it would go straight into an empty slot of the
    // active loadout or onto the body (E focus prefers it).
    public bool IsMissingGear(GameObject item) =>
        (Loadout.Fits(item) && loadout.TryGetFreeSlot(item, out _)) || equipment.CanWearInEmptySlot(item);

    // Put a Small world item into the bag: onto a matching stack with room, else into
    // the first empty slot. False for a Big item or when the bag is full.
    public bool TryAdd(WorldItem wi)
    {
        if (wi == null || IsBig(wi)) return false;
        if (TryTopUp(wi)) return true;

        int empty = Array.FindIndex(slots, s => s.item == null);
        if (empty < 0) return false;

        Store(empty, wi.gameObject, 1);
        Changed?.Invoke();
        return true;
    }

    // Merge a world item onto a matching stack that has room (auto-pickup's only move).
    bool TryTopUp(WorldItem wi)
    {
        if (IsBig(wi) || !wi.TryGetComponent(out Stackable stack)) return false;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].item != null && SameItem(slots[i].item, wi.gameObject) && slots[i].count < stack.maxStack)
            {
                slots[i].count++;
                Destroy(wi.gameObject);   // world items are single; the stack absorbs it
                Changed?.Invoke();
                return true;
            }
        return false;
    }

    // Whether one `item` would fit in the bag once `freed` stacks' worth of it has left
    // (crafting: a slot its ingredients empty counts as free). freed[i] is how much
    // slot i loses.
    public bool HasRoomFor(ItemDefinition item, int[] freed)
    {
        if (item == null || item.IsBig) return false;
        int maxStack = item.prefab != null && item.prefab.TryGetComponent(out Stackable s) ? s.maxStack : 1;
        for (int i = 0; i < slots.Length; i++)
        {
            int count = slots[i].count - (freed != null && i < freed.Length ? freed[i] : 0);
            if (slots[i].item == null || count <= 0) return true;
            if (ItemIn(slots[i].item) == item && count < maxStack) return true;
        }
        return false;
    }

    // Click on a slot: wield a tool or weapon (into the active set's empty Main / Off
    // Hand, else in place of the Main Hand), wear gear, or eat one of a food stack.
    public void UseSlot(int slot)
    {
        GameObject item = Item(slot);
        if (item == null) return;

        if (Loadout.Fits(item)) EquipToLoadout(slot, loadout.ActiveSet, loadout.AutoSlot(item));
        else if (item.TryGetComponent(out Equippable _)) EquipToBody(slot);
        else if (food != null && food.TryEat(item)) Consume(slot, 1);
    }

    // A slot's item into a loadout slot (a click, or dragged onto that box). What was
    // there comes back into this bag slot; what a two-handed tool pushes out of the
    // other slot needs an empty slot of its own, or nothing happens.
    public bool EquipToLoadout(int slot, int set, HandSide side)
    {
        GameObject item = Item(slot);
        if (!Loadout.Fits(item)) return false;
        if (loadout.Outgoing(item, set, side) - 1 > EmptySlots()) return false;   // this slot takes one back

        slots[slot] = default;   // the loadout owns it now
        if (!loadout.TryPut(item, set, side, out GameObject previous, out GameObject displaced))
        {
            slots[slot] = new Stack { item = item, count = 1 };
            return false;
        }
        if (previous != null) StoreBack(slot, previous);
        if (displaced != null) StoreBack(slot, displaced);
        Changed?.Invoke();
        return true;
    }

    // A slot's gear onto the body, in its own slot (a click, or dragged onto that box;
    // `target` must match). What was worn comes back into this bag slot -- for a bag
    // swap, into the new bag, contents and all.
    public bool EquipToBody(int slot, EquipSlot? target = null)
    {
        GameObject item = Item(slot);
        if (item == null || !item.TryGetComponent(out Equippable e)) return false;
        if (target != null && e.slot != target.Value) return false;

        slots[slot] = default;
        GameObject previous = equipment.Wear(item);   // a bag swap resizes the slots here
        if (previous != null) StoreBack(slot, previous);
        Changed?.Invoke();
        return true;
    }

    // Take a loadout item off into the bag. False with no empty slot (it stays on).
    public bool UnequipLoadout(int set, HandSide side)
    {
        if (loadout.Item(set, side) == null || !HasEmptySlot) return false;
        if (!loadout.TryPut(null, set, side, out GameObject previous, out _)) return false;
        return TryAdd(previous.GetComponent<WorldItem>());
    }

    // A loadout item dragged onto bag slot `slot`: into it if it's empty, or swapped
    // with the tool or weapon there (which goes into that loadout slot). False when
    // the slot holds something else.
    public bool UnequipLoadoutTo(int set, HandSide side, int slot)
    {
        GameObject item = loadout.Item(set, side);
        if (item == null || !InRange(slot)) return false;
        if (slots[slot].item != null) return Loadout.Fits(slots[slot].item) && EquipToLoadout(slot, set, side);

        if (!loadout.TryPut(null, set, side, out _, out _)) return false;
        Store(slot, item, 1);
        Changed?.Invoke();
        return true;
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

    // Use up part of a slot's stack (a crafting ingredient, a bite); the slot empties at zero.
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

    // Where something dragged off the HUD into the world lands: the aimed point, at
    // most the interactor's drop distance away.
    public Vector3 WorldDropPoint()
    {
        float range = TryGetComponent(out PlayerInteractor interactor) ? interactor.maxDropDistance : 1.5f;
        return UserInput.Instance.AimPoint(transform.position, hands.cam, range);
    }

    // A loadout item dragged off the HUD into the world: into a receiver whose zone
    // holds the point if it takes it (a torch into a fire), else onto the ground there.
    public void DropLoadoutAt(int set, HandSide side, Vector3 pos)
    {
        GameObject item = loadout.Item(set, side);
        if (item == null || !loadout.TryPut(null, set, side, out _, out _)) return;

        WorldItem wi = item.GetComponent<WorldItem>();
        ItemReceiver receiver = ItemReceiver.FindInZone(pos, wi);
        if (receiver != null && receiver.Deposit(wi)) Destroy(item);
        else Place(item, pos);
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

    // Something swapped out (a loadout item, worn gear) goes back into `slot`, else the
    // first empty slot; only with the bag full does it land at the player's feet.
    void StoreBack(int slot, GameObject item)
    {
        if (!InRange(slot) || slots[slot].item != null) slot = Array.FindIndex(slots, s => s.item == null);
        if (slot >= 0) Store(slot, item, 1);
        else Place(item, transform.position);
    }

    // Something swapped out by Hold E: into the bag, else at the player's feet.
    void StoreOrDrop(GameObject item)
    {
        if (item == null) return;
        if (!TryAdd(item.GetComponent<WorldItem>())) Place(item, transform.position);
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

    int EmptySlots()
    {
        int n = 0;
        foreach (Stack s in slots)
            if (s.item == null) n++;
        return n;
    }

    static ItemDefinition ItemIn(GameObject item) =>
        item != null && item.TryGetComponent(out WorldItem wi) ? wi.item : null;

    static bool SameItem(GameObject a, GameObject b) =>
        a.GetComponent<WorldItem>().item == b.GetComponent<WorldItem>().item;

    static bool CanMerge(GameObject a, GameObject b) => SameItem(a, b) && a.TryGetComponent(out Stackable _);
}

using System;
using UnityEngine;

// The action bar's consumable slot: a food type the player assigned (a food stack
// dragged from the bag onto it). It shows how many of that food the bag holds, and R
// (or a click on the slot) eats one from the bag -- after eatCooldown, and also
// while carrying, so a berry from the pocket never breaks a hauling trip. With none
// left the slot stays assigned; assigning another food replaces it.
[RequireComponent(typeof(Inventory))]
public class Quickslot : MonoBehaviour
{
    [Tooltip("Seconds between bites.")]
    [Min(0f)] public float eatCooldown = 0.5f;

    Inventory inventory;
    FoodUse food;
    float readyAt;

    public ItemDefinition Assigned { get; private set; }

    // raised when the assignment or the bag's contents change
    public event Action Changed;

    public float Cooldown01 => eatCooldown > 0f ? Mathf.Clamp01((readyAt - Time.time) / eatCooldown) : 0f;

    // How many of the assigned food the bag holds.
    public int Count
    {
        get
        {
            if (Assigned == null) return 0;
            int n = 0;
            for (int i = 0; i < inventory.Capacity; i++)
                if (ItemIn(inventory.Item(i)) == Assigned) n += inventory.Count(i);
            return n;
        }
    }

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        food = GetComponent<FoodUse>();
    }

    void OnEnable() => inventory.Changed += RaiseChanged;
    void OnDisable() => inventory.Changed -= RaiseChanged;

    void RaiseChanged() => Changed?.Invoke();

    void Update()
    {
        if (UserInput.Instance.Quickslot) TryEat();
    }

    // Assign the food in `item` (a bag stack's item); false if it isn't food.
    public bool Assign(GameObject item)
    {
        ItemDefinition def = ItemIn(item);
        if (def == null || !item.TryGetComponent(out Edible _)) return false;
        Assigned = def;
        Changed?.Invoke();
        return true;
    }

    // Eat one of the assigned food from the bag. False on cooldown or with none left.
    public bool TryEat()
    {
        if (Assigned == null || food == null || Time.time < readyAt) return false;
        for (int i = 0; i < inventory.Capacity; i++)
        {
            GameObject item = inventory.Item(i);
            if (ItemIn(item) != Assigned || !food.TryEat(item)) continue;
            inventory.Consume(i, 1);
            readyAt = Time.time + eatCooldown;
            return true;
        }
        return false;
    }

    static ItemDefinition ItemIn(GameObject item) =>
        item != null && item.TryGetComponent(out WorldItem wi) ? wi.item : null;
}

using UnityEngine;

// Eating capability: applies an Edible item's effects. Food lives in the bag, so
// clicking a food stack on the Player tab eats one (Inventory.UseSlot), which then
// takes that one off the stack.
public class FoodUse : MonoBehaviour
{
    Health health;
    PlayerBuffs buffs;

    void Awake()
    {
        health = GetComponent<Health>();
        buffs = GetComponent<PlayerBuffs>();
    }

    // Eat `item` if it's Edible; true when it was (the caller uses one up).
    public bool TryEat(GameObject item)
    {
        if (item == null || !item.TryGetComponent(out Edible edible)) return false;

        if (edible.heal > 0f) health.Heal(edible.heal);
        if (edible.damage > 0f) health.TakeDamage(edible.damage, DamageType.Poison);

        if (edible.buffDuration > 0f)
        {
            if (!Mathf.Approximately(edible.healthChange, 0f))
                buffs.ApplyMaxHealthBuff(edible.healthChange, edible.buffDuration);

            if (!Mathf.Approximately(edible.speedMultiplier, 1f))
                buffs.ApplySpeedBuff(edible.speedMultiplier, edible.buffDuration);
        }

        return true;
    }
}

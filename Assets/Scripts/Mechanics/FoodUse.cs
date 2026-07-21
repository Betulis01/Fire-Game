using UnityEngine;

// Eating capability: if the given hand holds an Edible item, applies its
// effects and consumes one. Called by ItemUser before it falls back to
// WeaponUse, so the Use button eats food instead of swinging whatever's in
// that hand.
[RequireComponent(typeof(Hands))]
public class FoodUse : MonoBehaviour
{
    Hands hands;
    Health health;
    PlayerBuffs buffs;

    void Awake()
    {
        hands = GetComponent<Hands>();
        health = GetComponent<Health>();
        buffs = GetComponent<PlayerBuffs>();
    }

    public bool TryEat(HandSide side)
    {
        GameObject held = hands.Held(side);
        if (held == null || !held.TryGetComponent(out Edible edible)) return false;

        if (edible.heal > 0f) health.Heal(edible.heal);
        if (edible.damage > 0f) health.TakeDamage(edible.damage, DamageType.Poison);

        if (edible.buffDuration > 0f)
        {
            if (!Mathf.Approximately(edible.healthChange, 0f))
                buffs.ApplyMaxHealthBuff(edible.healthChange, edible.buffDuration);

            if (!Mathf.Approximately(edible.speedMultiplier, 1f))
                buffs.ApplySpeedBuff(edible.speedMultiplier, edible.buffDuration);
        }

        hands.Consume(side, 1);
        return true;
    }
}

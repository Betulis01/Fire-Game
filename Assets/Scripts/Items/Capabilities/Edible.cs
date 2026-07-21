using UnityEngine;

// Marks an item prefab as edible: what eating it does to the player. Consumed
// by FoodUse on Use while the item is the active hand's held item.
public class Edible : MonoBehaviour
{
    public float heal;                 // instant; heals this amount on eating
    public float damage;               // instant; deals this much Poison damage on eating
    public float healthChange;         // temporary max-health delta for buffDuration; positive/negative
    public float speedMultiplier = 1f; // 1 = no effect; 0.9 = -10%, 1.1 = +10%
    public float buffDuration;         // seconds the healthChange/speedMultiplier buffs last (0 = no timed buff)
}

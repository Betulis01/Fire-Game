using UnityEngine;

// Composite dispatcher for the Use button (left click): resolves which hand
// acts, then hands the press to whichever capability actually applies to
// what's in that hand -- food first, then weapons. Neither capability reads
// input itself, so there is exactly one reaction per press and no risk of two
// components disagreeing about what a hand holds mid-frame (e.g. eating the
// last piece of food clearing the hand out from under a weapon check). Add
// further usable-item handlers here the same way: expose a TryX(HandSide) that
// returns whether it handled the press, and check it before falling through.
public class ItemUser : MonoBehaviour
{
    WeaponUse weaponUse;
    FoodUse foodUse;

    void Awake()
    {
        weaponUse = GetComponent<WeaponUse>();
        foodUse = GetComponent<FoodUse>();
    }

    void Update()
    {
        if (!UserInput.Instance.Use) return;

        HandSide side = weaponUse != null ? weaponUse.ResolveAttackHand() : HandSide.Left;
        if (foodUse != null && foodUse.TryEat(side)) return;
        if (weaponUse != null) weaponUse.TryUse(side);
    }
}

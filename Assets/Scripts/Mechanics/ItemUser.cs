using UnityEngine;

// Composite dispatcher for the Use buttons (LMB = left hand, RMB = right hand):
// hands each press to whichever capability actually applies to what's in that
// hand -- a shield blocks (ShieldBlock reads the held button itself, so the press
// is left alone here), then food, then weapons. Neither capability reads the press
// itself, so there is exactly one reaction per press and no risk of two
// components disagreeing about what a hand holds mid-frame (e.g. eating the
// last piece of food clearing the hand out from under a weapon check). Add
// further usable-item handlers here the same way: expose a TryX(HandSide) that
// returns whether it handled the press, and check it before falling through.
// Runs late (+100) so PlacementGhost / the drop ghost can claim a press first
// (UserInput.ConsumeUse) without it also swinging.
[DefaultExecutionOrder(100)]
public class ItemUser : MonoBehaviour
{
    Hands hands;
    WeaponUse weaponUse;
    FoodUse foodUse;

    void Awake()
    {
        hands = GetComponent<Hands>();
        weaponUse = GetComponent<WeaponUse>();
        foodUse = GetComponent<FoodUse>();
    }

    void Update()
    {
        // At most one press per frame: the single Animator layer plays one swing at a
        // time, and a same-frame second press would only be dropped by TryUse anyway.
        if (UserInput.Instance.UsePressed(HandSide.Left)) Use(HandSide.Left);
        else if (UserInput.Instance.UsePressed(HandSide.Right)) Use(HandSide.Right);
    }

    void Use(HandSide side)
    {
        GameObject held = hands != null ? hands.Held(side) : null;
        if (held != null && held.TryGetComponent(out Shield _)) return;
        if (foodUse != null && foodUse.TryEat(side)) return;
        if (weaponUse != null) weaponUse.TryUse(side);
    }
}

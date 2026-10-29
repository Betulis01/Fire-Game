using UnityEngine;

// Composite dispatcher for the Use buttons (LMB = Main Hand, RMB = Off Hand):
// hands each press to whichever capability actually applies to the drawn loadout
// item of that hand -- a shield blocks (ShieldBlock reads the held button itself,
// so the press is left alone here), else a weapon swings. While the hands carry
// something there's nothing drawn to use: a left click puts the load down instead,
// which draws the weapons again, so fighting is always one click away. Add further
// usable-item handlers here the same way: expose a TryX(HandSide) that returns
// whether it handled the press, and check it before falling through.
//
// Dual wielding: with a melee weapon drawn in each hand (fists, a torch or a shield
// don't count), left clicks chain into a combo that alternates the hands -- L, R,
// L -- as long as each comes within comboWindow of the previous swing ending; after
// a pause the chain restarts on the Main Hand. Each swing is that hand's own weapon,
// charged by the left button. Right click still swings the Off Hand on its own.
// Runs late (+100) so PlacementGhost / the drop ghost can claim a press first
// (UserInput.ConsumeUse) without it also swinging.
[DefaultExecutionOrder(100)]
public class ItemUser : MonoBehaviour
{
    [Tooltip("Seconds after a swing ends that the next left click still continues the " +
             "dual-wield combo (swinging the other hand).")]
    [SerializeField] float comboWindow = 0.6f;

    Hands hands;
    WeaponUse weaponUse;
    PlayerAnimator animator;

    HandSide comboNext = HandSide.Left;
    float lastSwingEnd = float.NegativeInfinity;
    bool wasAttacking;

    void Awake()
    {
        hands = GetComponent<Hands>();
        weaponUse = GetComponent<WeaponUse>();
        animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        bool attacking = animator != null && animator.IsAttacking;
        if (wasAttacking && !attacking) lastSwingEnd = Time.time;
        wasAttacking = attacking;

        // At most one press per frame: the single Animator layer plays one swing at a
        // time, and a same-frame second press would only be dropped by TryUse anyway.
        if (UserInput.Instance.UsePressed(HandSide.Left)) Use(HandSide.Left);
        else if (UserInput.Instance.UsePressed(HandSide.Right)) Use(HandSide.Right);
    }

    void Use(HandSide button)
    {
        if (hands == null) return;
        if (hands.IsCarrying)
        {
            if (button != HandSide.Left) return;
            UserInput.Instance.ConsumeUse(button);
            hands.DropLoad(transform.position);
            return;
        }

        HandSide side = button;
        bool combo = button == HandSide.Left && DualWielding();
        if (combo && Time.time - lastSwingEnd <= comboWindow) side = comboNext;

        GameObject item = hands.ActiveItem(side);
        if (item != null && item.TryGetComponent(out Shield _)) return;
        if (weaponUse == null || !weaponUse.TryUse(side, button)) return;

        comboNext = combo && side == HandSide.Left ? HandSide.Right : HandSide.Left;
    }

    bool DualWielding() => IsMeleeWeapon(hands.ActiveItem(HandSide.Left)) && IsMeleeWeapon(hands.ActiveItem(HandSide.Right));

    static bool IsMeleeWeapon(GameObject item) =>
        item != null && item.TryGetComponent(out MeleeWeapon _) && item.TryGetComponent(out Tool tool)
        && tool.kind != ToolKind.Fist && tool.kind != ToolKind.Torch;
}

using UnityEngine;
using UnityEngine.UI;

// Bottom action bar: every action the player can take, left to right --
//   [1 Skill][2 Skill][LMB Main Hand][RMB Off Hand][R Quickslot]  [..][..]  (scroll)
//                                                                 [..][..]
// Skills are the drawn weapons' spells (ActiveSkillSlots). The hand slots show the
// active loadout set's items -- dimmed while the hands carry something (the set
// that gets drawn when the load comes off), the Off Hand ghosting a two-handed Main
// Hand -- with a cooldown for the swap's beat (Loadout.SwapProgress01) and a full
// one while that hand's swing recovers (WeaponUse.IsBusy). The Quickslot is empty
// until consumables get one. Beside the bar both loadout sets show small, the
// active one bright, and the scroll hint (swap sets) right of them; the "Q Drop" hint
// sits under the bar while carrying. The hand slots' items drag like loadout boxes. Everything is placed explicitly by HudBuilder (no
// layout groups) to stay pixel-exact.
public class ActionBarUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] ActionSlotUI leftAction;     // skill 1
    [SerializeField] ActionSlotUI rightAction;    // skill 2
    [SerializeField] ActionSlotUI mainHand;
    [SerializeField] ActionSlotUI offHand;
    [SerializeField] GameObject dropHint;         // Q key + "Drop"; only while carrying

    [Header("Loadout sets (A, B), Main + Off each")]
    [SerializeField] CanvasGroup[] setRows;       // one per set
    [SerializeField] Image[] setIcons;            // set * 2 + hand
    [SerializeField] Vector2 setIconBox = new(18f, 18f);
    [SerializeField, Range(0f, 1f)] float inactiveSetAlpha = 0.4f;

    [Tooltip("Alpha of the hand slots' items while the loadout is stowed (carrying) or ghosted.")]
    [SerializeField, Range(0f, 1f)] float stowedAlpha = 0.4f;

    Hands hands;
    Loadout loadout;
    WeaponUse weaponUse;
    ActiveSkillSlots skills;

    void OnEnable()
    {
        if (root == null) return;
        hands = root.Hands;
        loadout = root.Loadout;
        skills = root.Skills;
        weaponUse = root.Player != null ? root.Player.GetComponent<WeaponUse>() : null;

        if (hands != null) hands.Changed += Redraw;
        if (skills != null) skills.Changed += RedrawSkills;
        mainHand.BindLoadout(loadout);
        offHand.BindLoadout(loadout);
        Redraw();
    }

    void OnDisable()
    {
        if (hands != null) hands.Changed -= Redraw;
        if (skills != null) skills.Changed -= RedrawSkills;
    }

    void Update()
    {
        if (loadout == null) return;
        float swap = loadout.SwapProgress01;
        mainHand.SetCooldown(Recovery(HandSide.Left, swap));
        offHand.SetCooldown(Recovery(HandSide.Right, swap));
    }

    float Recovery(HandSide side, float swap) =>
        weaponUse != null && weaponUse.IsBusy(side) ? 1f : swap;

    // Hands.Changed covers the loadout too (it relays Loadout.Changed).
    void Redraw()
    {
        bool carrying = hands != null && hands.IsCarrying;
        if (dropHint != null) dropHint.SetActive(carrying);

        if (loadout != null)
        {
            float alpha = carrying ? stowedAlpha : 1f;
            GameObject main = loadout.Drawn(HandSide.Left);
            bool twoHanded = loadout.MainIsTwoHanded(loadout.ActiveSet);
            mainHand.SetItem(main, alpha);
            offHand.SetItem(twoHanded ? main : loadout.Drawn(HandSide.Right), twoHanded ? stowedAlpha : alpha);

            for (int set = 0; set < Loadout.Sets && set < setRows.Length; set++)
            {
                setRows[set].alpha = set == loadout.ActiveSet ? 1f : inactiveSetAlpha;
                for (int hand = 0; hand < 2; hand++)
                {
                    int i = set * 2 + hand;
                    if (i < setIcons.Length)
                        UiPixel.FitIntegerScale(setIcons[i], UiPixel.IconOf(loadout.Item(set, (HandSide)hand)), setIconBox);
                }
            }
        }
        RedrawSkills();
    }

    void RedrawSkills()
    {
        leftAction.SetSkill(skills != null ? skills.SkillFor(HandSide.Left) : null);
        rightAction.SetSkill(skills != null ? skills.SkillFor(HandSide.Right) : null);
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One slot on the action bar: an icon with a cooldown overlay that drains from the
// top. A skill slot (keys 1 / 2) shows the spell dragged onto it (ActiveSkillSlots),
// dimmed while no drawn hand can cast it, and follows the IActiveSkill's own
// cooldown: drop a learned spell from a skill tree (or the other skill slot -- they
// swap) onto it, drag its spell off onto nothing to empty it. A hand slot (LMB = Main Hand, RMB = Off Hand) shows the
// loadout item, with its cooldown set from outside (ActionBarUI: the swap's beat,
// attack recovery). Hovering a hand slot's item shows its tooltip (ItemHoverTooltip
// on the same object, when there is one). A hand slot's item drags like a loadout box
// on the Player tab (onto the other hand, a loadout box, a bag slot or the world), and
// takes a bag slot or loadout item dropped on it into the active set's hand -- or, fuel
// onto a held torch, feeds it (Inventory.TryFeed). While an aimed spell is armed
// (ActionBarUI) the hand slots show what their click does instead -- the spell on the
// Main Hand, cancel on the Off Hand (SetOverride) -- and the spell's own slot lights its
// armed frame (SetArmed).
public class ActionSlotUI : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Image icon;
    [SerializeField] Image cooldown;   // Filled, vertical, from the top
    [SerializeField] Image armed;      // the frame lit while this slot's spell is armed
    [SerializeField] Vector2 iconBox = new(56f, 56f);
    [SerializeField] ItemMeterUI meter;   // the item's bar (a torch's fuel)

    [Tooltip("A loadout hand slot (Main / Off Hand) rather than a skill slot.")]
    [SerializeField] bool handSlot;
    [SerializeField] HandSide side;

    [Tooltip("Icon alpha of a skill slot's spell while no drawn hand can cast it.")]
    [SerializeField, Range(0f, 1f)] float uncastableAlpha = 0.4f;

    IActiveSkill skill;
    ActiveSkillSlots skillSlots;
    int skillIndex = -1;
    Loadout loadout;
    bool hasItem;
    bool overridden;   // showing an armed spell's click (SetOverride), not the item
    Sprite itemSprite;
    float itemAlpha = 1f;
    bool dragging;
    int dragSet;   // the set the dragged item came from (the active one when it started)

    // the loadout a hand slot drags from and drops into (ActionBarUI)
    public void BindLoadout(Loadout owner) => loadout = owner;

    // the skill slots a skill slot shows and assigns, and which one it is (ActionBarUI)
    public void BindSkillSlot(ActiveSkillSlots slots, int index)
    {
        skillSlots = slots;
        skillIndex = index;
    }

    public void SetSkill(IActiveSkill newSkill)
    {
        skill = newSkill;
        UiPixel.FitIntegerScale(icon, skill?.Icon, iconBox);
        cooldown.enabled = skill != null;
        cooldown.fillAmount = 0f;
    }

    // A hand slot's item; `alpha` fades it (a stowed loadout, the Off Hand beside a
    // two-handed tool).
    public void SetItem(GameObject item, float alpha = 1f)
    {
        itemSprite = UiPixel.IconOf(item);
        itemAlpha = alpha;
        hasItem = item != null;
        if (overridden) return;
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(item);
        if (meter != null) meter.Show(item);
        ShowIcon(itemSprite, alpha);
    }

    // A hand slot showing what its click does while a spell is armed (the spell, or
    // cancel) in place of its item -- no item meter or tooltip meanwhile. ClearOverride
    // shows the item again (true when there was an override to clear); the owner's
    // SetItem then restores its meter and tooltip.
    public void SetOverride(Sprite sprite)
    {
        overridden = true;
        ShowIcon(sprite, 1f);
        if (meter != null) meter.Show(null);
        if (TryGetComponent(out ItemHoverTooltip hover)) hover.SetItem(null);
        SetCooldown(0f);
    }

    public bool ClearOverride()
    {
        if (!overridden) return false;
        overridden = false;
        ShowIcon(itemSprite, itemAlpha);
        return true;
    }

    public void SetArmed(bool on)
    {
        if (armed != null) armed.enabled = on;
    }

    void ShowIcon(Sprite sprite, float alpha)
    {
        UiPixel.FitIntegerScale(icon, sprite, iconBox);
        icon.color = new Color(1f, 1f, 1f, alpha);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!handSlot)
        {
            if (skillSlots != null && BagDrag.ClaimSkill(out IActiveSkill dropped, out int _))
                UiSfx.Play(skillSlots.Assign(skillIndex, dropped) ? UiSound.SpellAssign : UiSound.Denied);
            return;
        }
        if (loadout == null) return;
        if (BagDrag.Claim(out Inventory inv, out int from))
        {
            int before = inv.Count(from);
            ItemDefinition fuel = UiDrop.Def(inv.Item(from));
            GameObject target = loadout.Drawn(side);
            if (inv.TryFeed(from, target)) UiDrop.Fed(before, inv.Count(from), fuel, target);
            else if (!inv.EquipToLoadout(from, loadout.ActiveSet, side)) UiSfx.Play(UiSound.Denied);
        }
        else if (BagDrag.ClaimLoadout(out Loadout owner, out int fromSet, out HandSide fromSide))
        {
            UiSound s = owner.Item(loadout.ActiveSet, side) == null ? UiSound.Place : UiSound.Swap;
            UiSfx.Play(owner.TryMove(fromSet, fromSide, loadout.ActiveSet, side) ? s : UiSound.Denied);
        }
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!handSlot)
        {
            if (skillSlots == null || skill == null) { e.pointerDrag = null; return; }
            dragging = true;
            BagDrag.BeginSkill(skill, skillIndex);
            if (DragIconUI.Instance != null) DragIconUI.Instance.Show(icon.sprite, e);
            return;
        }

        // the Off Hand ghosting a two-handed Main Hand has nothing of its own to drag,
        // and a slot showing an armed spell's click isn't showing its item
        if (!handSlot || overridden || loadout == null || !hasItem || loadout.Drawn(side) == null)
        {
            e.pointerDrag = null;
            return;
        }
        dragging = true;
        dragSet = loadout.ActiveSet;
        BagDrag.BeginLoadout(loadout, dragSet, side);
        if (DragIconUI.Instance != null) DragIconUI.Instance.Show(icon.sprite, e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragging && DragIconUI.Instance != null) DragIconUI.Instance.Move(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        if (DragIconUI.Instance != null) DragIconUI.Instance.Hide();

        // a spell nobody took is taken off its slot
        bool claimed = BagDrag.End();
        if (!handSlot)
        {
            if (!claimed && skillSlots != null)
            {
                skillSlots.Clear(skillIndex);
                UiSfx.Play(UiSound.Return, 1f, 0.85f);   // taken off the slot
            }
            return;
        }

        // unclaimed and released over no UI: dropped into the world under the cursor
        bool intoWorld = e.pointerCurrentRaycast.gameObject == null;
        if (!claimed) UiDrop.Unclaimed(intoWorld);
        if (!claimed && intoWorld && loadout.TryGetComponent(out Inventory inv))
            inv.DropLoadoutAt(dragSet, side, inv.WorldDropPoint());
    }

    public void SetCooldown(float fill01)
    {
        if (overridden) fill01 = 0f;
        cooldown.enabled = fill01 > 0f;
        cooldown.fillAmount = fill01;
    }

    void Update()
    {
        // a weapon destroyed while slotted leaves a dead Unity object behind the
        // interface (interface == null can't see that), so test it as an Object
        if (skill is Object o && o == null) { SetSkill(null); return; }
        if (skill == null) return;
        cooldown.fillAmount = skill.Cooldown01;

        // castable changes with the hands and their locks (a thrown axe), so poll it
        bool castable = skillSlots == null || skillSlots.TryGetCaster(skill, out HandSide _);
        icon.color = new Color(1f, 1f, 1f, castable ? 1f : uncastableAlpha);
    }
}

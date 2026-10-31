// The thing being dragged around the HUD, if any: a bag slot (BagSlotUI), a loadout
// item (LoadoutSlotUI on the Player tab, the action bar's Main / Off Hand slots) or a
// learned spell (a skill tree's SkillNodeUI, an action bar skill slot). Drop targets
// claim the drop here in their OnDrop, which runs before the dragged box's
// OnEndDrag: Claim takes a bag slot, ClaimLoadout a loadout item, ClaimSkill a spell,
// each leaving the other kinds alone. An item nobody claimed and released over no UI
// at all goes into the world at the aimed point (Inventory.DropAt / DropLoadoutAt);
// a spell dragged off its skill slot that nobody claims leaves the slot empty.
public static class BagDrag
{
    static Inventory inventory;
    static int slot = -1;
    static Loadout loadout;
    static int set;
    static HandSide side;
    static IActiveSkill skill;
    static int skillSlot = -1;
    static bool claimed;

    public static bool Active => inventory != null || loadout != null || skill != null;

    public static void Begin(Inventory inv, int index)
    {
        End();
        inventory = inv;
        slot = index;
    }

    public static void BeginLoadout(Loadout from, int fromSet, HandSide fromSide)
    {
        End();
        loadout = from;
        set = fromSet;
        side = fromSide;
    }

    // A learned spell, from a skill slot (`fromSlot`) or a skill tree (-1).
    public static void BeginSkill(IActiveSkill dragged, int fromSlot)
    {
        End();
        skill = dragged;
        skillSlot = fromSlot;
    }

    // A target takes a dragged spell: the spell and the skill slot it came from (-1
    // from a tree).
    public static bool ClaimSkill(out IActiveSkill dragged, out int fromSlot)
    {
        dragged = skill;
        fromSlot = skillSlot;
        if (skill == null || claimed) return false;
        claimed = true;
        return true;
    }

    // A target takes a dragged bag slot: the inventory and slot to act on.
    public static bool Claim(out Inventory inv, out int index)
    {
        inv = inventory;
        index = slot;
        if (inventory == null || claimed) return false;
        claimed = true;
        return true;
    }

    // A target takes a dragged loadout item: the loadout slot it came from.
    public static bool ClaimLoadout(out Loadout from, out int fromSet, out HandSide fromSide)
    {
        from = loadout;
        fromSet = set;
        fromSide = side;
        if (loadout == null || claimed) return false;
        claimed = true;
        return true;
    }

    // Ends the drag; true when a target claimed it.
    public static bool End()
    {
        bool wasClaimed = claimed;
        inventory = null;
        slot = -1;
        loadout = null;
        skill = null;
        skillSlot = -1;
        claimed = false;
        return wasClaimed;
    }
}

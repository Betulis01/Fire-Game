// The bag slot being dragged out of the Bag tab, if any. Drop targets (another bag
// slot, a hand slot, an open station menu) claim the drop here in their OnDrop, which
// runs before the dragged slot's OnEndDrag; a drag nobody claimed and released over
// no UI at all goes into the world (BagSlotUI).
public static class BagDrag
{
    static Inventory inventory;
    static int slot = -1;
    static bool claimed;

    public static bool Active => inventory != null;

    public static void Begin(Inventory inv, int index)
    {
        inventory = inv;
        slot = index;
        claimed = false;
    }

    // A target takes the drop: the inventory and slot to act on.
    public static bool Claim(out Inventory inv, out int index)
    {
        inv = inventory;
        index = slot;
        if (!Active || claimed) return false;
        claimed = true;
        return true;
    }

    // Ends the drag; true when a target claimed it.
    public static bool End()
    {
        bool wasClaimed = claimed;
        inventory = null;
        slot = -1;
        claimed = false;
        return wasClaimed;
    }
}

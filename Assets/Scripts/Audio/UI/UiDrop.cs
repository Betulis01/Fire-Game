using UnityEngine;

// What a HUD drop sounds like, decided before it happens (the drop targets ask): a bag
// stack moved onto an empty slot or merged onto its own kind is a place, onto something
// else a swap, onto a full stack of its own kind nothing happens -- denied; fuel dragged
// onto a torch is that torch's feed (from its FireAudio's set, since a torch in the bag is
// inactive and can't play it itself), or denied when it's already full.
public static class UiDrop
{
    public static UiSound MoveSound(Inventory inv, int from, int to)
    {
        if (from == to) return UiSound.Return;   // dropped back where it was
        GameObject a = inv.Item(from), b = inv.Item(to);
        if (b == null) return UiSound.Place;
        if (Def(a) != Def(b) || !b.TryGetComponent(out Stackable stack)) return UiSound.Swap;
        return inv.Count(to) < stack.maxStack ? UiSound.Place : UiSound.Denied;
    }

    // After Inventory.TryFeed accepted the drop: `before` / `after` are the dragged slot's
    // count either side of it, `fuel` what was dragged, `target` the item fed.
    public static void Fed(int before, int after, ItemDefinition fuel, GameObject target)
    {
        if (after >= before) { UiSfx.Play(UiSound.Denied); return; }   // full: nothing went in
        if (target == null || !target.TryGetComponent(out FireAudio fire) || fire.sounds == null) return;
        if (fire.isActiveAndEnabled) return;   // a drawn torch plays its own feed (Fuel.FuelAdded)
        FuelType? type = fuel != null && fuel.prefab != null && fuel.prefab.TryGetComponent(out Burnable b) ? b.fuelType : null;
        Sfx.Play2D(fire.sounds.FeedFor(type));
    }

    // A drag nobody took: dropped into the world (over no UI) or snapped back.
    public static void Unclaimed(bool intoWorld) => UiSfx.Play(intoWorld ? UiSound.Toss : UiSound.Return);

    public static ItemDefinition Def(GameObject item) =>
        item != null && item.TryGetComponent(out WorldItem wi) ? wi.item : null;
}

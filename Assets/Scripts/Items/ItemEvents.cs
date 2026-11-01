using System;
using UnityEngine;

// Items moving between the world and the player, as plain facts for the audio layer
// (ItemAudioDirector): raised where the move happens, heard by whoever cares. Items are
// passed as their ItemDefinition and a position, since the object may be gone by then
// (an auto-pickup destroys the world item it merges into a stack).
public static class ItemEvents
{
    // Taken from the world: an E press (`auto` false) or walking over a stack the bag
    // tops up (`auto` true).
    public static event Action<ItemDefinition, Vector3, bool> PickedUp;
    // Come to rest in the world: dropped, put down, spilled out of the bag, or a throw
    // landing outside any receiver.
    public static event Action<ItemDefinition, Vector3> Landed;
    // A held-Q throw leaving the hand.
    public static event Action<ItemDefinition, Vector3> Thrown;
    // A bag stack tipped out into the world, `count` items around `pos`.
    public static event Action<ItemDefinition, Vector3, int> Spilled;
    // Gear onto / off the body, a tool or weapon into a loadout slot, food eaten.
    public static event Action<GameObject> Worn;
    public static event Action<GameObject> Removed;
    public static event Action<GameObject> Wielded;
    public static event Action<GameObject> Eaten;

    internal static void RaisePickedUp(ItemDefinition item, Vector3 at, bool auto) => PickedUp?.Invoke(item, at, auto);
    internal static void RaiseLanded(GameObject item) { if (item != null) Landed?.Invoke(Def(item), item.transform.position); }
    internal static void RaiseThrown(GameObject item) { if (item != null) Thrown?.Invoke(Def(item), item.transform.position); }
    internal static void RaiseSpilled(ItemDefinition item, Vector3 at, int count) => Spilled?.Invoke(item, at, count);
    internal static void RaiseWorn(GameObject item) => Worn?.Invoke(item);
    internal static void RaiseRemoved(GameObject item) => Removed?.Invoke(item);
    internal static void RaiseWielded(GameObject item) => Wielded?.Invoke(item);
    internal static void RaiseEaten(GameObject item) => Eaten?.Invoke(item);

    static ItemDefinition Def(GameObject item) => item.TryGetComponent(out WorldItem wi) ? wi.item : null;
}

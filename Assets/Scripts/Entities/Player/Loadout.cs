using System;
using UnityEngine;

// The player's weapon loadouts: two sets (A, B), each with a Main Hand slot (left
// click) and an Off Hand slot (right click) for a tool, weapon, shield or torch.
// Only the active set is in use: Hands draws it on the hand anchors while the arms
// are free, and stows the whole loadout (inactive, so a torch gives no light or
// warmth) while they carry something. Scroll up / down picks set A / B and Tab
// toggles, but never mid-swing; a swap takes a short beat (swapTime) in which
// neither hand attacks, blocks or casts. Every loadout item is kept "held" (pickup
// collider off) as a child of the player, the same trick Equipment uses for worn
// gear; Hands decides which of them are active and where they hang.
//
// A two-handed tool (the bow, the pickaxe: Tool.twoHanded) fills a whole set: it's stored in the
// Main Hand and the Off Hand stays empty -- equipping one displaces the Off Hand's
// item, and equipping an Off Hand displaces it.
[RequireComponent(typeof(Hands))]
public class Loadout : MonoBehaviour
{
    public const int Sets = 2;

    [Tooltip("Seconds a set swap takes; neither hand can attack, block or cast meanwhile.")]
    [Min(0f)] public float swapTime = 0.3f;

    readonly GameObject[,] items = new GameObject[Sets, 2];
    WeaponUse weaponUse;
    PlayerAnimator animator;
    float swapStart = float.NegativeInfinity;

    public int ActiveSet { get; private set; }

    // raised after anything moves in or out, or the active set changes
    public event Action Changed;

    void Awake()
    {
        weaponUse = GetComponent<WeaponUse>();
        animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        // the wheel picks a set rather than toggling, so a smooth-scrolling touchpad
        // sending many events can't flicker between them
        int scroll = UserInput.Instance.HandScrollStep;
        if (scroll != 0) SetActiveSet(scroll > 0 ? 0 : 1);
        if (UserInput.Instance.SwitchHand) SetActiveSet(1 - ActiveSet);
    }

    // What a loadout slot takes: something to swing or block with.
    public static bool Fits(GameObject item) =>
        item != null && (item.TryGetComponent(out Tool _) || item.TryGetComponent(out Shield _));

    public static bool IsTwoHanded(GameObject item) =>
        item != null && item.TryGetComponent(out Tool tool) && tool.twoHanded;

    public GameObject Item(int set, HandSide side) => items[set, (int)side];

    public bool MainIsTwoHanded(int set) => IsTwoHanded(items[set, (int)HandSide.Left]);

    // The active set's item for a hand (drawn while the hands are free). A two-handed
    // tool keeps its set's Off Hand empty.
    public GameObject Drawn(HandSide side) => items[ActiveSet, (int)side];

    // A swing in progress owns the drawn weapons until it's over.
    public bool Busy => (weaponUse != null && weaponUse.IsSwingArmed) || (animator != null && animator.IsAttacking);

    // 1 right after a swap, draining to 0 over swapTime.
    public float SwapProgress01 => swapTime > 0f ? 1f - Mathf.Clamp01((Time.time - swapStart) / swapTime) : 0f;
    public bool IsSwapping => SwapProgress01 > 0f;

    public bool SetActiveSet(int set)
    {
        set = Mathf.Clamp(set, 0, Sets - 1);
        if (set == ActiveSet || Busy) return false;
        ActiveSet = set;
        swapStart = Time.time;
        Changed?.Invoke();
        return true;
    }

    // Put `item` (or null, to empty the slot) into a slot. `previous` is what was
    // there and `displaced` whatever the two-handed rule pushes out of the set's other
    // slot; the caller decides where both go. A two-handed item always lands in the
    // Main Hand. Refused for the drawn set mid-swing.
    public bool TryPut(GameObject item, int set, HandSide side, out GameObject previous, out GameObject displaced)
    {
        displaced = null;
        if (IsTwoHanded(item)) side = HandSide.Left;
        previous = items[set, (int)side];
        if (item != null && !Fits(item)) return false;
        if (set == ActiveSet && Busy) return false;

        HandSide other = side == HandSide.Left ? HandSide.Right : HandSide.Left;
        if (item != null && (IsTwoHanded(item) || (side == HandSide.Right && MainIsTwoHanded(set))))
        {
            displaced = items[set, (int)other];
            items[set, (int)other] = null;
        }

        if (item != null)
        {
            item.transform.SetParent(transform, false);
            item.GetComponent<WorldItem>().SetHeld(true);
            item.SetActive(false);   // Hands activates it if it's drawn
        }
        items[set, (int)side] = item;
        Changed?.Invoke();
        return true;
    }

    // Drag one loadout slot onto another (either set): the two swap. Refused when the
    // result breaks the two-handed rule (a two-handed tool dropped on an Off Hand moves
    // to its set's Main Hand if that's free), or for the drawn set mid-swing.
    public bool TryMove(int fromSet, HandSide fromSide, int toSet, HandSide toSide)
    {
        if (fromSet == toSet && fromSide == toSide) return false;
        if (items[fromSet, (int)fromSide] == null) return false;
        if ((fromSet == ActiveSet || toSet == ActiveSet) && Busy) return false;

        var next = (GameObject[,])items.Clone();
        (next[fromSet, (int)fromSide], next[toSet, (int)toSide]) = (next[toSet, (int)toSide], next[fromSet, (int)fromSide]);
        if (!Settle(next, fromSet) || !Settle(next, toSet)) return false;

        for (int s = 0; s < Sets; s++)
            for (int h = 0; h < 2; h++) items[s, h] = next[s, h];
        Changed?.Invoke();
        return true;
    }

    // A set's slots after a move: a two-handed tool sits in the Main Hand with nothing
    // beside it. False when the move can't be made to fit.
    static bool Settle(GameObject[,] slots, int set)
    {
        GameObject main = slots[set, (int)HandSide.Left];
        GameObject off = slots[set, (int)HandSide.Right];
        if (IsTwoHanded(off))
        {
            if (main != null) return false;
            slots[set, (int)HandSide.Left] = off;
            slots[set, (int)HandSide.Right] = null;
            return true;
        }
        return !IsTwoHanded(main) || off == null;
    }

    // How many items putting `item` into a slot would push out (previous + displaced),
    // so the caller can check there's room for them first.
    public int Outgoing(GameObject item, int set, HandSide side)
    {
        if (IsTwoHanded(item)) side = HandSide.Left;
        HandSide other = side == HandSide.Left ? HandSide.Right : HandSide.Left;
        int n = items[set, (int)side] != null ? 1 : 0;
        if (IsTwoHanded(item) || (side == HandSide.Right && MainIsTwoHanded(set)))
            n += items[set, (int)other] != null ? 1 : 0;
        return n;
    }

    // The active set's slot that `item` can take without pushing anything out.
    public bool TryGetFreeSlot(GameObject item, out HandSide side)
    {
        side = HandSide.Left;
        bool mainFree = Drawn(HandSide.Left) == null;
        bool offFree = Drawn(HandSide.Right) == null && !MainIsTwoHanded(ActiveSet);
        if (IsTwoHanded(item)) return mainFree && Drawn(HandSide.Right) == null;
        if (mainFree) return true;
        side = HandSide.Right;
        return offFree;
    }

    // The active set's slot a clicked bag item goes into: a free one if there is
    // one, else the Main Hand (swapping out what's there).
    public HandSide AutoSlot(GameObject item) => TryGetFreeSlot(item, out HandSide side) ? side : HandSide.Left;
}

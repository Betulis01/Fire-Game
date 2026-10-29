using System;
using UnityEngine;

public enum HandSide { Left, Right }

// The player's two hands. They carry Big items (logs, stone: ItemSize.Big), one per
// hand -- each an actual item GameObject parented to a body transform. Small items
// and gear go in the bag (Inventory) instead.
//
// While both hands are empty the player is armed: the Loadout's active set is drawn
// on the hand anchors (Main Hand on the left, Off Hand on the right) and that's what
// each hand swings, or its default fists when that loadout slot is empty (a
// two-handed tool in the Main Hand leaves the Off Hand with nothing). Carrying
// anything stows the whole loadout -- inactive, so a torch goes dark -- and the hands
// can't attack (ItemUser turns a left click into putting the load down).
[RequireComponent(typeof(Loadout))]
public class Hands : MonoBehaviour
{
    public Transform leftHand;    // empty child transforms on the player body
    public Transform rightHand;

    [Tooltip("Default bare-hands weapon (a Fists prefab with Tool + MeleeWeapon). One is " +
             "spawned into each hand and shown whenever that hand is armed with an empty " +
             "loadout slot.")]
    public GameObject fistsPrefab;

    [Tooltip("Source of the west-facing mirror flip. Falls back to a PlayerAnimator " +
             "on this GameObject.")]
    [SerializeField] PlayerAnimator playerAnimator;

    [Tooltip("How far the two halves of a load put down at once scatter around the spot.")]
    [SerializeField] float dropScatter = 0.15f;

    GameObject leftItem;
    GameObject rightItem;
    HandSide lastFilled;

    GameObject leftFists;
    GameObject rightFists;
    Loadout loadout;

    public Camera cam;

    // raised after any pick-up/drop, and when the drawn loadout changes, so the UI
    // can redraw
    public event Action Changed;

    void Awake()
    {
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        loadout = GetComponent<Loadout>();
        leftFists = SpawnFists(leftHand);
        rightFists = SpawnFists(rightHand);
    }

    void OnEnable()
    {
        loadout.Changed += OnLoadoutChanged;
        Refresh();
    }

    void OnDisable() => loadout.Changed -= OnLoadoutChanged;

    void OnLoadoutChanged()
    {
        Refresh();
        Changed?.Invoke();
    }

    public bool IsCarrying => leftItem != null || rightItem != null;

    public bool IsHolding(HandSide side) => Held(side) != null;

    // The carried (Big) item in a hand; never a loadout item.
    public GameObject Held(HandSide side) => side == HandSide.Left ? leftItem : rightItem;

    // The hand Q drops or deposits from: the one filled last, else the other.
    public bool TryGetLastFilled(out HandSide side)
    {
        side = IsHolding(lastFilled) ? lastFilled : Other(lastFilled);
        return IsHolding(side);
    }

    // What hangs on a hand right now: the carried item, else the drawn loadout item.
    public GameObject Shown(HandSide side)
    {
        if (IsCarrying) return Held(side);
        return loadout.Drawn(side);
    }

    // The thing a hand swings: its drawn loadout item, or its default fists when that
    // slot is empty; nothing while carrying, or for the Off Hand beside a two-handed
    // tool. Combat (WeaponUse) reads this so it never has to special-case being unarmed.
    public GameObject ActiveItem(HandSide side)
    {
        if (IsCarrying || !HasFreeOffHand(side)) return null;
        GameObject drawn = loadout.Drawn(side);
        if (drawn != null) return drawn;
        return side == HandSide.Left ? leftFists : rightFists;
    }

    // false for the Off Hand while the Main Hand's tool needs both hands
    bool HasFreeOffHand(HandSide side) => side == HandSide.Left || !loadout.MainIsTwoHanded(loadout.ActiveSet);

    // The physical transform a hand's item should render at right now. leftHand/
    // rightHand are fixed anchors, but which one visually reads as "left"/"right"
    // flips when the player mirrors to face west (PlayerAnimator.FlipX) -- so the
    // anchor for a side swaps too, and the item shown on it is re-parented onto it
    // here rather than staying stuck on whichever anchor it was attached to.
    // This holds mid-attack too: PlayerAnimator.PlayAttack flips the clip's _l/_r
    // suffix on the same mirror, so a west-facing left-hand swing plays the _r
    // clip, which animates rightHand -- exactly the anchor this swap puts the
    // left item on.
    public Transform Anchor(HandSide side)
    {
        Transform anchor = (side == HandSide.Right) != FlipX ? rightHand : leftHand;

        GameObject item = Shown(side);
        if (item != null && item.transform.parent != anchor) Attach(item, anchor, worldPositionStays: false);
        return anchor;
    }

    bool FlipX => playerAnimator != null && playerAnimator.FlipX;

    // Parent an item onto a hand anchor and match its sprite to the west-facing
    // mirror. The anchor's own mirror (PlayerAnimator.LateUpdate) only moves and
    // rotates the item, so asymmetric art (an axe head) needs flipX to read
    // mirrored too. FlipX only changes together with the anchor swap above, so
    // re-attaching is the one place the flip needs updating.
    void Attach(GameObject item, Transform anchor, bool worldPositionStays = true)
    {
        item.transform.SetParent(anchor, worldPositionStays);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        SetSpriteFlip(item, FlipX);
    }

    static void SetSpriteFlip(GameObject item, bool flip)
    {
        foreach (SpriteRenderer sr in item.GetComponentsInChildren<SpriteRenderer>(true))
            sr.flipX = flip;
    }

    // Move a world item onto the given hand; false when that hand is already full.
    public bool TryHold(GameObject worldItem, HandSide side)
    {
        if (worldItem == null || IsHolding(side)) return false;

        bool wasCarrying = IsCarrying;
        Set(side, worldItem);
        lastFilled = side;
        if (!wasCarrying) Refresh();   // stow the loadout off the anchors first

        Attach(worldItem, Anchor(side));
        worldItem.GetComponent<WorldItem>().SetHeld(true);
        Changed?.Invoke();
        return true;
    }

    // Into whichever hand is free (left first); false when both are full.
    public bool TryHoldFree(GameObject worldItem) =>
        TryHold(worldItem, HandSide.Left) || TryHold(worldItem, HandSide.Right);

    // Drop a hand's item at the player's feet, a little toward the aim.
    public void Drop(HandSide side)
    {
        Vector2 aim = UserInput.Instance.AimDirection(transform.position, cam);
        DropAt(side, transform.position + (Vector3)(aim * 0.5f));
    }

    // Drop a hand's item at an explicit world position (e.g. a ghost-aimed drop).
    public void DropAt(HandSide side, Vector3 dropPos)
    {
        GameObject item = Held(side);
        if (item == null) return;

        Place(item, dropPos);
        Set(side, null);
        Refresh();
        Changed?.Invoke();
    }

    // Take a hand's item out without putting it anywhere (a throw: ThrownItem takes
    // it from here). It keeps WorldItem.IsHeld until the caller changes that.
    public GameObject Release(HandSide side)
    {
        GameObject item = Held(side);
        if (item == null) return null;

        item.transform.SetParent(null, true);
        SetSpriteFlip(item, false);
        Set(side, null);
        Refresh();
        Changed?.Invoke();
        return item;
    }

    // Put the whole load down around `pos` (a left click while carrying), which
    // draws the loadout again.
    public void DropLoad(Vector3 pos)
    {
        if (!IsCarrying) return;
        foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
        {
            GameObject item = Held(side);
            if (item == null) continue;
            Place(item, pos + (Vector3)(UnityEngine.Random.insideUnitCircle * dropScatter));
            Set(side, null);
        }
        Refresh();
        Changed?.Invoke();
    }

    void Place(GameObject item, Vector3 pos)
    {
        item.transform.SetParent(null);
        item.transform.position = pos;
        SetSpriteFlip(item, false);
        item.GetComponent<WorldItem>().SetHeld(false);
    }

    // Use up a hand's item (a crafting input, a log fed to the fire): destroyed, and
    // the hand is free again. Hands hold one item each, so any amount takes it.
    public void Consume(HandSide side, int amount = 1)
    {
        if (amount > 0) Clear(side);
    }

    // destroy and remove the item in a hand without dropping it
    public void Clear(HandSide side)
    {
        GameObject item = Held(side);
        if (item == null) return;

        Destroy(item);
        Set(side, null);
        Refresh();
        Changed?.Invoke();
    }

    void Set(HandSide side, GameObject item)
    {
        if (side == HandSide.Left) leftItem = item;
        else rightItem = item;
    }

    GameObject SpawnFists(Transform hand)
    {
        if (fistsPrefab == null || hand == null) return null;
        GameObject f = Instantiate(fistsPrefab, hand);
        f.transform.localPosition = Vector3.zero;
        return f;
    }

    // Draw or stow the loadout. Armed: the active set's items hang on the anchors and
    // fists fill an empty slot. Carrying: every loadout item is stowed (an inactive
    // child of the player) and no fists show. The inactive set is always stowed.
    void Refresh()
    {
        bool carrying = IsCarrying;
        for (int set = 0; set < Loadout.Sets; set++)
            foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
            {
                GameObject item = loadout.Item(set, side);
                if (item == null) continue;

                if (!carrying && set == loadout.ActiveSet)
                {
                    item.SetActive(true);
                    Attach(item, Anchor(side), worldPositionStays: false);
                }
                else
                {
                    item.SetActive(false);
                    if (item.transform.parent != transform) item.transform.SetParent(transform, false);
                }
            }

        if (leftFists != null) leftFists.SetActive(!carrying && loadout.Drawn(HandSide.Left) == null);
        if (rightFists != null)
            rightFists.SetActive(!carrying && loadout.Drawn(HandSide.Right) == null && HasFreeOffHand(HandSide.Right));
    }

    static HandSide Other(HandSide side) => side == HandSide.Left ? HandSide.Right : HandSide.Left;
}

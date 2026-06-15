using System;
using UnityEngine;

public enum HandSide { Left, Right }

// The player's "inventory" is literally two hands. Each hand holds (or not) an
// actual item GameObject parented to a body transform. Picking up re-parents a
// WorldItem onto a hand; dropping re-parents it back into the world.
public class Hands : MonoBehaviour
{
    public Transform leftHand;    // empty child transforms on the player body
    public Transform rightHand;

    GameObject leftItem;
    GameObject rightItem;

    // raised after any pick-up/drop so the UI can redraw
    public event Action Changed;

    public bool IsHolding(HandSide side) => Held(side) != null;

    public GameObject Held(HandSide side) => side == HandSide.Left ? leftItem : rightItem;

    // move a world item onto the given hand; fails if that hand is already full
    public bool TryHold(GameObject worldItem, HandSide side)
    {
        if (worldItem == null || IsHolding(side)) return false;

        Transform hand = side == HandSide.Left ? leftHand : rightHand;
        worldItem.transform.SetParent(hand);
        worldItem.transform.localPosition = Vector3.zero;
        worldItem.GetComponent<WorldItem>().SetHeld(true);

        Set(side, worldItem);
        Changed?.Invoke();
        return true;
    }

    // swap the item in a hand for a new one (e.g. crafting wood -> torch).
    // overwrites whatever's there, destroying the old item.
    public void Replace(HandSide side, GameObject newItem)
    {
        GameObject old = Held(side);
        if (old != null) Destroy(old);

        Transform hand = side == HandSide.Left ? leftHand : rightHand;
        newItem.transform.SetParent(hand);
        newItem.transform.localPosition = Vector3.zero;
        newItem.GetComponent<WorldItem>().SetHeld(true);

        Set(side, newItem);
        Changed?.Invoke();
    }

    // drop the item in the given hand back into the world at the player's feet
    public void Drop(HandSide side)
    {
        GameObject item = Held(side);
        if (item == null) return;

        item.transform.SetParent(null);
        item.transform.position = transform.position;
        item.GetComponent<WorldItem>().SetHeld(false);

        Set(side, null);
        Changed?.Invoke();
    }

    // destroy and remove the item in a hand without dropping it (e.g. an input
    // consumed by crafting).
    public void Clear(HandSide side)
    {
        GameObject item = Held(side);
        if (item == null) return;

        Destroy(item);
        Set(side, null);
        Changed?.Invoke();
    }

    void Set(HandSide side, GameObject item)
    {
        if (side == HandSide.Left) leftItem = item;
        else rightItem = item;
    }
}

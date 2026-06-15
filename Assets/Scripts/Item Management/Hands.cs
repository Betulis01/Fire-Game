using System;
using UnityEngine;

public enum HandSide { Left, Right }

// The player's "inventory" is literally two hands. Each hand holds (or not) an
// actual item GameObject parented to a body transform, plus a stack count. Picking
// up re-parents a WorldItem onto a hand; a matching Stackable item merges into the
// hand's count instead. Stacking lives only here — world items are always single.
public class Hands : MonoBehaviour
{
    public Transform leftHand;    // empty child transforms on the player body
    public Transform rightHand;

    GameObject leftItem;
    GameObject rightItem;
    int leftCount;
    int rightCount;

    // raised after any pick-up/drop so the UI can redraw
    public event Action Changed;

    public bool IsHolding(HandSide side) => Held(side) != null;

    public GameObject Held(HandSide side) => side == HandSide.Left ? leftItem : rightItem;

    public int Count(HandSide side) => side == HandSide.Left ? leftCount : rightCount;

    // move a world item onto the given hand, or merge it into a matching stack
    public bool TryHold(GameObject worldItem, HandSide side)
    {
        if (worldItem == null) return false;

        GameObject current = Held(side);
        if (current != null)
        {
            // merge a single matching item into the hand's stack if there's room
            Stackable stack = current.GetComponent<Stackable>();
            WorldItem held = current.GetComponent<WorldItem>();
            WorldItem incoming = worldItem.GetComponent<WorldItem>();

            if (stack != null && incoming != null && held.item == incoming.item
                && Count(side) < stack.maxStack)
            {
                SetCount(side, Count(side) + 1);
                Destroy(worldItem);            // the world single is absorbed into the stack
                Changed?.Invoke();
                return true;
            }

            return false;                      // hand occupied and can't stack
        }

        Transform hand = side == HandSide.Left ? leftHand : rightHand;
        worldItem.transform.SetParent(hand);
        worldItem.transform.localPosition = Vector3.zero;
        worldItem.GetComponent<WorldItem>().SetHeld(true);

        Set(side, worldItem);
        SetCount(side, 1);
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
        SetCount(side, 1);
        Changed?.Invoke();
    }

    // drop one item from the given hand at the player's feet. A stack releases a
    // single world item and keeps the rest; the last one drops the held object.
    public void Drop(HandSide side)
    {
        GameObject item = Held(side);
        if (item == null) return;

        if (Count(side) > 1)
        {
            SetCount(side, Count(side) - 1);
            GameObject prefab = item.GetComponent<WorldItem>().item.prefab;
            GameObject one = Instantiate(prefab, transform.position, Quaternion.identity);
            one.GetComponent<WorldItem>().SetHeld(false);
            Changed?.Invoke();
            return;
        }

        item.transform.SetParent(null);
        item.transform.position = transform.position;
        item.GetComponent<WorldItem>().SetHeld(false);

        Set(side, null);
        SetCount(side, 0);
        Changed?.Invoke();
    }

    // remove `amount` from a hand's stack (e.g. a crafting input). Decrements the
    // count, freeing the hand only when it hits zero.
    public void Consume(HandSide side, int amount = 1)
    {
        if (Held(side) == null) return;

        int remaining = Count(side) - amount;
        if (remaining > 0)
        {
            SetCount(side, remaining);
            Changed?.Invoke();
        }
        else
        {
            Clear(side);   // destroys the held object and raises Changed
        }
    }

    // destroy and remove the item in a hand without dropping it (e.g. an input
    // consumed by crafting).
    public void Clear(HandSide side)
    {
        GameObject item = Held(side);
        if (item == null) return;

        Destroy(item);
        Set(side, null);
        SetCount(side, 0);
        Changed?.Invoke();
    }

    void Set(HandSide side, GameObject item)
    {
        if (side == HandSide.Left) leftItem = item;
        else rightItem = item;
    }

    void SetCount(HandSide side, int count)
    {
        if (side == HandSide.Left) leftCount = count;
        else rightCount = count;
    }
}

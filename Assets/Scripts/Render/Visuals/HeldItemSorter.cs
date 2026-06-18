using UnityEngine;

// Sorts items held in the hands in front of or behind the body based on facing.
// Held items are parented under the player's SortingGroup, so their sortingOrder is
// relative to the body sprite: above it draws in front, below draws behind. Facing
// north (away from the camera) puts items behind the body; every other direction
// puts them in front.
//
// Runs each frame so it tracks both facing changes and newly picked-up items. A
// held item's own world YSort is disabled while held (see WorldItem.SetHeld), so
// the two never fight over the same sortingOrder.
[RequireComponent(typeof(Hands))]
public class HeldItemSorter : MonoBehaviour
{
    [Tooltip("Source of facing. Falls back to a PlayerAnimator on this GameObject.")]
    [SerializeField] PlayerAnimator playerAnimator;

    [Tooltip("Body sprite the held items sort relative to (its sortingOrder is the " +
             "reference). Falls back to a SpriteRenderer on this GameObject.")]
    [SerializeField] SpriteRenderer body;

    [Tooltip("Order added to the body's when the item should draw in front.")]
    [SerializeField] int frontOffset = 1;

    [Tooltip("Order added to the body's when the item should draw behind (facing north).")]
    [SerializeField] int backOffset = -1;

    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        if (body == null) body = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        Apply(HandSide.Left);
        Apply(HandSide.Right);
    }

    void Apply(HandSide side)
    {
        GameObject item = hands.Held(side);
        if (item == null) return;

        SpriteRenderer sr = item.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        int order = body.sortingOrder + (IsFront(side) ? frontOffset : backOffset);
        sr.sortingOrder = order;
    }

    // Whether the item in this hand should draw in front of the body for the current
    // facing. South: both front. North: both behind. Side-on: the leading hand is in
    // front (right when facing east, left when facing west), the trailing hand behind.
    bool IsFront(HandSide side)
    {
        Vector2 f = playerAnimator.Facing;

        // Same cardinal resolution as PlayerAnimator: horizontal wins ties.
        if (Mathf.Abs(f.x) >= Mathf.Abs(f.y))
        {
            bool west = f.x < 0f;
            return west ? side == HandSide.Left : side == HandSide.Right;
        }

        return f.y < 0f;   // south = facing camera = front; north = behind
    }
}

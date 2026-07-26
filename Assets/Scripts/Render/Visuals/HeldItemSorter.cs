using UnityEngine;

// Sorts items held in the hands in front of or behind the body based on facing.
// Held items are parented under the player's SortingGroup, so their sortingOrder is
// relative to the body sprite: above it draws in front, below draws behind. Facing
// NE (away from the camera) puts items behind the body; facing SE (toward the
// camera) puts them in front. Both hands always match — there's no side-on facing
// left in the 4-direction isometric scheme to justify treating them differently.
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

    [Tooltip("Order added to the body's when the item should draw behind (facing NE).")]
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
        bool front = CardinalDir.Resolve(playerAnimator.Facing).dir == "se";
        Apply(HandSide.Left, front);
        Apply(HandSide.Right, front);
    }

    void Apply(HandSide side, bool front)
    {
        GameObject item = hands.Held(side);
        if (item == null) return;

        SpriteRenderer sr = item.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        int order = body.sortingOrder + (front ? frontOffset : backOffset);
        sr.sortingOrder = order;
    }
}

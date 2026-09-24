using UnityEngine;

// Sorts items held in the hands in front of or behind the body. Held items are
// parented under the player's SortingGroup, so their sortingOrder is relative to
// the body sprite: above it draws in front, below draws behind.
//
// Front/back is decided per physical anchor (LeftHand/RightHand) and art direction
// (NE/SE), not per hand: in the NE art one arm is on the near side of the body and
// the other behind it. NW/SW reuse the NE/SE art mirrored and Hands.Anchor swaps
// the anchors on the same mirror, so the same table covers them.
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

    [Tooltip("Order added to the body's when the item should draw behind.")]
    [SerializeField] int backOffset = -1;

    [Header("In front of the body? (per anchor, NE/SE art; NW/SW mirror these)")]
    [SerializeField] bool seLeftHandFront = true;
    [SerializeField] bool seRightHandFront = true;
    [SerializeField] bool neLeftHandFront = true;
    [SerializeField] bool neRightHandFront = false;

    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        if (body == null) body = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        bool se = CardinalDir.Resolve(playerAnimator.Facing).dir == "se";
        Apply(HandSide.Left, se);
        Apply(HandSide.Right, se);
    }

    void Apply(HandSide side, bool se)
    {
        GameObject item = hands.Held(side);
        if (item == null) return;

        bool onLeftAnchor = hands.Anchor(side) == hands.leftHand;
        bool front = se
            ? (onLeftAnchor ? seLeftHandFront : seRightHandFront)
            : (onLeftAnchor ? neLeftHandFront : neRightHandFront);
        int order = body.sortingOrder + (front ? frontOffset : backOffset);

        // Shift every sprite in the item by the same amount, so multi-sprite items
        // (e.g. a torch's flame) keep their internal layering. The first renderer
        // is the reference; once it's at `order` the shift is 0.
        SpriteRenderer[] srs = item.GetComponentsInChildren<SpriteRenderer>();
        if (srs.Length == 0) return;
        int shift = order - srs[0].sortingOrder;
        if (shift == 0) return;
        foreach (SpriteRenderer sr in srs) sr.sortingOrder += shift;
    }
}

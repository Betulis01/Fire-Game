using UnityEngine;

// Held items are parented onto the hand rig's BackHand/FrontHand transforms, which
// have their rotation keyframed directly in the animation clips (idle sway, attack
// swings). That rotation would otherwise apply to every held item uniformly. This
// filters it by capability: weapons (anything with a Tool component, including bare
// fists) follow the hand's swing/sway; anything else (torches, etc.) is counter-
// rotated every frame so it stays visually upright regardless of the hand's pose.
[RequireComponent(typeof(Hands))]
public class HeldItemRotationFilter : MonoBehaviour
{
    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
    }

    void LateUpdate()
    {
        Apply(HandSide.Left, hands.leftHand);
        Apply(HandSide.Right, hands.rightHand);
    }

    void Apply(HandSide side, Transform hand)
    {
        GameObject item = hands.Held(side);
        if (item == null) return;

        item.transform.localRotation = item.GetComponent<Tool>() != null
            ? Quaternion.identity
            : Quaternion.Inverse(hand.localRotation);
    }
}

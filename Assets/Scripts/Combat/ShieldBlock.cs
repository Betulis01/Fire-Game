using UnityEngine;

// Right-click blocking with a Shield held in either hand: raises the hand rig
// (visually lifting the shield in front of the body) and reduces incoming damage
// while held. IsBlocking/DamageReduction are read by Health.OnHit, the same way it
// already reads ToolDamageFilter -- a per-hit gate, not a mutation of HitInfo
// itself (HitInfo/AttackData are immutable).
[RequireComponent(typeof(Hands))]
public class ShieldBlock : MonoBehaviour
{
    [Tooltip("Hand rig raised while blocking. Falls back to a PlayerAnimator on this GameObject.")]
    [SerializeField] PlayerAnimator playerAnimator;

    Hands hands;
    Vector3 restHandRigPos;
    Shield activeShield;

    public bool IsBlocking { get; private set; }
    public float DamageReduction => activeShield != null ? activeShield.blockReduction : 0f;

    void Awake()
    {
        hands = GetComponent<Hands>();
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
        if (playerAnimator != null && playerAnimator.handRig != null)
            restHandRigPos = playerAnimator.handRig.localPosition;
    }

    void Update()
    {
        activeShield = FindShield();
        IsBlocking = activeShield != null && UserInput.Instance.BlockHeld;

        if (playerAnimator == null || playerAnimator.handRig == null) return;
        Vector3 offset = IsBlocking ? (Vector3)activeShield.raiseOffset : Vector3.zero;
        playerAnimator.handRig.localPosition = restHandRigPos + offset;
    }

    // Either hand -- a shield in the off-hand should still block while the other
    // hand swings a weapon.
    Shield FindShield()
    {
        GameObject left = hands.Held(HandSide.Left);
        if (left != null && left.TryGetComponent(out Shield leftShield)) return leftShield;

        GameObject right = hands.Held(HandSide.Right);
        if (right != null && right.TryGetComponent(out Shield rightShield)) return rightShield;

        return null;
    }
}

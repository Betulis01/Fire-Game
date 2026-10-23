using UnityEngine;

// Blocking with a Shield: holding the Use button of the hand that carries it (LMB
// = left, RMB = right) raises the hand rig
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
        activeShield = FindBlockingShield();
        IsBlocking = activeShield != null;

        if (playerAnimator == null || playerAnimator.handRig == null) return;
        Vector3 offset = IsBlocking ? (Vector3)activeShield.raiseOffset : Vector3.zero;
        playerAnimator.handRig.localPosition = restHandRigPos + offset;
    }

    // The shield whose hand's Use button is held, if any. Each hand blocks on its
    // own button, so the other hand can still swing a weapon meanwhile.
    Shield FindBlockingShield()
    {
        foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
        {
            GameObject item = hands.Held(side);
            if (item != null && item.TryGetComponent(out Shield shield) && UserInput.Instance.UseHeld(side))
                return shield;
        }
        return null;
    }
}

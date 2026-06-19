using UnityEngine;

// "Use the item in a hand" via the mouse: left button uses the left hand, right
// button the right. A hand swings whatever is "in" it — a held weapon, or its
// default fists when empty (Hands.ActiveItem hides that distinction). Both are just
// GameObjects with a Tool (stats) and Hitbox (strike region); a hand holding a
// non-weapon (e.g. wood) can't swing. The swing plays an attack animation; the hit
// lands when that clip's Animation Event calls OnAttackHit. Swing rate is limited
// per hand by the weapon's swingSpeed.
[RequireComponent(typeof(Hands))]
public class ToolUser : MonoBehaviour
{
    [Tooltip("Camera used to turn the mouse position into a world aim point. " +
             "Defaults to Camera.main if left empty.")]
    public Camera cam;

    [Tooltip("Point swings originate from (aim direction and strike center). " +
             "Place at the character's center; with a bottom pivot the transform " +
             "itself sits at the feet. Falls back to this transform if unset.")]
    public Transform aimOrigin;

    [Tooltip("Drives the attack animation. Each *_attack clip needs an Animation " +
             "Event at its contact frame calling OnAttackHit. Defaults to one here.")]
    public PlayerAnimator animator;

    Hands hands;
    float leftReadyAt;
    float rightReadyAt;

    // The swing whose contact frame we're waiting on. Only one at a time: the
    // player's single Animator layer can play just one attack clip, so a later
    // swing supersedes an earlier one that hasn't landed yet.
    struct Swing { public Hitbox hitbox; public AttackData attack; public float range; public bool armed; }
    Swing pending;

    // World point swings originate from (aim direction + strike center). Uses the
    // assigned aimOrigin, falling back to this transform if none is set.
    public Vector2 Origin => aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;

    void Awake()
    {
        hands = GetComponent<Hands>();
        if (cam == null) cam = Camera.main;
        if (animator == null) animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        if (UserInput.Instance.AttackLeft) UseHand(HandSide.Left);
        if (UserInput.Instance.AttackRight) UseHand(HandSide.Right);
    }

    void UseHand(HandSide side)
    {
        if (Time.time < ReadyAt(side)) return;

        // Swing whatever the hand offers (held weapon or default fists). It must be a
        // weapon: a Tool for stats and a Hitbox for the strike. Holding a non-weapon
        // (e.g. wood) yields no swing.
        GameObject weapon = hands.ActiveItem(side);
        if (weapon == null) return;
        if (!weapon.TryGetComponent(out Tool tool) || !weapon.TryGetComponent(out Hitbox hitbox)) return;

        // Arm the strike; the hit lands when the clip's Animation Event fires.
        pending = new Swing { hitbox = hitbox, attack = tool.Attack, range = tool.range, armed = true };

        float lockDuration = 1f / Mathf.Max(0.01f, tool.swingSpeed);
        if (animator != null) animator.PlayAttack(side, lockDuration);
        SetReadyAt(side, Time.time + lockDuration);
    }

    // Called by an Animation Event on each *_attack clip, at the contact frame.
    // Lands the armed swing where the player is currently aiming.
    public void OnAttackHit()
    {
        if (!pending.armed || pending.hitbox == null) return;

        Vector2 origin = Origin;
        Vector2 center = origin + AimDirection(origin) * pending.range;
        pending.hitbox.Strike(pending.attack, gameObject, center);
        pending.armed = false;
    }

    // direction the swing aims: toward the mouse cursor, or the gamepad aim stick
    Vector2 AimDirection(Vector2 origin)
    {
        if (cam == null) cam = Camera.main;
        return UserInput.Instance.AimDirection(origin, cam);
    }

    float ReadyAt(HandSide side) => side == HandSide.Left ? leftReadyAt : rightReadyAt;

    void SetReadyAt(HandSide side, float time)
    {
        if (side == HandSide.Left) leftReadyAt = time;
        else rightReadyAt = time;
    }
}

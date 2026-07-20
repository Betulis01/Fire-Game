using UnityEngine;

// "Use the item in a hand" via a single attack button. The attack goes to the
// player's selected hand (PlayerInteractor.ActiveHand, the 1/2 keys), except when
// both hands hold melee weapons of the same class — then swings alternate
// (ResolveAttackHand). A hand swings whatever is "in" it — a held weapon, or its
// default fists when empty (Hands.ActiveItem hides that distinction). Each is a
// GameObject with a Tool (stats) plus either a Hitbox (melee strike region) or a
// RangedWeapon (fires a projectile); a hand holding neither (e.g. wood) can't
// swing. The swing plays an attack animation; the hit/shot lands when that clip's
// Animation Event calls OnAttackHit. Swing rate is limited per hand by the
// weapon's swingSpeed.
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

    AttackLunge lunge;
    Hands hands;
    PlayerInteractor interactor;   // source of the selected hand (1/2 keys)
    float leftReadyAt;
    float rightReadyAt;

    // Dual-wield alternation: when both hands hold a weapon, swings alternate hands
    // each time one actually lands (see UseHand) — starting from the right.
    HandSide lastHandUsed = HandSide.Right;

    // The swing whose contact frame we're waiting on. Only one at a time: the
    // player's single Animator layer can play just one attack clip, so a later
    // swing supersedes an earlier one that hasn't landed yet. Exactly one of
    // hitbox/ranged is set, depending on which the held item offered.
    struct Swing { public Tool tool; public HandSide side; public Hitbox hitbox; public RangedWeapon ranged; public AttackData attack; public float range; public bool armed; }
    Swing pending;

    // World point swings originate from (aim direction + strike center). Uses the
    // assigned aimOrigin, falling back to this transform if none is set.
    public Vector2 Origin => aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;

    void Awake()
    {
        hands = GetComponent<Hands>();
        interactor = GetComponent<PlayerInteractor>();
        lunge = GetComponent<AttackLunge>();
        if (cam == null) cam = Camera.main;
        if (animator == null) animator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        if (UserInput.Instance.Attack) UseHand(ResolveAttackHand());
    }

    // A hand "wields melee" if what it offers has a Tool (stats) and a Hitbox
    // (melee strike region) but no RangedWeapon — fists count, since the fists
    // prefab has Tool + Hitbox.
    bool IsMelee(HandSide side)
    {
        GameObject item = hands.ActiveItem(side);
        if (item == null || !item.TryGetComponent<Tool>(out _)) return false;
        return item.TryGetComponent<Hitbox>(out _) && !item.TryGetComponent<RangedWeapon>(out _);
    }

    // The attack button uses the selected hand (PlayerInteractor.ActiveHand) —
    // whatever it holds, and nothing happens if that's not a weapon. The one
    // exception is dual-wield: when both hands wield melee of the same class
    // (two real held weapons, or two bare fists), swings alternate hands every
    // time one lands, starting from the right. A mixed pair (weapon + fist) does
    // not alternate. Side-effect-free (only UseHand mutates the alternation
    // state), so UI like AimIndicator can poll it every frame to preview the
    // next attack's hand.
    public HandSide ResolveAttackHand()
    {
        bool leftReal = hands.Held(HandSide.Left) != null;
        bool rightReal = hands.Held(HandSide.Right) != null;

        bool dualWield = IsMelee(HandSide.Left) && IsMelee(HandSide.Right)
            && leftReal == rightReal;

        if (dualWield)
            return lastHandUsed == HandSide.Right ? HandSide.Left : HandSide.Right;

        return interactor != null ? interactor.ActiveHand : HandSide.Left;
    }

    void UseHand(HandSide side)
    {
        if (Time.time < ReadyAt(side)) return;
        if (animator != null && animator.IsAttacking) return;

        // Swing whatever the hand offers (held weapon or default fists). It must be a
        // weapon: a Tool for stats plus either a Hitbox (melee) or a RangedWeapon
        // (fires a projectile). Holding a non-weapon (e.g. wood) yields no swing.
        GameObject weapon = hands.ActiveItem(side);
        if (weapon == null || !weapon.TryGetComponent(out Tool tool)) return;
        weapon.TryGetComponent(out Hitbox hitbox);
        weapon.TryGetComponent(out RangedWeapon ranged);
        if (hitbox == null && ranged == null) return;

        lastHandUsed = side;

        // Arm the strike/shot; it lands when the clip's Animation Event fires.
        pending = new Swing { tool = tool, side = side, hitbox = hitbox, ranged = ranged, attack = tool.Attack, range = tool.range, armed = true };

        float lockDuration = 1f / Mathf.Max(0.01f, tool.swingSpeed);
        // Same aim source OnAttackHit uses to land the hit, so the swing animation
        // always faces the direction the hit will actually land in.
        if (animator != null) animator.PlayAttack(side, lockDuration, AimDirection(Origin));
        SetReadyAt(side, Time.time + lockDuration);
    }

    // Called by an Animation Event on each *_attack clip, at the swing's start
    // frame — OnAttackHit's visual counterpart. Spawns the weapon's swing VFX
    // aimed where the strike will land; leaves the swing armed for the hit.
    public void OnAttackSwing()
    {
        if (!pending.armed || pending.tool == null) return;
        Vector2 dir = AimDirection(Origin);
        // Left-hand swings sweep the opposite way (mirrorSweep) for directional art;
        // the effect follows the swing origin so it moves with the player.
        pending.tool.SpawnSwingEffect(Origin, dir, pending.side == HandSide.Left,
                                      aimOrigin != null ? aimOrigin : transform);

        // Push fires here, at the windup, not on the later hit frame.
        if (lunge != null && lunge.Begin(dir, pending.tool.lungeSpeed, pending.tool.lungeDuration, pending.tool.lungeCurve))
            HitResolution.NotifySwing(gameObject);
    }

    // Called by an Animation Event on each *_attack clip, at the contact frame.
    // Lands the armed swing/shot where the player is currently aiming.
    public void OnAttackHit()
    {
        if (!pending.armed) return;

        Vector2 origin = Origin;
        Vector2 dir = AimDirection(origin);

        if (pending.hitbox != null)
        {
            pending.hitbox.Strike(pending.attack, gameObject, origin + dir * pending.range);
        }
        else if (pending.ranged != null)
        {
            RangedWeapon ranged = pending.ranged;
            Projectile arrow = Instantiate(ranged.projectilePrefab, origin + dir * ranged.spawnOffset, Quaternion.identity);
            arrow.Launch(pending.attack, gameObject, dir, ranged.projectileSpeed);
        }
        else return;

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

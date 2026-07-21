using UnityEngine;

// Weapon-swing capability: arms and resolves a melee/ranged attack for a given
// hand. ItemUser decides whether a Use press reaches this (vs FoodUse); once it
// does, this owns everything about the swing itself -- dual-wield hand
// resolution, charging, animation events, landing the hit. A hand swings
// whatever is "in" it -- a held weapon, or its default fists when empty
// (Hands.ActiveItem hides that distinction). Each is a GameObject with a Tool
// (stats) plus either a Hitbox (melee strike region) or a RangedWeapon (fires a
// projectile); a hand holding neither (e.g. wood) can't swing. The swing plays
// an attack animation; the hit/shot lands when that clip's Animation Event
// calls OnAttackHit. Swing rate is gated by the attack clip itself finishing
// (animator.IsAttacking), not a weapon stat.
[RequireComponent(typeof(Hands))]
public class WeaponUse : MonoBehaviour
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

    // Dual-wield alternation: when both hands hold a weapon, swings alternate hands
    // each time one actually lands (see TryUse) -- starting from the right.
    HandSide lastHandUsed = HandSide.Right;

    // The swing whose contact frame we're waiting on. Only one at a time: the
    // player's single Animator layer can play just one attack clip, so a later
    // swing supersedes an earlier one that hasn't landed yet. Exactly one of
    // hitbox/ranged is set, depending on which the held item offered. `heavy` is
    // resolved once the charge decides (see charging below), not at arm time.
    struct Swing { public Tool tool; public HandSide side; public Hitbox hitbox; public RangedWeapon ranged; public float range; public bool armed; public bool heavy; public float chargeFraction; }
    Swing pending;

    // Charging: the button was pressed and the swing is waiting on release to find out
    // whether it resolves light or heavy -- that's decided purely by elapsed hold time
    // vs the tool's chargeTime (see Update), independent of the animation. chargePaused
    // is separate, purely visual bookkeeping: whether the clip is actually frozen right
    // now (true once OnAttackChargeReady has fired with the button still held). Holding
    // is indefinite either way -- no auto-release, only release ends a charge.
    bool charging;
    bool chargePaused;
    float chargeStart;
    public bool IsCharging => charging;

    // 0 at press, 1 once held for the current weapon's chargeTime (and beyond, since
    // holding is uncapped). 0 whenever not charging.
    public float ChargeProgress => charging
        ? Mathf.Clamp01((Time.time - chargeStart) / Mathf.Max(0.0001f, pending.tool.chargeTime))
        : 0f;

    // Seconds the current press has been held. 0 whenever not charging.
    public float ChargeElapsed => charging ? Time.time - chargeStart : 0f;

    // Duration passed to PlayerAnimator.PlayAttack purely as its failsafe ceiling
    // (see PlayAttack's doc comment) -- large so an indefinitely held charge can never
    // hit it; the real end of a charge is always the button release.
    const float ChargeFailsafeCeiling = 3600f;

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
        if (!charging) return;

        // Once the swing has actually landed (or its animation finished on its own --
        // e.g. a weapon whose clip never calls OnAttackChargeReady) there's nothing
        // left to resolve. Stop treating a still-held button as an ongoing charge, so
        // holding the button past a quick shot/tap can't leave movement locked forever.
        if (!pending.armed || (animator != null && !animator.IsAttacking))
        {
            charging = false;
            chargePaused = false;
            CameraZoom.Instance?.SetZoomed(false);
            return;
        }

        CameraZoom.Instance?.SetZoomed(Time.time - chargeStart >= pending.tool.chargeTime);

        if (UserInput.Instance.UseReleased)
        {
            float elapsed = Time.time - chargeStart;
            float fraction = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, pending.tool.chargeTime));
            ResolveCharge(heavy: elapsed >= pending.tool.chargeTime, fraction);
        }
    }

    // A hand "wields melee" if what it offers has a Tool (stats) and a Hitbox
    // (melee strike region) but no RangedWeapon -- fists count, since the fists
    // prefab has Tool + Hitbox.
    bool IsMelee(HandSide side)
    {
        GameObject item = hands.ActiveItem(side);
        if (item == null || !item.TryGetComponent<Tool>(out _)) return false;
        return item.TryGetComponent<Hitbox>(out _) && !item.TryGetComponent<RangedWeapon>(out _);
    }

    // The Use button uses the selected hand (PlayerInteractor.ActiveHand) --
    // whatever it holds, and nothing happens if that's not a weapon. The one
    // exception is dual-wield: when both hands wield melee of the same class
    // (two real held weapons, or two bare fists), swings alternate hands every
    // time one lands, starting from the right. A mixed pair (weapon + fist) does
    // not alternate. Side-effect-free (only TryUse mutates the alternation
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

    public void TryUse(HandSide side)
    {
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

        // Arm the strike/shot; it lands when the clip's Animation Event fires. Whether
        // this resolves light or heavy is decided later (OnAttackChargeReady / Update).
        pending = new Swing { tool = tool, side = side, hitbox = hitbox, ranged = ranged, range = tool.range, armed = true, heavy = false };
        charging = true;
        chargePaused = false;
        chargeStart = Time.time;

        // Same aim source OnAttackHit uses to land the hit, so the swing animation
        // always faces the direction the hit will actually land in.
        if (animator != null) animator.PlayAttack(side, ChargeFailsafeCeiling, AimDirection(Origin));
    }

    // Called by an Animation Event on each *_attack clip, at the "charge hold" frame --
    // before the swing/hit. Purely visual: if the button's still held once the windup
    // reaches this pose, freeze the clip there until release. Whether the eventual
    // release counts as light or heavy is decided in Update by tool.chargeTime, not by
    // whether this ever fired -- a release can already be a valid heavy before the
    // animation gets here, or still resolve light after it, if chargeTime says so.
    public void OnAttackChargeReady()
    {
        if (!charging || !UserInput.Instance.UseHeld) return;
        chargePaused = true;
        if (animator != null) animator.PauseAttack();
    }

    void ResolveCharge(bool heavy, float fraction)
    {
        pending.heavy = heavy;
        pending.chargeFraction = fraction;
        charging = false;
        if (chargePaused && animator != null) animator.ResumeAttack();
        chargePaused = false;
        CameraZoom.Instance?.SetZoomed(false);
    }

    // Called by an Animation Event on each *_attack clip, at the swing's start
    // frame -- OnAttackHit's visual counterpart. Spawns the weapon's swing VFX
    // aimed where the strike will land; leaves the swing armed for the hit.
    public void OnAttackSwing()
    {
        if (!pending.armed || pending.tool == null) return;
        Vector2 dir = AimDirection(Origin);
        // Left-hand swings sweep the opposite way (mirrorSweep) for directional art;
        // the effect follows the swing origin so it moves with the player.
        pending.tool.SpawnSwingEffect(Origin, dir, pending.side == HandSide.Left,
                                      aimOrigin != null ? aimOrigin : transform, pending.heavy);

        // Push fires here, at the windup, not on the later hit frame.
        (float lungeSpeed, float lungeDuration, AnimationCurve lungeCurve) = pending.tool.GetLunge(pending.heavy);
        if (lunge != null && lunge.Begin(dir, lungeSpeed, lungeDuration, lungeCurve))
            HitResolution.NotifySwing(gameObject);
    }

    // Called by an Animation Event on each *_attack clip, at the contact frame.
    // Lands the armed swing/shot where the player is currently aiming.
    public void OnAttackHit()
    {
        if (!pending.armed) return;

        Vector2 origin = Origin;
        Vector2 dir = AimDirection(origin);

        AttackData attack = pending.tool.GetAttack(pending.heavy);

        if (pending.hitbox != null)
        {
            pending.hitbox.Strike(attack, gameObject, origin + dir * pending.range);
        }
        else if (pending.ranged != null)
        {
            RangedWeapon ranged = pending.ranged;
            Projectile arrow = Instantiate(ranged.projectilePrefab, origin + dir * ranged.spawnOffset, Quaternion.identity);
            float speed = ranged.GetSpeed(pending.chargeFraction);
            float range = ranged.GetRange(pending.chargeFraction, pending.tool.range);
            arrow.Launch(attack, gameObject, dir, speed, range);
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

}

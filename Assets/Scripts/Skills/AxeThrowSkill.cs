using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Axe spell: Axe Throw. The player swings the axe overhand (an attack clip of
// `throwStyle`, played as a cast) and lets go on the clip's contact frame -- its
// OnAttackHit Animation Event, which WeaponUse ignores during a cast. The axe
// (ThrownAxe) spins out at the soft target -- the one in a long, narrow cone along
// the aim that the aim points at most, the highlighted target winning when it's
// there -- passing through everything else, and hits only it with the axe's light
// attack at damageMultiplier (half). With nothing damageable in the cone it flies
// along the aim. Either way it comes back to the hand it left.
// While it's away the held axe is hidden and the hand is out of action (IHandLock:
// no swings, no spells). The catch shows it again with a punch and a jolt of the
// camera, and starts the cooldown. Lives in Prefabs/Skills/Axe Throw.prefab (see
// ActiveSkill); the flying axe is the Thrown Axe prefab it spawns.
public class AxeThrowSkill : ActiveSkill, IHandLock
{
    [Header("Throw")]
    [Tooltip("What flies: the Thrown Axe prefab (it takes on the thrown axe's sprite).")]
    [SerializeField] ThrownAxe thrownAxePrefab;
    [Tooltip("Attack clip family the throw plays ({dir}_attack_{style}_{l|r}).")]
    [SerializeField] string throwStyle = "hack";
    [Tooltip("Full angle of the cone the target is picked in, centred on the aim.")]
    [SerializeField, Range(1f, 180f)] float aimArc = 40f;
    [SerializeField] float damageMultiplier = 0.5f;
    [Tooltip("Seconds from the catch until it can be thrown again.")]
    [SerializeField] float cooldown = 6f;
    [SerializeField] ThrownAxe.Settings flight = new()
    {
        range = 7f,
        outSpeed = 7f,
        returnSpeed = 16f,
        stickTime = 0.12f,
        spinSpeed = 1080f,
        returnCurve = 0.35f,
    };

    [Header("Catch")]
    [Tooltip("The held axe pops to this scale on the catch and settles back.")]
    [SerializeField] float catchPunch = 1.3f;
    [SerializeField] float catchPunchTime = 0.12f;
    [Tooltip("Camera trauma on the catch.")]
    [SerializeField] float catchShake = 0.06f;

    Tool tool;             // the axe being thrown or in flight; null otherwise
    HandSide side;
    bool winding;          // the throw clip plays, the axe not yet let go
    bool away;             // in flight
    float readyAt;
    readonly List<SpriteRenderer> hidden = new();
    readonly List<StrikeTarget> targets = new();

    public override float Cooldown01 =>
        tool != null ? 1f : cooldown > 0f ? Mathf.Clamp01((readyAt - Time.time) / cooldown) : 0f;

    public bool Locks(HandSide hand) => tool != null && hand == side;

    public override bool TryActivate(GameObject caster, HandSide hand)
    {
        if (tool != null || thrownAxePrefab == null || Time.time < readyAt || animator.IsAttacking) return false;

        GameObject item = hands.ActiveItem(hand);
        if (item == null || !item.TryGetComponent(out Tool axe) || axe.kind != Weapon) return false;
        if (!item.TryGetComponent(out MeleeWeapon _)) return false;

        animator.PlayAttack(hand, 3f, Aim(), throwStyle, cast: true);
        if (!animator.IsCasting) return false;   // no such clip

        tool = axe;
        side = hand;
        winding = true;
        return true;
    }

    // The attack clips' contact frame (WeaponUse's event too): let go.
    public override void OnAnimationEvent(string function)
    {
        if (function != nameof(WeaponUse.OnAttackHit) || !winding) return;
        winding = false;

        // the axe left the hand during the windup (dropped, swapped): no throw
        if (tool == null || hands.ActiveItem(side) != tool.gameObject || !tool.TryGetComponent(out MeleeWeapon melee)
            || !tool.TryGetComponent(out SpriteRenderer held))
        {
            tool = null;
            return;
        }

        Vector2 aim = Aim();
        AttackData attack = tool.GetAttack(false, stats).Scaled(damageMultiplier).WithoutRecoil();
        Hurtbox target = PickTarget(melee, aim);

        Transform anchor = hands.Anchor(side);
        float height = anchor.position.y - user.transform.position.y;
        float curveSide = side == HandSide.Left ? 1f : -1f;
        ThrownAxe.Launch(thrownAxePrefab, held, anchor, height, aim, target, user, attack, flight, curveSide, Caught);

        Hide(tool.gameObject);
        away = true;
    }

    // The soft target along the aim: the highlighted one when it's in the cone, else
    // the one nearest the aim. Only damageable targets -- never homes onto one it'd
    // glance off.
    Hurtbox PickTarget(MeleeWeapon melee, Vector2 aim)
    {
        StrikeShape cone = new(flight.range, aimArc, 1);
        GameObject highlighted = preview != null ? preview.Primary : null;
        MeleeTargeting.Select(melee.Query(user, Origin, aim, cone, tool.kind, TargetScoring.AimAngle,
                                          highlighted, float.PositiveInfinity), targets);
        return targets.Count > 0 && !targets[0].glancing ? targets[0].hurtbox : null;
    }

    void Caught()
    {
        GameObject axe = tool != null ? tool.gameObject : null;
        bool inHand = axe != null && hands.ActiveItem(side) == axe;
        Show();
        tool = null;
        away = false;
        readyAt = Time.time + cooldown;

        if (!inHand) return;   // it left the hand mid-flight: nothing to catch it in
        StartCoroutine(Punch(axe.transform));
        if (CameraShake.Instance != null) CameraShake.Instance.Shake(catchShake);
    }

    void Update()
    {
        if (user == null) return;   // not bound yet

        // the throw clip ended without letting go (no contact event): call it off
        if (winding && !animator.IsCasting)
        {
            winding = false;
            tool = null;
        }

        // the axe left the hand mid-flight (swapped out, dropped, put in the bag):
        // don't leave it invisible wherever it went; the flying one still comes home
        if (away && hidden.Count > 0 && (tool == null || hands.ActiveItem(side) != tool.gameObject)) Show();
    }

    void Hide(GameObject item)
    {
        hidden.Clear();
        foreach (SpriteRenderer r in item.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!r.enabled) continue;
            r.enabled = false;
            hidden.Add(r);
        }
    }

    void Show()
    {
        foreach (SpriteRenderer r in hidden)
            if (r != null) r.enabled = true;
        hidden.Clear();
    }

    IEnumerator Punch(Transform item)
    {
        Vector3 rest = item.localScale;
        for (float t = 0f; t < catchPunchTime; t += Time.deltaTime)
        {
            if (item == null) yield break;
            item.localScale = rest * Mathf.Lerp(catchPunch, 1f, t / catchPunchTime);
            yield return null;
        }
        if (item != null) item.localScale = rest;
    }

    Vector2 Aim() => UserInput.Instance.AimDirection(Origin, Camera.main);

    void OnDisable()
    {
        Show();
        tool = null;
        winding = away = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.6f, 0.3f);
        Gizmos.DrawWireSphere(user != null ? (Vector3)Origin : transform.position, flight.range);
    }
}

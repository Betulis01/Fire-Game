using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Axe spell: Axe Throw. Aimed: its key arms it and a left click (or the key again)
// casts it (ActiveSkillSlots), the soft target following its cone meanwhile. The
// player swings the axe overhand (an attack clip of
// `throwStyle`, played as a cast) and lets go on the clip's contact frame -- its
// OnAttackHit Animation Event, which WeaponUse ignores during a cast. Its targets are
// characters only (never trees or ore) in a long, narrow cone along the aim: the one
// the aim points at most (the highlighted one) plus, with ranks in the node, the
// nearest others -- 1 / 2 / 3 targets at rank 1 / 2 / 3 (targetsByRank). They're
// locked on the cast click (what was lit), and the axe (ThrownAxe) hits them all with
// the axe's light attack at damageMultiplier, each at full damage, hopping on to
// whichever is closest to the one it just hit, even if they've moved -- passing
// through everything else. With no target it flies along the aim. Either way it comes
// back to the hand it left.
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
    [Tooltip("Targets one throw hits at each rank of its skill node (rank 1 first).")]
    [SerializeField] int[] targetsByRank = { 1, 2, 3 };
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
    readonly List<StrikeTarget> locked = new();   // the cast click's targets, main one first
    readonly List<Hurtbox> flightTargets = new();

    // For the audio layer (on this skill's prefab): the overhand windup starting, the
    // axe let go (the flying one, which raises its own Stuck / Returning), and the
    // catch (`inHand` false when the axe left the hand mid-flight).
    public event System.Action WindupStarted;
    public event System.Action<ThrownAxe> Released;
    public event System.Action<bool> CaughtBack;

    public override float Cooldown01 =>
        tool != null ? 1f : cooldown > 0f ? Mathf.Clamp01((readyAt - Time.time) / cooldown) : 0f;

    public bool Locks(HandSide hand) => tool != null && hand == side;

    public override bool Aimed => true;
    public override bool AimLive => winding;
    public override HandSide CastSide => side;

    public override bool TryGetAim(HandSide hand, out SkillAim aim)
    {
        aim = new SkillAim { range = flight.range, arc = aimArc };
        return true;
    }

    // How many targets a throw hits at the node's current rank.
    int TargetCount
    {
        get
        {
            if (targetsByRank == null || targetsByRank.Length == 0) return 1;
            int i = Mathf.Clamp(Rank - 1, 0, targetsByRank.Length - 1);
            return Mathf.Max(1, targetsByRank[i]);
        }
    }

    // The characters in the long, narrow cone along the aim: the one the aim points at
    // most, then up to TargetCount - 1 nearest it. Only the axe in `hand` can say what it
    // would damage, so no axe, no targets. Through the windup it's the locked targets.
    public override void SelectTargets(HandSide hand, Vector2 dir, GameObject preferred, float bias, List<StrikeTarget> into)
    {
        into.Clear();
        if (winding)
        {
            foreach (StrikeTarget t in locked)
                if (t.hurtbox != null && t.hurtbox.Health != null && !t.hurtbox.Health.IsDead) into.Add(t);
            return;
        }
        GameObject item = hands.ActiveItem(hand);
        if (item == null || !item.TryGetComponent(out Tool axe) || !item.TryGetComponent(out MeleeWeapon melee)) return;

        StrikeShape cone = new(flight.range, aimArc, TargetCount);
        TargetQuery query = melee.Query(user, Origin, dir, cone, axe.kind, TargetScoring.AimAngle, preferred, bias);
        query.charactersOnly = true;
        MeleeTargeting.Select(query, into);
    }

    public override bool TryActivate(GameObject caster, HandSide hand)
    {
        if (tool != null || thrownAxePrefab == null || Time.time < readyAt || animator.IsAttacking) return false;

        GameObject item = hands.ActiveItem(hand);
        if (item == null || !item.TryGetComponent(out Tool axe) || axe.kind != Weapon) return false;
        if (!item.TryGetComponent(out MeleeWeapon _)) return false;

        // lock what's lit now (the highlighted target forced first), before the windup
        Vector2 aim = Aim();
        LockTargets(hand, aim);

        animator.PlayAttack(hand, 3f, aim, throwStyle, cast: true);
        if (!animator.IsCasting)   // no such clip
        {
            locked.Clear();
            return false;
        }

        tool = axe;
        side = hand;
        winding = true;
        WindupStarted?.Invoke();
        return true;
    }

    // The attack clips' contact frame (WeaponUse's event too): let go.
    public override void OnAnimationEvent(string function)
    {
        if (function != nameof(WeaponUse.OnAttackHit) || !winding) return;
        winding = false;

        // the axe left the hand during the windup (dropped, swapped): no throw
        if (tool == null || hands.ActiveItem(side) != tool.gameObject || !tool.TryGetComponent(out MeleeWeapon _)
            || !tool.TryGetComponent(out SpriteRenderer held))
        {
            tool = null;
            locked.Clear();
            return;
        }

        Vector2 aim = Aim();
        AttackData attack = tool.GetAttack(false, stats).Scaled(damageMultiplier).WithoutRecoil();

        // the locked targets, main one first; ones that died meanwhile ThrownAxe skips
        flightTargets.Clear();
        foreach (StrikeTarget t in locked)
            if (t.hurtbox != null) flightTargets.Add(t.hurtbox);
        locked.Clear();

        Transform anchor = hands.Anchor(side);
        float height = anchor.position.y - user.transform.position.y;
        float curveSide = side == HandSide.Left ? 1f : -1f;
        ThrownAxe flying = ThrownAxe.Launch(thrownAxePrefab, held, anchor, height, aim, flightTargets, user, attack, flight, curveSide, Caught);

        Hide(tool.gameObject);
        away = true;
        Released?.Invoke(flying);
    }

    // What the axe will fly at, locked on the cast click: the highlighted target when
    // it's in the cone (the preview follows this spell while it's armed), else the one
    // nearest the aim, then the nearest others up to TargetCount. Only targets it would
    // damage -- never one it'd glance off.
    void LockTargets(HandSide hand, Vector2 aim)
    {
        GameObject highlighted = preview != null ? preview.Primary : null;
        SelectTargets(hand, aim, highlighted, float.PositiveInfinity, locked);
        locked.RemoveAll(t => t.glancing);
    }

    void Caught()
    {
        GameObject axe = tool != null ? tool.gameObject : null;
        bool inHand = axe != null && hands.ActiveItem(side) == axe;
        Show();
        tool = null;
        away = false;
        readyAt = Time.time + cooldown;
        CaughtBack?.Invoke(inHand);

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
            locked.Clear();
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
        locked.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.6f, 0.3f);
        Gizmos.DrawWireSphere(user != null ? (Vector3)Origin : transform.position, flight.range);
    }
}

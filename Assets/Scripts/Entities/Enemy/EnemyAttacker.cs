using UnityEngine;

// AI-driven counterpart to WeaponUse, scoped down: no HandSide, no Hands. The enemy
// swings whatever Tool+MeleeWeapon sits on its assigned `weapon` object (its own
// object, same convention as Fists.prefab), triggered by being in Combat and in range
// of EnemyBrain's aggro target instead of a player input button. Mirrors
// WeaponUse.TryUse/OnAttackHit (arm a swing, land it on the clip's Animation Event)
// so it stays reusable for enemies that never have Hands (e.g. a bear) and not just
// this player-like first enemy.
//
// Same strike cone and target cap as the player, but no soft-targeting: the aim is
// latched toward the aggro target when the swing starts (so it can be dodged), and
// that target is forced primary if it's still in the cone at contact. Otherwise
// whatever else is in the cone is hit, nearest first.
[RequireComponent(typeof(EnemyBrain))]
public class EnemyAttacker : MonoBehaviour
{
    [Tooltip("Drives the attack animation. Falls back to one on this GameObject.")]
    public EnemyAnimator animator;

    [Tooltip("The weapon GameObject this enemy swings (needs a Tool + MeleeWeapon), e.g. a Claws object.")]
    public GameObject weapon;

    [Tooltip("Distance to the aggro target within which a swing can start.")]
    public float attackRange = 0.8f;

    [Tooltip("Seconds to wait after a swing ends before the next one can start.")]
    public float attackCooldown = 3f;

    [Tooltip("Point swings originate from. Falls back to this transform if unset.")]
    public Transform aimOrigin;

    Tool tool;
    MeleeWeapon melee;
    EnemyBrain brain;

    bool armed;
    bool wasAttacking;
    float nextAttackAt;
    Vector2 aimDir;
    Transform swingTarget;   // the aggro target the armed swing was aimed at

    public Vector2 Origin => aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;

    // Direction of the last (or current) swing, for debug visualization. Zero
    // until this enemy has swung at least once.
    public Vector2 AimDir => aimDir;

    // Where the next swing would aim right now, for debug visualization: the armed
    // swing's latched aim, else toward the aggro target, else the last swing's aim.
    public Vector2 PreviewAimDir
    {
        get
        {
            if (armed || brain == null || brain.AggroTarget == null) return aimDir;
            Vector2 to = (Vector2)brain.AggroTarget.position - Origin;
            return to.sqrMagnitude > 1e-6f ? to.normalized : aimDir;
        }
    }

    // The strike cone this enemy swings with (its weapon is a prefab reference, not
    // an object in the scene, so debug tools can't find it by searching).
    public StrikeShape Strike => tool != null ? tool.GetStrike(false) : default;

    void Awake()
    {
        tool = weapon.GetComponent<Tool>();
        melee = weapon.GetComponent<MeleeWeapon>();
        brain = GetComponent<EnemyBrain>();
        if (animator == null) animator = GetComponent<EnemyAnimator>();
    }

    void Update()
    {
        if (brain.CurrentState != EnemyBrain.State.Combat) return;
        if (animator != null && animator.IsSpawning) return;

        // The swing just finished (clip done): the cooldown runs from here.
        bool attacking = animator != null && animator.IsAttacking;
        if (wasAttacking && !attacking) nextAttackAt = Time.time + attackCooldown;
        wasAttacking = attacking;

        if (attacking || Time.time < nextAttackAt) return;

        Transform target = brain.AggroTarget;
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.position - Origin;
        if (toTarget.magnitude > attackRange) return;

        aimDir = toTarget.normalized;
        swingTarget = target;
        armed = true;

        if (animator != null) animator.PlayAttack(0f, aimDir);
    }

    // Called by an Animation Event on the attack clip, at the swing's start frame —
    // OnAttackHit's visual counterpart. Spawns the weapon's swing VFX along the
    // aim captured when the swing began; leaves the swing armed for the hit.
    public void OnAttackSwing()
    {
        if (!armed) return;
        tool.SpawnSwingEffect(Origin, aimDir, false, aimOrigin != null ? aimOrigin : transform, false,
                              tool.GetStrike(false).range);
    }

    // Called by an Animation Event on the attack clip, at the contact frame.
    public void OnAttackHit()
    {
        if (!armed) return;

        GameObject preferred = swingTarget != null ? swingTarget.gameObject : null;
        TargetQuery query = melee.Query(gameObject, Origin, aimDir, tool.GetStrike(false), tool.kind,
                                        TargetScoring.Distance, preferred, float.PositiveInfinity);
        melee.Strike(tool.GetAttack(false), query);
        armed = false;
    }
}

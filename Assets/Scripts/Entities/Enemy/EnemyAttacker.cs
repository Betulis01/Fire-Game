using UnityEngine;

// AI-driven counterpart to ToolUser, scoped down: no HandSide, no Hands. The enemy
// swings whatever Tool+Hitbox sits on its assigned `weapon` object (its own object,
// same convention as Fists.prefab), triggered by being in Combat and in range of
// EnemyBrain's aggro target instead of a player input button. Mirrors
// ToolUser.UseHand/OnAttackHit (arm a swing, land it on the clip's Animation Event)
// so it stays reusable for enemies that never have Hands (e.g. a bear) and not just
// this player-like first enemy.
[RequireComponent(typeof(EnemyBrain))]
public class EnemyAttacker : MonoBehaviour
{
    [Tooltip("Drives the attack animation. Falls back to one on this GameObject.")]
    public EnemyAnimator animator;

    [Tooltip("The weapon GameObject this enemy swings (needs a Tool + Hitbox), e.g. a Claws object.")]
    public GameObject weapon;

    [Tooltip("Distance to the aggro target within which a swing can start.")]
    public float attackRange = 0.8f;

    [Tooltip("Point swings originate from. Falls back to this transform if unset.")]
    public Transform aimOrigin;

    Tool tool;
    Hitbox hitbox;
    EnemyBrain brain;

    float readyAt;
    bool armed;
    Vector2 aimDir;

    Vector2 Origin => aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;

    void Awake()
    {
        tool = weapon.GetComponent<Tool>();
        hitbox = weapon.GetComponent<Hitbox>();
        brain = GetComponent<EnemyBrain>();
        if (animator == null) animator = GetComponent<EnemyAnimator>();
    }

    void Update()
    {
        if (brain.CurrentState != EnemyBrain.State.Combat) return;
        if (Time.time < readyAt) return;

        Transform target = brain.AggroTarget;
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.position - Origin;
        if (toTarget.magnitude > attackRange) return;

        aimDir = toTarget.normalized;
        armed = true;

        float lockDuration = 1f / Mathf.Max(0.01f, tool.swingSpeed);
        if (animator != null) animator.PlayAttack(lockDuration, aimDir);
        readyAt = Time.time + lockDuration;
    }

    // Called by an Animation Event on the attack clip, at the swing's start frame —
    // OnAttackHit's visual counterpart. Spawns the weapon's swing VFX along the
    // aim captured when the swing began; leaves the swing armed for the hit.
    public void OnAttackSwing()
    {
        if (!armed) return;
        tool.SpawnSwingEffect(Origin, aimDir, false, aimOrigin != null ? aimOrigin : transform, false);
    }

    // Called by an Animation Event on the attack clip, at the contact frame.
    public void OnAttackHit()
    {
        if (!armed) return;

        Vector2 center = Origin + aimDir * tool.range;
        hitbox.Strike(tool.GetAttack(false), gameObject, center);
        armed = false;
    }
}

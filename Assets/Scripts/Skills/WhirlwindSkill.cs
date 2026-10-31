using UnityEngine;

// Axe spell: Whirlwind. The player spins with the axe held out, playing the
// "whirlwind" clip `spins` times in a row; each turn of the clip fires its
// OnWhirlwindHit Animation Event, which strikes a full 360-degree cone of `radius`
// around the player with the axe's light attack (scaled by damageMultiplier) -- the
// nearest `maxTargets` (enemies before resources), so it can't scale infinitely
// either. Hits run through the normal pipeline (Hurtbox -> Health, ToolDamageFilter,
// knockback away from the player), so they also earn axe XP. The player can keep
// moving while spinning.
// A left-hand cast plays the clip mirrored (a clockwise spin), the same way
// attacks mirror, so the axe always sits on the anchor the clip swings.
// Lives in Prefabs/Skills/Whirlwind.prefab (see ActiveSkill).
public class WhirlwindSkill : ActiveSkill
{
    public const string HitEvent = "OnWhirlwindHit";

    [Header("Spin")]
    [Tooltip("Animator state played; its clip is one full turn with an OnWhirlwindHit event.")]
    [SerializeField] string state = "whirlwind";
    [Tooltip("How many times the clip plays (one strike per turn).")]
    [SerializeField] int spins = 5;
    [Tooltip("Strike radius around the player, per turn.")]
    [SerializeField] float radius = 1f;   // hand orbit (0.25) + axe length (0.5) + a hurtbox's worth
    [Tooltip("Most entities each turn can hit, nearest first.")]
    [SerializeField, Min(1)] int maxTargets = 4;
    [SerializeField] float damageMultiplier = 1f;
    [Tooltip("Seconds from cast until it can be cast again.")]
    [SerializeField] float cooldown = 8f;

    Tool tool;            // the axe that's spinning; null when not casting
    int strikesLeft;
    float readyAt;

    public override float Cooldown01 => cooldown > 0f ? Mathf.Clamp01((readyAt - Time.time) / cooldown) : 0f;

    public override bool TryActivate(GameObject caster, HandSide side)
    {
        if (Time.time < readyAt || animator.IsAttacking) return false;

        GameObject item = hands.ActiveItem(side);
        if (item == null || !item.TryGetComponent(out Tool axe) || axe.kind != Weapon) return false;
        if (!item.TryGetComponent(out MeleeWeapon _)) return false;

        if (!animator.PlayCast(state, spins, mirrored: side == HandSide.Left)) return false;

        tool = axe;
        strikesLeft = spins;
        readyAt = Time.time + cooldown;
        return true;
    }

    // The whirlwind clip's event, once per turn.
    public override void OnAnimationEvent(string function)
    {
        if (function != HitEvent || tool == null || strikesLeft <= 0) return;

        // the axe left the hand mid-spin (dropped, swapped): nothing to strike with
        if (!tool.TryGetComponent(out MeleeWeapon melee) || !IsHeld(tool.gameObject))
        {
            tool = null;
            return;
        }

        // the axe's light attack at damageMultiplier (the skill's scaling); no recoil:
        // the spin hits from every side at once
        AttackData attack = tool.GetAttack(false, stats).Scaled(damageMultiplier).WithoutRecoil();
        StrikeShape spin = new(radius, 360f, maxTargets, tool.secondaryDamageMultiplier);
        melee.Strike(attack, melee.Query(user, Origin, Vector2.right, spin, tool.kind, TargetScoring.Distance));

        if (--strikesLeft <= 0) tool = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.66f, 0.42f, 0.95f);
        Gizmos.DrawWireSphere(user != null ? (Vector3)Origin : transform.position, radius);
    }
}

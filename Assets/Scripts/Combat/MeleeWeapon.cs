using System.Collections.Generic;
using UnityEngine;

// Capability: this item strikes in melee. Sits beside Tool (stats: damage, range,
// arcAngle, maxTargets, ...) on a weapon prefab, in place of RangedWeapon. Strike()
// only DETECTS and DELIVERS: MeleeTargeting picks who the aimed cone hits (primary +
// capped secondaries) and each gets a HitInfo through HitResolution. What a hit does
// (damage, knockback, ...) lives in the victim's IHitReactor components (Health
// checks ToolDamageFilter before applying damage); recoil lives in the attacker's
// IAttackReactor, notified once per connecting swing.
public class MeleeWeapon : MonoBehaviour
{
    [Tooltip("Hurtbox layers this weapon can reach (player weapons: EnemyHurtbox + " +
             "ResourceHurtbox; enemy weapons: PlayerHurtbox).")]
    public LayerMask targetLayers;

    [Tooltip("Hurtbox layers that outrank the rest when picking the primary target, " +
             "e.g. EnemyHurtbox so an enemy in the cone wins over a tree.")]
    public LayerMask priorityLayers;

    readonly List<StrikeTarget> targets = new();

    // A targeting query for this weapon's layers.
    public TargetQuery Query(GameObject owner, Vector2 origin, Vector2 direction, in StrikeShape shape,
                             ToolKind kind, TargetScoring scoring, GameObject preferred = null,
                             float preferredBias = 0f) => new TargetQuery
    {
        owner = owner,
        origin = origin,
        direction = direction,
        shape = shape,
        kind = kind,
        targetLayers = targetLayers,
        priorityLayers = priorityLayers,
        scoring = scoring,
        preferred = preferred,
        preferredBias = preferredBias,
    };

    // Land a swing: the primary takes `attack`, each secondary the same scaled by
    // shape.secondaryDamage. The attacker's reactors hear about it once, at the
    // primary's contact point. Returns how many targets were hit.
    public int Strike(in AttackData attack, in TargetQuery query)
    {
        MeleeTargeting.Select(query, targets);
        if (targets.Count == 0) return 0;

        AttackData secondary = attack.WithDamage(attack.damage * query.shape.secondaryDamage);
        for (int i = 0; i < targets.Count; i++)
            HitResolution.Deliver(targets[i].hurtbox, query.owner, i == 0 ? attack : secondary, targets[i].point);

        HitResolution.NotifyAttacker(query.owner, attack, targets[0].point);
        return targets.Count;
    }
}

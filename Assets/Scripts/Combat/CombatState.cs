using System;
using System.Collections.Generic;
using UnityEngine;

// Whether this entity (the player) is in combat. It is while any living enemy is
// engaged with it (an EnemyBrain chasing it, registered via Engage), and for
// leaveDelay seconds after the last combat exchange: dealing damage to an enemy or
// taking combat damage from an attacker. Chopping trees or freezing never counts.
// Combat-only UI (AimIndicator, ...) reads InCombat or listens to Changed.
[RequireComponent(typeof(Health))]
public class CombatState : MonoBehaviour, IDamageDealtReactor
{
    [Tooltip("Seconds after the last hit dealt or taken (with no enemy still engaged) " +
             "before combat ends.")]
    [SerializeField] float leaveDelay = 5f;

    public bool InCombat { get; private set; }
    public event Action<bool> Changed;

    readonly HashSet<EnemyBrain> engaged = new();
    float lastExchange = float.NegativeInfinity;

    void Awake() => GetComponent<Health>().Damaged += OnDamaged;

    // An enemy started chasing this entity. It counts until it dies, despawns or
    // switches target (EnemyBrain.IsEngagedWith).
    public void Engage(EnemyBrain enemy)
    {
        engaged.Add(enemy);
        Refresh();
    }

    void OnDamaged(DamageInfo info)
    {
        if (info.type == DamageType.Combat && info.attacker != null) lastExchange = Time.time;
    }

    public void OnDealtDamage(Health victim, in DamageInfo info)
    {
        if (victim.TryGetComponent(out EnemyBrain _)) lastExchange = Time.time;
    }

    void Update() => Refresh();

    void Refresh()
    {
        engaged.RemoveWhere(e => e == null || !e.IsEngagedWith(transform));
        bool now = engaged.Count > 0 || Time.time - lastExchange < leaveDelay;
        if (now == InCombat) return;
        InCombat = now;
        Changed?.Invoke(now);
    }
}

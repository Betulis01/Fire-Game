using System.Collections.Generic;
using UnityEngine;

// Drop on any entity whose hits should show floating numbers. Listens to the
// sibling Health and spawns a popup above the target when it takes damage.
[RequireComponent(typeof(Health))]
public class CombatTextEmitter : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1f, 0f);   // spawn height above the target

    Health health;

    // damage accumulated per type but not yet shown, plus where to spawn it when it flushes
    readonly Dictionary<DamageType, (float amount, Vector3 position)> pending = new();

    // healing accumulated but not yet shown, plus where to spawn it when it flushes
    float pendingHeal;
    Vector3 pendingHealPos;

    void Awake() => health = GetComponent<Health>();

    void OnEnable()
    {
        pending.Clear();
        pendingHeal = 0f;
        health.Damaged += OnDamaged;
        health.Healed += OnHealed;
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Healed -= OnHealed;
    }

    void OnDamaged(DamageInfo info)
    {
        Vector3 spawnPos = info.point.HasValue ? (Vector3)info.point.Value + offset : transform.position + offset;

        float accumulated = (pending.TryGetValue(info.type, out var bucket) ? bucket.amount : 0f) + info.amount;
        if (accumulated < 1f)
        {
            pending[info.type] = (accumulated, spawnPos);
            return;
        }

        float toShow = Mathf.Floor(accumulated);
        pending[info.type] = (accumulated - toShow, spawnPos);

        CombatText.Spawn(spawnPos, toShow, StyleFor(info.type));
    }

    void OnHealed(float amount)
    {
        pendingHealPos = transform.position + offset;
        pendingHeal += amount;
        if (pendingHeal < 1f) return;

        float toShow = Mathf.Floor(pendingHeal);
        pendingHeal -= toShow;

        CombatText.Spawn(pendingHealPos, toShow, TargetStyle(), heal: true);
    }

    PopupStyle TargetStyle() => gameObject.CompareTag("Player") ? PopupStyle.Player : PopupStyle.Enemy;

    PopupStyle StyleFor(DamageType type) => type switch
    {
        DamageType.Combat => TargetStyle(),
        DamageType.Environment => PopupStyle.Environment,
        _ => PopupStyle.Enemy,
    };
}

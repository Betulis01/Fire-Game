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

    void Awake() => health = GetComponent<Health>();

    void OnEnable()
    {
        pending.Clear();
        health.Damaged += OnDamaged;
    }

    void OnDisable() => health.Damaged -= OnDamaged;

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

    PopupStyle StyleFor(DamageType type) => type switch
    {
        DamageType.Combat => gameObject.CompareTag("Player") ? PopupStyle.Player : PopupStyle.Enemy,
        DamageType.Environment => PopupStyle.Environment,
        _ => PopupStyle.Enemy,
    };
}

using UnityEngine;

// Drop on any entity whose hits should show floating numbers. Listens to the
// sibling Health and spawns a popup above the target when it takes damage.
[RequireComponent(typeof(Health))]
public class CombatTextEmitter : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 1f, 0f);   // spawn height above the target

    Health health;

    void Awake() => health = GetComponent<Health>();

    void OnEnable() => health.Damaged += OnDamaged;
    void OnDisable() => health.Damaged -= OnDamaged;

    void OnDamaged(float amount)
    {
        CombatText.Spawn(transform.position + offset, amount);
    }
}

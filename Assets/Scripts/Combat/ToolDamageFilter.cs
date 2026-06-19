using UnityEngine;

// Capability: restricts which tool kinds can damage this entity (axe-only trees,
// pickaxe-only ore, ...). Sits beside Health in the composition model, next to
// DropOnDeath/EntityDeath. Absent = damageable by any tool (the default for enemies
// and the player). Only gates tool/Hitbox damage; environmental damage that calls
// Health.TakeDamage directly (cold, fire) bypasses this and still applies.
[RequireComponent(typeof(Health))]
public class ToolDamageFilter : MonoBehaviour
{
    [Tooltip("Tool kinds allowed to damage this entity. Strikes from other kinds are ignored.")]
    public ToolKind damagedBy = ToolKind.Axe;

    public bool Accepts(ToolKind kind) => (damagedBy & kind) != 0;
}

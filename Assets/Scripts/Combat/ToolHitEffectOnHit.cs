using System;
using UnityEngine;

// Attacker-side reactor: spawns a tool-specific VFX prefab at the strike point whenever
// this entity lands a hit. Lives on the attacker (player or AI); Hitbox notifies it
// through IAttackReactor, so it needs no wiring into the hit pipeline. Mirrors
// CameraShakeOnHit/HitStopOnHit. ToolKind is [Flags], so entries are matched by bit
// overlap rather than equality.
// HitInfo.direction is victim-relative and zero on this attacker-side echo (Hitbox
// builds it with no target), but HitInfo.point is the real world-space contact point
// (Hitbox.Strike computes it from where the strike circle actually touched a
// hurtbox), so the angle is derived from aimOrigin -> hit.point instead of reading
// live input — works the same for a mouse-aimed player and an AI attacker.
public class ToolHitEffectOnHit : MonoBehaviour, IAttackReactor
{
    [Serializable]
    public struct Entry
    {
        public ToolKind kind;
        public GameObject effectPrefab;
        [Tooltip("Degrees added so the effect's drawn forward aligns with the aim " +
                 "(+X). Art pointing right = 0, up = -90.")]
        public float spriteAngleOffset;
    }

    public Entry[] effects;

    [Tooltip("Swing origin to aim from — the same point ToolUser/EnemyAttacker strikes " +
             "from. Falls back to this transform if unset.")]
    public Transform aimOrigin;

    public void OnDealtHit(in HitInfo hit)
    {
        Vector2 origin = aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;

        foreach (Entry entry in effects)
        {
            if (entry.effectPrefab == null) continue;
            if ((entry.kind & hit.kind) == 0) continue;

            Vector2 toPoint = hit.point - origin;
            Vector2 dir = toPoint.sqrMagnitude > 1e-6f ? toPoint.normalized : Vector2.right;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Instantiate(entry.effectPrefab, hit.point, Quaternion.Euler(0f, 0f, angle + entry.spriteAngleOffset));
            break;
        }
    }
}

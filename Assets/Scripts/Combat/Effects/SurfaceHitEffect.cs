using UnityEngine;

// Victim-side reactor: spawns the contact VFX for hits this entity takes. The
// weapon decides the animation (HitInfo.hitEffectPrefab — a sword hit looks like a
// sword hit); this entity's SurfaceMaterial decides the color (blood red, bark
// brown, ...). Sits on the entity root next to Health/Knockback; Hurtbox fans
// landed hits out to every IHitReactor there, so melee strikes and projectiles
// both arrive with no extra wiring. A weapon with no contact effect (or an unset
// material) spawns nothing.
// HitInfo.direction is the attacker->victim unit vector, so the effect faces away
// from whoever dealt the blow.
public class SurfaceHitEffect : MonoBehaviour, IHitReactor
{
    [Tooltip("What this entity is made of — the tint applied to the weapon's contact effect.")]
    public SurfaceMaterial material;

    public void OnHit(in HitInfo hit)
    {
        if (material == null || hit.hitEffectPrefab == null) return;

        Vector2 dir = hit.direction.sqrMagnitude > 1e-6f ? hit.direction : Vector2.right;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GameObject effect = Instantiate(hit.hitEffectPrefab, hit.point,
                                        Quaternion.Euler(0f, 0f, angle + hit.hitEffectAngleOffset));

        foreach (SpriteRenderer sr in effect.GetComponentsInChildren<SpriteRenderer>())
            sr.color *= material.tint;
    }
}

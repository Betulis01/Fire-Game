using UnityEngine;

// Victim-side reactor: spawns the contact VFX for hits this entity takes. The
// weapon decides the animation (HitInfo.hitEffectPrefab — a sword hit looks like a
// sword hit); this entity's SurfaceMaterial decides the color (blood red, bark
// brown, ...). Sits on the entity root next to Health/Knockback; Hurtbox fans
// landed hits out to every IHitReactor there, so melee strikes and projectiles
// both arrive with no extra wiring. A weapon with no contact effect (or an unset
// material) spawns nothing.
// Spawned as drawn (no rotation toward the hit direction) since these effects
// (blood, ...) fall under gravity and would look wrong tilted off-vertical.
public class SurfaceHitEffect : MonoBehaviour, IHitReactor
{
    [Tooltip("What this entity is made of — the tint applied to the weapon's contact effect.")]
    public SurfaceMaterial material;

    public void OnHit(in HitInfo hit)
    {
        if (material == null || hit.hitEffectPrefab == null) return;

        GameObject effect = Instantiate(hit.hitEffectPrefab, hit.point, Quaternion.identity);

        foreach (SpriteRenderer sr in effect.GetComponentsInChildren<SpriteRenderer>())
            sr.color *= material.tint;
    }
}

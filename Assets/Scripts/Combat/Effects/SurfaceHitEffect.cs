using UnityEngine;

// Victim-side reactor: spawns the contact VFX and sfx for hits this entity takes,
// picked by what this entity is made of (HitEffect), not by which weapon struck —
// a punch to a tree looks/sounds the same as an axe to a tree. Sits on the entity
// root next to Health/Knockback; Hurtbox fans landed hits out to every IHitReactor
// there, so melee strikes and projectiles both arrive with no extra wiring.
// Spawned as drawn (no rotation toward the hit direction) since these effects
// (blood, ...) fall under gravity and would look wrong tilted off-vertical.
public class SurfaceHitEffect : MonoBehaviour, IHitReactor
{
    public HitEffect hitEffect;

    public void OnHit(in HitInfo hit)
    {
        if (hitEffect == null) return;

        if (hitEffect.effectPrefab != null)
            Instantiate(hitEffect.effectPrefab, hit.point, Quaternion.identity);

        if (hitEffect.sfx != null)
            AudioSource.PlayClipAtPoint(hitEffect.sfx, hit.point);
    }
}

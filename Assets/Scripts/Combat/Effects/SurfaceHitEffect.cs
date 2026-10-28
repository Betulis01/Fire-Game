using UnityEngine;
using UnityEngine.Rendering;

// Victim-side reactor: spawns the contact VFX and sfx for hits this entity takes,
// picked by what this entity is made of (HitEffect), not by which weapon struck —
// a punch to a tree looks/sounds the same as an axe to a tree. Sits on the entity
// root next to Health/Knockback; Hurtbox fans landed hits out to every IHitReactor
// there, so melee strikes and projectiles both arrive with no extra wiring.
// Spawned as drawn, unless the HitEffect is alignToHit (a blood spray): then it's
// turned to fly along the hit, away from the attacker, at any angle. atSpriteCentre
// moves it from the contact point (the hurtbox's near edge) to the middle of the art.
//
// sortWithBody puts the VFX inside the entity's SortingGroup (as a child, so it also
// rides along with knockback): in front of the body art when it flies down-screen
// (toward the camera), behind it when it flies up-screen. Without a SortingGroup it
// keeps the prefab's own sorting.
public class SurfaceHitEffect : MonoBehaviour, IHitReactor
{
    public HitEffect hitEffect;

    [Tooltip("The body art: where an atSpriteCentre effect spawns (its centre) and what a " +
             "sortWithBody effect sorts against. For art split into layers (the skeleton's " +
             "Body and Head). Empty = this object's own sprite.")]
    public SpriteRenderer[] bodySprites;

    SortingGroup group;

    void Awake()
    {
        group = GetComponent<SortingGroup>();
        if (bodySprites == null || bodySprites.Length == 0)
            bodySprites = TryGetComponent(out SpriteRenderer own) ? new[] { own } : new SpriteRenderer[0];
    }

    public void OnHit(in HitInfo hit)
    {
        if (hitEffect == null) return;

        if (hitEffect.effectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffect.effectPrefab, SpawnPoint(hit), Quaternion.identity);
            if (hitEffect.alignToHit && hit.direction != Vector2.zero)
                AlignTo(fx, hit.direction, hitEffect.drawnAngle);
            if (hitEffect.sortWithBody)
                SortWithBody(fx, hit.direction);
        }

        if (hitEffect.sfx != null)
            AudioSource.PlayClipAtPoint(hitEffect.sfx, hit.point);
    }

    // The centre of the art as it's drawn right now (this frame's sprite), else the
    // contact point when there's no art to measure.
    Vector2 SpawnPoint(in HitInfo hit)
    {
        if (!hitEffect.atSpriteCentre) return hit.point;

        Bounds? art = null;
        foreach (SpriteRenderer sr in bodySprites)
        {
            if (sr == null || sr.sprite == null) continue;
            if (art.HasValue)
            {
                Bounds b = art.Value;
                b.Encapsulate(sr.bounds);
                art = b;
            }
            else art = sr.bounds;
        }
        return art.HasValue ? (Vector2)art.Value.center : hit.point;
    }

    // One order above the body's top layer (in front) or below its bottom one (behind),
    // on the body's sorting layer. Layers outside bodySprites (the skeleton's ground
    // shadow and target outline) sit further down, clear of the behind slot.
    void SortWithBody(GameObject fx, Vector2 dir)
    {
        if (group == null || bodySprites.Length == 0 || bodySprites[0] == null) return;

        int lowest = int.MaxValue, highest = int.MinValue;
        foreach (SpriteRenderer sr in bodySprites)
        {
            if (sr == null) continue;
            lowest = Mathf.Min(lowest, sr.sortingOrder);
            highest = Mathf.Max(highest, sr.sortingOrder);
        }

        bool behind = dir.y > 0f;
        fx.transform.SetParent(transform, true);
        foreach (SpriteRenderer sr in fx.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerID = bodySprites[0].sortingLayerID;
            sr.sortingOrder = behind ? lowest - 1 : highest + 1;
        }
    }

    // Rotate the art from its drawn direction onto dir. Leftward hits mirror it first
    // (flipX), so it never turns more than 90 degrees and its top edge stays on top.
    static void AlignTo(GameObject fx, Vector2 dir, float drawnAngle)
    {
        bool mirror = dir.x < 0f;
        float drawn = mirror ? 180f - drawnAngle : drawnAngle;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - drawn;
        fx.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        foreach (SpriteRenderer sr in fx.GetComponentsInChildren<SpriteRenderer>())
            sr.flipX = mirror;
    }
}

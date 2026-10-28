using UnityEngine;

// Fits a swing effect to the strike cone. The art is a sweep drawn around its pivot
// (the canvas centre); it's rotated so the sweep faces the aim, then pushed out along
// the aim until its leading edge -- `artReach` past the pivot -- sits exactly on the
// cone's rim (`reach` from the apex). Translated only, never scaled, so the pixel
// art stays crisp. Follows the wielder (the cone's apex) while it plays.
//
// `mirrorSweep` (left-hand swings) adds a local Y flip, which after rotation is
// always across the aim axis: the sweep still faces the aim but runs the other way,
// following the hand wielding the weapon. Shared by every melee weapon's effect
// (axe, sword, fists); per-art differences are just these fields.
public class SwingEffectOrienter : MonoBehaviour, ISwingEffectAnchor
{
    [Tooltip("Degrees added to the aim so the drawn sweep faces it: the art direction " +
             "that should point along the aim, measured clockwise from east (+X). Art " +
             "whose sweep bulges down = 90. Negated for mirrorSweep so both hands stay " +
             "symmetric about the aim.")]
    public float startAngleOffset = 0f;

    [Tooltip("How far the art's leading edge reaches past its pivot, along the " +
             "direction startAngleOffset turns onto the aim, in world units at scale 1 " +
             "(pixels / PPU), measured over every frame. The effect is placed so that " +
             "edge sits exactly on the cone's rim.")]
    public float artReach = 0f;

    [Tooltip("Flip the sweep for off-hand (left) swings.")]
    public bool mirrorForOffHand = true;

    // The wielder's aim origin, followed by world position each frame. Not parented:
    // the wielder's SortingGroup would swallow this effect's own sorting layer / YSort
    // (same reason AimIndicator detaches).
    Transform anchor;
    Vector2 apex;     // spawn point, used when there's no anchor to follow
    Vector2 dir = Vector2.right;
    float offset;     // pivot distance from the apex along the aim

    public void Orient(Vector2 aimDir, bool mirrorSweep, Transform anchor, float reach)
    {
        this.anchor = anchor;
        apex = transform.position;
        dir = aimDir.sqrMagnitude > 1e-6f ? aimDir.normalized : Vector2.right;
        offset = reach - artReach * Mathf.Abs(transform.localScale.x);

        bool mirror = mirrorSweep && mirrorForOffHand;
        float aim = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, aim + (mirror ? -startAngleOffset : startAngleOffset));

        if (mirror)
        {
            Vector3 s = transform.localScale;
            s.y = -Mathf.Abs(s.y);
            transform.localScale = s;
        }

        Follow();
    }

    // Track the wielder after it has moved this frame, so the swing stays on the
    // character instead of hanging where it was spawned.
    void LateUpdate() => Follow();

    void Follow()
    {
        Vector2 from = anchor != null ? (Vector2)anchor.position : apex;
        transform.position = from + dir * offset;
    }
}

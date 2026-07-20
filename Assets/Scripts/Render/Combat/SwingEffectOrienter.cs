using UnityEngine;

// Orients a swing-effect whose art is drawn facing EAST (bottom-left pivot). The
// effect simply rotates to the exact aim angle — no direction-based mirroring, so
// the arc sweeps identically all the way around the circle with no flip pop.
// The only mirror is `mirrorSweep` (left-hand swings): a local Y flip, which after
// rotation is always across the aim axis, so the arc still points where aimed but
// sweeps the other way — the effect follows the hand wielding the weapon.
// The aim is also registered as a cardinal (same horizontal-wins tie-break as
// PlayerAnimator.ResolveDir), exposed for systems that need the swing's discrete
// direction.
// Presence of this component makes Tool.SpawnSwingEffect anchor the effect at
// strike range from the wielder (rather than spawning it fixed in place, as
// legacy art without an orienter does).
public class SwingEffectOrienter : MonoBehaviour
{
    [Tooltip("Degrees added to the aim so the sweep starts offset from dead-on (e.g. " +
             "45 so a swing starts up-and-right of the aim and arcs through it). " +
             "Negated for mirrorSweep so both hands stay symmetric about the aim.")]
    public float startAngleOffset = 45f;

    [Tooltip("Extra distance added to the strike range along the aim, for hand-tuning " +
             "where this art needs to sit further out (or in) than the raw range — e.g. " +
             "elongated art whose drawn tip visibly falls short of the range circle at " +
             "a large startAngleOffset. Positive pushes it further from the wielder.")]
    public float rangeOffset = 0f;

    // The discrete direction this swing registered as (unit right/left/up/down).
    public Vector2 Cardinal { get; private set; } = Vector2.right;

    // The wielder's swing origin, followed by world position each frame (offset out
    // to `range + rangeOffset` along the aim). Not parented: the wielder's
    // SortingGroup would swallow this effect's own sorting layer / YSort (same
    // reason AimIndicator detaches).
    Transform anchor;
    Vector2 dir = Vector2.right;
    float range;

    public void Orient(Vector2 aimDir, bool mirrorSweep, Transform anchor = null, float range = 0f)
    {
        this.anchor = anchor;
        this.range = range;
        dir = aimDir.sqrMagnitude > 1e-6f ? aimDir.normalized : Vector2.right;

        float aim = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        // Horizontal wins ties, so a mostly sideways diagonal reads as east/west.
        bool horizontal = Mathf.Abs(aimDir.x) >= Mathf.Abs(aimDir.y);
        Cardinal = horizontal ? (aimDir.x < 0f ? Vector2.left : Vector2.right)
                              : (aimDir.y >= 0f ? Vector2.up : Vector2.down);

        float offset = mirrorSweep ? -startAngleOffset : startAngleOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, aim + offset);

        if (mirrorSweep)
        {
            // Opposite sweep for the off-hand: local Y flip = across the aim axis.
            Vector3 s = transform.localScale;
            s.y = -Mathf.Abs(s.y);
            transform.localScale = s;
        }
    }

    // Track the wielder after it has moved this frame, so the swing stays on the
    // character instead of hanging where it was spawned.
    void LateUpdate()
    {
        if (anchor != null) transform.position = (Vector2)anchor.position + dir * (range + rangeOffset);
    }
}

using UnityEngine;

// Axe swing effect: a single continuous piece of art (no directional variants)
// that spawns at the wielder's own center and rotates to face the aim direction.
// Same rotate-to-aim idea as SwordSwingEffectOrienter, just anchored at the
// wielder's position instead of out at strike range.
public class AxeSwingEffectOrienter : MonoBehaviour, ISwingEffectAnchor
{
    [Tooltip("Local Y flip for the off-hand sweep (mirrorSweep), same trick as " +
             "SwordSwingEffectOrienter: after rotation this is always across the aim axis.")]
    public bool mirrorForOffHand = true;

    Transform anchor;

    // Art must be drawn facing EAST (+X) — rotation goes straight to the aim
    // angle with no offset, so the effect always points exactly where aimed.
    public void Orient(Vector2 aimDir, bool mirrorSweep, Transform anchor, float range)
    {
        this.anchor = anchor;

        float aim = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, aim);

        if (mirrorSweep && mirrorForOffHand)
        {
            Vector3 s = transform.localScale;
            s.y = -Mathf.Abs(s.y);
            transform.localScale = s;
        }

        transform.position = anchor.position;
    }

    // Track the wielder after it has moved this frame, same reason as
    // SwordSwingEffectOrienter: not parented, so the wielder's SortingGroup doesn't
    // swallow this effect's own sorting.
    void LateUpdate()
    {
        if (anchor != null) transform.position = anchor.position;
    }
}

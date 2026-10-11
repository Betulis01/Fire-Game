using UnityEngine;

// Keeps a light/VFX on the floor under the wielder instead of riding the item (e.g. a
// torch's ground pool light, which would otherwise bob and tilt with the hand rig's
// animated height/rotation). While its WorldItem ancestor is held, this is reparented
// onto the wielder (Hands root) and pinned to its position + floorOffset; when the
// item is dropped it goes back under its original parent at its authored local
// position. World rotation is always identity. If the item is destroyed while the
// light is parented to the wielder (consumed, crafted away), the light goes with it.
[DefaultExecutionOrder(100)] // after HeldItemRotationFilter's LateUpdate (default order 0)
public class FloorAnchor : MonoBehaviour
{
    [Tooltip("Height above the wielder's floor to sit at while held.")]
    [SerializeField] float floorOffset = 0f;

    WorldItem worldItem;
    Transform home;                 // authored parent (the item), restored on drop
    Vector3 restLocalPosition;
    Hands wielder;                  // set while reparented onto the wielder

    void Awake()
    {
        worldItem = GetComponentInParent<WorldItem>();
        home = transform.parent;
        restLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        // never inherit the item's (or hand rig's) rotation, so the light stays upright
        transform.rotation = Quaternion.identity;

        if (worldItem == null) return;

        // the item was destroyed while we were parented to the wielder
        if (home == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!worldItem.IsHeld)
        {
            if (wielder != null)
            {
                wielder = null;
                transform.SetParent(home, worldPositionStays: false);
            }
            transform.localPosition = restLocalPosition;
            return;
        }

        if (wielder == null)
        {
            wielder = home.GetComponentInParent<Hands>();
            if (wielder == null) return;
            transform.SetParent(wielder.transform, worldPositionStays: true);
        }

        Vector3 p = wielder.transform.position;
        p.y += floorOffset;
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }
}

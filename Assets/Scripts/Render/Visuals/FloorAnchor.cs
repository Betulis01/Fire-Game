using UnityEngine;

// Pins this transform's world Y to the wielder's floor height while its WorldItem
// ancestor is held (e.g. a torch's ground pool light, which would otherwise ride
// up and down with the hand rig's animated height/rotation). Restores its authored
// local position while the item sits in the world (dropped or never picked up).
[DefaultExecutionOrder(100)] // after HeldItemRotationFilter's LateUpdate (default order 0)
public class FloorAnchor : MonoBehaviour
{
    [Tooltip("Height above the wielder's floor to sit at while held.")]
    [SerializeField] float floorOffset = 0f;

    WorldItem worldItem;
    Vector3 restLocalPosition;

    void Awake()
    {
        worldItem = GetComponentInParent<WorldItem>();
        restLocalPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (worldItem == null) return;

        if (!worldItem.IsHeld)
        {
            transform.localPosition = restLocalPosition;
            return;
        }

        Hands wielder = GetComponentInParent<Hands>();
        if (wielder == null) return;

        Vector3 p = transform.position;
        p.y = wielder.transform.position.y + floorOffset;
        transform.position = p;
    }
}

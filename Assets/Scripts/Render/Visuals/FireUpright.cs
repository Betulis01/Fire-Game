using UnityEngine;

// The torch's flame child must never tilt: the torch itself has a Tool component,
// so HeldItemRotationFilter treats it as a weapon and lets it follow the hand rig's
// swing/sway rotation. This cancels that inherited rotation every frame so the flame
// sprite always renders at 0 degrees while still following the torch's position.
[DefaultExecutionOrder(100)] // after HeldItemRotationFilter's LateUpdate (default order 0)
public class FireUpright : MonoBehaviour
{
    void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}

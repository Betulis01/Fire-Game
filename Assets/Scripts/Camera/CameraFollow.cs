using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform target;        // the player
    public Vector3 offset = new Vector3(0, 0, -10);

    [Tooltip("Seconds for the camera to catch up to the target. 0 = snap (no smoothing).")]
    public float smoothTime = 0.15f;

    Vector3 velocity;   // SmoothDamp state

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 destination = target.position + offset;
        transform.position = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime)
            : destination;
    }
}

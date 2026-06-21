using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform target;        // the player
    public Vector3 offset = new Vector3(0, 0, -10);

    [Tooltip("Seconds for the camera to catch up to the target. 0 = snap (no smoothing).")]
    public float smoothTime = 0.15f;

    Vector3 velocity;   // SmoothDamp state
    CameraShake shake;
    Camera cam;

    void Awake()
    {
        shake = GetComponent<CameraShake>();
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 destination = target.position + offset;
        Vector3 position = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime)
            : destination;

        if (WorldBounds.Instance != null && cam != null)
        {
            Vector2 clamped = WorldBounds.Instance.ClampCamera(position, cam);
            position.x = clamped.x;
            position.y = clamped.y;
        }

        // shake rides on top of the followed position (it isn't smoothed away)
        if (shake != null) position += shake.Offset;

        transform.position = position;
    }
}

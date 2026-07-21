using UnityEngine;

// Smoothly eases the camera's orthographic size toward a "zoomed in" target while a
// charged attack is fully charged (see WeaponUse, which calls SetZoomed). Runs before
// CameraFollow (DefaultExecutionOrder) so its LateUpdate sees the already-updated
// orthographicSize this same frame, since WorldBounds.ClampCamera reads it live.
[DefaultExecutionOrder(-100)]
public class CameraZoom : MonoBehaviour
{
    public static CameraZoom Instance { get; private set; }

    [Tooltip("How much smaller the orthographic size gets when zoomed in.")]
    public float zoomAmount = 1f;
    [Tooltip("Seconds to ease toward the target size.")]
    public float smoothTime = 0.2f;

    Camera cam;
    float baseSize;
    float velocity;
    bool zoomedIn;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam != null) baseSize = cam.orthographicSize;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetZoomed(bool zoomed) => zoomedIn = zoomed;

    void LateUpdate()
    {
        if (cam == null) return;

        float target = baseSize - (zoomedIn ? zoomAmount : 0f);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, target, ref velocity, smoothTime);
    }
}

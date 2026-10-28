using UnityEngine;

// The single owner of the camera's orthographic size. Smoothly eases it toward the
// base size (the player's chosen zoom level, CameraZoomControl), minus a "zoomed in"
// amount while a charged attack is fully charged (see WeaponUse, which calls
// SetZoomed). Runs before CameraFollow (DefaultExecutionOrder) so its LateUpdate sees
// the already-updated orthographicSize this same frame, since WorldBounds.ClampCamera
// reads it live.
[DefaultExecutionOrder(-100)]
public class CameraZoom : MonoBehaviour
{
    public static CameraZoom Instance { get; private set; }

    [Tooltip("How much smaller the orthographic size gets when zoomed in, at the " +
             "camera's authored size (scaled with the current zoom level).")]
    public float zoomAmount = 1f;
    [Tooltip("Seconds to ease toward the target size.")]
    public float smoothTime = 0.2f;

    Camera cam;
    float authoredSize;
    float baseSize;
    float velocity;
    bool zoomedIn;

    // The resting size the camera eases to, before the charge zoom. Starts at the
    // camera's authored orthographic size.
    public float BaseSize
    {
        get => baseSize;
        set => baseSize = Mathf.Max(0.1f, value);
    }

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam != null) authoredSize = baseSize = cam.orthographicSize;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetZoomed(bool zoomed) => zoomedIn = zoomed;

    void LateUpdate()
    {
        if (cam == null) return;

        // the charge zoom keeps the same feel at every zoom level
        float charge = zoomedIn && authoredSize > 0f ? zoomAmount * baseSize / authoredSize : 0f;
        float target = baseSize - charge;
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, target, ref velocity, smoothTime);
    }
}

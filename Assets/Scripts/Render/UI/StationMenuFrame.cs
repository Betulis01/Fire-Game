using UnityEngine;

// The Canvas root of each station's menu prefab. The menu is drawn like the HUD: a
// screen-space overlay canvas with the HUD's scaler (1920x1080 reference), so its
// pixel art comes out the same size and as crisp as the HUD's at any zoom, instead
// of being scaled with the world. The panel follows the station on screen,
// worldOffset above it, snapped to whole reference pixels. Sorts under the HUD so
// tooltips and the bag's drag icon draw over it.
// An overlay canvas undoes its parents' scale, and station roots are flattened to
// z scale 0, so on first open the canvas moves to the scene root; it keeps
// following the station and is destroyed with it (StationInteractable.OnDestroy).
[RequireComponent(typeof(Canvas))]
[DefaultExecutionOrder(1000)] // after the camera has moved (CameraFollow, CameraShake)
public class StationMenuFrame : MonoBehaviour
{
    [Tooltip("Where the panel's centre sits, in world units from the station.")]
    public Vector2 worldOffset = new(0f, 1f);

    [Tooltip("The panel that follows the station (frame + content).")]
    public RectTransform panel;

    // the station this menu belongs to (it's no longer a parent once detached)
    public StationInteractable Station { get; private set; }

    Canvas canvas;
    Transform anchor;
    Camera cam;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        Station = GetComponentInParent<StationInteractable>(true);
        anchor = Station != null ? Station.transform : transform.parent;
        transform.SetParent(null, false);
    }

    void OnEnable() => Follow();

    void LateUpdate() => Follow();

    void Follow()
    {
        if (panel == null || anchor == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(anchor.position + (Vector3)worldOffset);
        float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        panel.anchoredPosition = new Vector2(Mathf.Round(screen.x / scale), Mathf.Round(screen.y / scale));
    }
}

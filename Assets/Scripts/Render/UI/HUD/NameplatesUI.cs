using System.Collections.Generic;
using UnityEngine;

// Enemy health plates on the screen canvas: one pooled NameplateUI per registered
// Nameplate, placed over its world anchor every frame. A plate shows while its
// enemy is alive, on screen, and hurt or the player's soft target (TargetPreview).
// Its size follows the camera's zoom -- `baseSize` at the zoom the scene starts at,
// scaled by how far the camera has zoomed, clamped to minScale..maxScale -- and
// position and size are whole reference pixels so the 2px frame stays crisp.
// Sits under the rest of the HUD, so menus and tooltips draw over it.
[RequireComponent(typeof(RectTransform))]
public class NameplatesUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] Sprite frameSprite;   // bar_health_frame, 9-sliced
    [SerializeField] Sprite fillSprite;    // bar_health_fill

    [Header("Size")]
    [Tooltip("Plate size in reference pixels at the camera's starting zoom.")]
    [SerializeField] Vector2 baseSize = new(64f, 10f);
    [SerializeField] float minScale = 0.6f;
    [SerializeField] float maxScale = 1.4f;

    [Header("Hit animation")]
    [Tooltip("Seconds the white flash fades over.")]
    public float flashTime = 0.12f;
    [Tooltip("Seconds the chip holds the lost health before draining.")]
    public float chipHold = 0.3f;
    [Tooltip("How fast the chip eases down (fraction of the gap per second).")]
    public float chipEase = 8f;
    [Tooltip("Slowest the chip drains, in bar widths per second.")]
    public float chipMinSpeed = 0.6f;

    RectTransform rect;
    Camera cam;
    float homeSize;

    readonly Dictionary<Nameplate, NameplateUI> plates = new();
    readonly Stack<NameplateUI> pool = new();
    readonly List<Nameplate> gone = new();

    void Awake() => rect = (RectTransform)transform;

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
            homeSize = cam.orthographicSize;
        }

        // plates whose enemy was disabled or destroyed go back to the pool
        gone.Clear();
        foreach (Nameplate n in plates.Keys)
            if (n == null || !Nameplate.All.Contains(n)) gone.Add(n);
        foreach (Nameplate n in gone) Release(n);

        float scale = cam.orthographicSize > 0f
            ? Mathf.Clamp(homeSize / cam.orthographicSize, minScale, maxScale)
            : 1f;
        // an even width, so the bottom-centre pivot lands on a whole pixel
        Vector2 size = new(2f * Mathf.Round(baseSize.x * scale / 2f), Mathf.Round(baseSize.y * scale));
        size.y = Mathf.Max(size.y, 2 * NameplateUI.Border + 1);   // always a row of fill
        GameObject target = root != null && root.Preview != null ? root.Preview.Primary : null;
        float dt = Time.deltaTime;

        foreach (Nameplate n in Nameplate.All)
        {
            NameplateUI plate = Plate(n);
            plate.Rect.sizeDelta = size;
            plate.Tick(dt);

            Health h = n.Health;
            bool show = h != null && !h.IsDead && (h.Normalized < 1f || n.gameObject == target)
                        && Place(plate.Rect, n.Anchor);
            if (plate.gameObject.activeSelf != show) plate.gameObject.SetActive(show);
        }
    }

    // Puts the plate over `world`; false when that point is off screen.
    bool Place(RectTransform plate, Vector3 world)
    {
        Vector3 vp = cam.WorldToViewportPoint(world);
        if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;

        Vector2 screen = cam.ViewportToScreenPoint(vp);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out Vector2 local))
            return false;
        plate.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
        return true;
    }

    NameplateUI Plate(Nameplate n)
    {
        if (plates.TryGetValue(n, out NameplateUI plate)) return plate;

        if (pool.Count > 0) plate = pool.Pop();
        else
        {
            var go = new GameObject("Nameplate", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            plate = go.AddComponent<NameplateUI>();
            plate.Build(this, frameSprite, fillSprite);
        }
        plate.name = "Nameplate (" + n.name + ")";
        plate.Bind(n);
        plates[n] = plate;
        return plate;
    }

    void Release(Nameplate n)
    {
        NameplateUI plate = plates[n];
        plates.Remove(n);
        plate.Bind(null);
        plate.gameObject.SetActive(false);
        pool.Push(plate);
    }
}

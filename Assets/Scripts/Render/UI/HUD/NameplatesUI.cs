using System.Collections.Generic;
using UnityEngine;

// Enemy health plates on the screen canvas: one pooled NameplateUI per registered
// Nameplate, placed over its world anchor every frame. A plate shows while its
// enemy is alive, on screen, and hurt or the player's soft target (TargetPreview).
// Its size is fixed on screen (`baseSize`, whatever the camera's zoom), and its
// position is whole reference pixels, so the 2px frame stays crisp.
// Sits under the rest of the HUD, so menus and tooltips draw over it.
[RequireComponent(typeof(RectTransform))]
public class NameplatesUI : MonoBehaviour
{
    [SerializeField] HudRoot root;

    [Header("Size")]
    [Tooltip("Plate size in reference pixels. Keep the width even, so the bottom-centre pivot lands on a whole pixel.")]
    [SerializeField] Vector2 baseSize = new(64f, 10f);

    [Header("Colours (apply live)")]
    public Color frameColor = new(64f / 255f, 82f / 255f, 115f / 255f);     // the player bar's frame blue
    [Tooltip("Behind the fill: the empty part of the bar. Transparent shows the world through it.")]
    public Color backgroundColor = new(0f, 0f, 0f, 0f);
    public Color fillColor = new(181f / 255f, 89f / 255f, 69f / 255f);      // the player bar's red
    [Tooltip("The health just lost, holding then draining.")]
    public Color chipColor = new(241f / 255f, 226f / 255f, 180f / 255f);
    [Tooltip("Over the fill for a moment on each hit (its alpha fades out over flashTime).")]
    public Color flashColor = Color.white;

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

    readonly Dictionary<Nameplate, NameplateUI> plates = new();
    readonly Stack<NameplateUI> pool = new();
    readonly List<Nameplate> gone = new();

    void Awake() => rect = (RectTransform)transform;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // plates whose enemy was disabled or destroyed go back to the pool
        gone.Clear();
        foreach (Nameplate n in plates.Keys)
            if (n == null || !Nameplate.All.Contains(n)) gone.Add(n);
        foreach (Nameplate n in gone) Release(n);

        Vector2 size = new(Mathf.Round(baseSize.x), Mathf.Max(Mathf.Round(baseSize.y), 2 * NameplateUI.Border + 1));
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
            plate.Build(this);
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

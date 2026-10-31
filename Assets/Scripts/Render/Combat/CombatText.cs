using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Floating combat numbers on the HUD canvas (a stretched layer under the HUD root,
// above the nameplates). It registers itself as the singleton and pools its popups
// (DamagePopup, built in code in the Tiny5 pixel font with a 1px outline). Anything
// wanting a number on screen calls CombatText.Spawn(worldPos, amount, style).
// A popup rises from the world point it was spawned at, re-placed over it every
// frame, so it stays on the hit spot as the camera pans and zooms; its size is fixed
// on screen. Font sizes are Tiny5's: 8 = one screen pixel per font pixel at 1080p.
// Popups never overlap: a new one stays at its spawn point and any live popup it
// would cover is pushed up above it (cascading), so the newest number sits at the
// hit and earlier ones stack over it.
[RequireComponent(typeof(RectTransform))]
public class CombatText : MonoBehaviour
{
    public static CombatText Instance { get; private set; }

    [SerializeField] TMP_FontAsset font;          // Tiny5 Pixel
    [SerializeField] Material fontMaterial;       // its material
    public int prewarm = 16;                      // popups pooled up front

    [Header("Motion")]
    [Tooltip("Seconds until a popup returns to the pool.")]
    public float lifetime = 0.7f;
    [Tooltip("Reference pixels climbed per second.")]
    public float riseSpeed = 100f;
    public AnimationCurve alpha = new(new Keyframe(0.1f, 1f, 0f, 0f), new Keyframe(0.12f, 0.95f, 0f, 0f));
    [Tooltip("Reference pixels left between stacked popups.")]
    public float stackGap = 1f;

    [Header("Font size (8 = 1x)")]
    public float damageSize = 48f;
    public float critSize = 64f;
    public float experienceSize = 32f;

    [Header("Colours")]
    public Color environmentDmgColor = new(0f, 0.676517f, 1f);     // fire / cold, any target
    public Color enemyDmgColor = new(1f, 0.6401813f, 0f);         // damage dealt to an enemy
    public Color playerDmgColor = new(1f, 0f, 0f);                // damage dealt to the player
    public Color enemyHealColor = new(0.2f, 0.9f, 0.3f);          // healing on an enemy
    public Color playerHealColor = new(0.2f, 0.9f, 0.3f);         // healing on the player
    public Color experienceColor = new(0.66f, 0.42f, 0.95f);      // XP gained (purple)

    RectTransform rect;
    Camera cam;
    readonly Queue<DamagePopup> pool = new();
    readonly List<DamagePopup> live = new();
    readonly List<DamagePopup> settled = new();   // Separate's scratch lists
    readonly List<DamagePopup> others = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        rect = (RectTransform)transform;

        for (int i = 0; i < prewarm; i++) Release(Create());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // A number; a crit shows bigger with a "!".
    public static void Spawn(Vector3 worldPos, float amount, PopupStyle style, bool heal = false, bool crit = false)
    {
        if (Instance == null) return;
        string text = Mathf.RoundToInt(amount) + (crit ? "!" : "");
        Instance.Show(worldPos, text, Instance.ColorFor(style, heal), Instance.SizeFor(style, crit));
    }

    // Free-form text instead of a number (e.g. "+2" XP).
    public static void SpawnText(Vector3 worldPos, string text, PopupStyle style)
    {
        if (Instance == null) return;
        Instance.Show(worldPos, text, Instance.ColorFor(style, false), Instance.SizeFor(style, false));
    }

    Color ColorFor(PopupStyle style, bool heal) => heal
        ? (style == PopupStyle.Player ? playerHealColor : enemyHealColor)
        : style switch
        {
            PopupStyle.Player => playerDmgColor,
            PopupStyle.Environment => environmentDmgColor,
            PopupStyle.Experience => experienceColor,
            _ => enemyDmgColor,
        };

    float SizeFor(PopupStyle style, bool crit) =>
        style == PopupStyle.Experience ? experienceSize : crit ? critSize : damageSize;

    void Show(Vector3 worldPos, string text, Color color, float size)
    {
        if (!FindCamera()) return;
        DamagePopup popup = pool.Count > 0 ? pool.Dequeue() : Create();
        popup.gameObject.SetActive(true);
        live.Add(popup);
        popup.Play(worldPos, text, color, size);
        Place(popup);
        Separate(popup);
    }

    void LateUpdate()
    {
        if (!FindCamera()) return;
        float dt = Time.deltaTime;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            DamagePopup p = live[i];
            if (p.Tick(dt)) Place(p);
            else Release(p);
        }
    }

    bool FindCamera()
    {
        if (cam == null) cam = Camera.main;
        return cam != null;
    }

    // Over its world point, raised by its rise and stacking lift, on a whole pixel.
    void Place(DamagePopup p)
    {
        Vector2 screen = cam.WorldToScreenPoint(p.WorldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out Vector2 local);
        local.y += p.Offset;
        p.Rect.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
    }

    // Push every live popup that overlaps `fresh` (or one already pushed) up above
    // it, lowest first, so the stack keeps its order.
    void Separate(DamagePopup fresh)
    {
        settled.Clear();
        settled.Add(fresh);
        others.Clear();
        foreach (DamagePopup p in live)
            if (p != fresh) others.Add(p);
        others.Sort((a, b) => a.Box.yMin.CompareTo(b.Box.yMin));

        foreach (DamagePopup p in others)
        {
            // re-check from the bottom after each lift: a lift can land on another
            for (int i = 0; i < settled.Count; i++)
            {
                Rect a = p.Box, b = settled[i].Box;
                bool overlapX = a.xMin < b.xMax && b.xMin < a.xMax;
                bool overlapY = a.yMin < b.yMax + stackGap && b.yMin < a.yMax;
                if (!overlapX || !overlapY) continue;
                p.Lift += Mathf.Ceil(b.yMax + stackGap - a.yMin);
                Place(p);
                i = -1;
            }
            settled.Add(p);
        }
    }

    DamagePopup Create()
    {
        var go = new GameObject("Popup", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Build(this, font, fontMaterial);
        return popup;
    }

    void Release(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        live.Remove(popup);
        pool.Enqueue(popup);
    }
}

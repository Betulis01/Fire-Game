using TMPro;
using UnityEngine;

// The HUD layer interact prompts live on (a stretched child of the HUD, over the
// nameplates and combat numbers, under the stats and menus), and the one place
// their look is tuned: every InteractPrompt reads these settings each time it's
// shown, so changes apply live. A prompt moves itself under Instance the first time
// it's shown and asks Place to sit over its world point each frame, on whole
// reference pixels, so prompts stay crisp and a fixed size at any camera zoom.
[RequireComponent(typeof(RectTransform))]
public class PromptLayer : MonoBehaviour
{
    public static PromptLayer Instance { get; private set; }

    public TMP_FontAsset font;          // Tiny5 Pixel
    public Material fontMaterial;       // its material

    [Header("Icon")]
    [Tooltip("Screen pixels per key-icon pixel (at the 1920x1080 reference).")]
    public int iconScale = 4;

    [Header("Label")]
    [Tooltip("Tiny5 font size: 8 per screen pixel per font pixel (32 = 4x).")]
    public float labelSize = 32f;
    public Color labelColor = new(1f, 0.5905554f, 0f);
    [Tooltip("Reference pixels between the icon's top and the label.")]
    public float labelGap = 4f;

    [Header("Hold bar")]
    [Tooltip("Colour of the Hold E fill bar under the icon.")]
    public Color holdColor = Color.white;

    RectTransform rect;
    Camera cam;

    void Awake()
    {
        Instance = this;
        rect = (RectTransform)transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Puts `item`'s pivot over `world`; false when there's no camera to place by.
    public bool Place(RectTransform item, Vector3 world)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        Vector2 screen = cam.WorldToScreenPoint(world);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out Vector2 local))
            return false;
        item.anchoredPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
        return true;
    }
}

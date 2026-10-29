using TMPro;
using UnityEngine;

// A single reusable world-space prompt that PlayerInteractor moves over
// whichever interactable is currently in focus. Attach to a prefab that has
// both a SpriteRenderer (the "E" icon) and a TMP_Text child for optional text.
// SetHold draws a thin bar under the icon that fills while E is held (Hold E to
// equip); the bar is made on first use, so prompt prefabs need nothing for it.
public class InteractPrompt : MonoBehaviour
{
    [Tooltip("Optional TMP_Text child for dynamic labels (e.g. 'Drop Stone (0/1)').")]
    public TMP_Text label;

    SpriteRenderer sr;
    public TMP_Text pressString;

    [Tooltip("Colour of the Hold E fill bar under the icon.")]
    [SerializeField] Color holdColor = Color.white;

    SpriteRenderer holdBar;
    float holdWidth;          // the bar's full width: the icon's
    static Sprite pixel;      // shared 1x1 white sprite, one art pixel tall at 32 PPU

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        Hide();
    }

    public void Show(Vector3 worldPos)
    {
        transform.position = worldPos;
        if (!sr.enabled) sr.enabled = true;
        if (label != null) label.enabled = true;
        if (pressString != null) pressString.enabled = true;
    }

    public void Show(Vector3 worldPos, string text)
    {
        if (label != null)
        {
            label.text = text;
            label.enabled = true;
        }
        Show(worldPos);
    }

    // 0 hides the bar; 0..1 fills it left to right.
    public void SetHold(float progress01)
    {
        if (progress01 <= 0f)
        {
            if (holdBar != null) holdBar.enabled = false;
            return;
        }
        if (holdBar == null) CreateHoldBar();
        holdBar.enabled = sr.enabled;
        holdBar.transform.localScale = new Vector3(holdWidth * Mathf.Clamp01(progress01) * 32f, 1f, 1f);
    }

    void CreateHoldBar()
    {
        if (pixel == null)
        {
            Texture2D tex = new(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 32f);
        }

        var go = new GameObject("Hold Bar");
        go.transform.SetParent(sr.transform, false);
        holdBar = go.AddComponent<SpriteRenderer>();
        holdBar.sprite = pixel;
        holdBar.color = holdColor;
        holdBar.sharedMaterial = sr.sharedMaterial;
        holdBar.sortingLayerID = sr.sortingLayerID;
        holdBar.sortingOrder = sr.sortingOrder + 1;

        // under the icon, two art pixels below its bottom edge, as wide as it is
        Bounds b = sr.sprite != null ? sr.sprite.bounds : new Bounds(Vector3.zero, Vector3.one * 0.5f);
        holdWidth = b.size.x;
        go.transform.localPosition = new Vector3(b.min.x, b.min.y - 2f / 32f, 0f);
    }

    public void Hide()
    {
        if (holdBar != null) holdBar.enabled = false;
        if (sr.enabled) sr.enabled = false;
        if (label != null && label.enabled) label.enabled = false;
        if (pressString != null && pressString.enabled) pressString.enabled = false;
    }
}

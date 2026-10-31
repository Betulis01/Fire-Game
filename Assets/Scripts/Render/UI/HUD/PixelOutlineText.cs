using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

// 1px black outline for pixel-font HUD text. The pixel font is a raster (bitmap)
// TMP font, which can't use TMP's SDF outline, so this draws four black copies of
// the label offset by one reference pixel (left/right/up/down), as siblings just
// behind it. The copies follow the label's text, size, layout, alpha and visibility
// (a pooled label that changes size or fades -- CombatText's popups -- keeps its
// outline in step).
// Copies are spawned at runtime only, so they never get saved into the prefab.
[RequireComponent(typeof(TMP_Text))]
public class PixelOutlineText : MonoBehaviour
{
    [SerializeField] Color outlineColor = Color.black;

    static readonly Vector2[] Offsets = { new(-1, 0), new(1, 0), new(0, 1), new(0, -1) };
    // rich-text colour tags in the label would tint the copies; they're stripped
    static readonly Regex ColorTags = new(@"</?color[^>]*>", RegexOptions.IgnoreCase);

    TMP_Text label;
    TMP_Text[] copies;
    string lastText;
    float lastSize = -1f;
    float lastAlpha = -1f;

    void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        if (copies == null) CreateCopies();
        foreach (TMP_Text c in copies) c.gameObject.SetActive(true);
        Sync(force: true);
    }

    void OnDisable()
    {
        if (copies == null) return;
        foreach (TMP_Text c in copies)
            if (c != null) c.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (copies == null) return;
        foreach (TMP_Text c in copies)
            if (c != null) Destroy(c.gameObject);
    }

    void LateUpdate()
    {
        if (copies != null) Sync(force: false);
    }

    void CreateCopies()
    {
        copies = new TMP_Text[Offsets.Length];
        for (int i = 0; i < Offsets.Length; i++)
        {
            var go = new GameObject(name + " Outline", typeof(RectTransform));
            go.transform.SetParent(transform.parent, false);
            go.transform.SetSiblingIndex(transform.GetSiblingIndex());   // behind the label
            TMP_Text copy = label is TextMeshProUGUI ? go.AddComponent<TextMeshProUGUI>() : go.AddComponent<TextMeshPro>();
            copy.font = label.font;
            copy.fontSharedMaterial = label.fontSharedMaterial;
            copy.raycastTarget = false;
            copy.color = outlineColor;
            copies[i] = copy;
        }
    }

    void Sync(bool force)
    {
        var src = (RectTransform)transform;
        bool changed = force || label.text != lastText || !Mathf.Approximately(label.fontSize, lastSize);
        bool faded = force || !Mathf.Approximately(label.alpha, lastAlpha);

        for (int i = 0; i < copies.Length; i++)
        {
            TMP_Text c = copies[i];
            var rt = (RectTransform)c.transform;
            rt.anchorMin = src.anchorMin;
            rt.anchorMax = src.anchorMax;
            rt.pivot = src.pivot;
            rt.sizeDelta = src.sizeDelta;
            rt.anchoredPosition = src.anchoredPosition + Offsets[i];

            if (faded) c.alpha = label.alpha;
            if (!changed) continue;
            c.text = ColorTags.Replace(label.text, "");
            c.fontSize = label.fontSize;
            c.alignment = label.alignment;
            c.textWrappingMode = label.textWrappingMode;
            c.overflowMode = label.overflowMode;
            c.characterSpacing = label.characterSpacing;
        }
        lastText = label.text;
        lastSize = label.fontSize;
        lastAlpha = label.alpha;
    }
}

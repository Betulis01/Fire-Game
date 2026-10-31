using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A reusable interact prompt -- a key icon with an optional label above it -- that
// PlayerInteractor / ItemReceiver show over whatever is in focus. It's drawn on the
// HUD canvas (PromptLayer): the first time it's shown it moves itself onto the layer
// and builds its parts, and each frame it re-places itself over the world point it
// was shown at, so it's crisp and a fixed size at any zoom.
// The prefab only picks the key icon; the look (scale, label size, colours, gap) is
// shared by every prompt and tuned on the HUD's PromptLayer, re-read on every Show.
//  - icon: the key sprite at iconScale screen pixels per art pixel, centred on the point
//  - label: Tiny5, 1px outlined, bottom-centred labelGap above the icon, so a second
//    line (Hold E: Equip) stacks upward
//  - hold bar (SetHold): one icon pixel tall, two under the icon, as wide as it is
[RequireComponent(typeof(RectTransform))]
public class InteractPrompt : MonoBehaviour
{
    [SerializeField] Sprite icon;

    Image iconImage;
    TextMeshProUGUI label;
    RectTransform holder;     // the hold bar's track, as wide as the icon
    RectTransform holdBar;
    Image holdImage;
    Vector3 worldPoint;

    void Awake() => Hide();

    void Build(PromptLayer layer)
    {
        var rect = (RectTransform)transform;
        rect.SetParent(layer.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        iconImage = Part<Image>("Icon", rect);
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        // hold bar: a plain quad (no sprite), left-anchored inside its track
        holder = Part<RectTransform>("Hold Bar", rect);
        holder.pivot = new Vector2(0f, 1f);
        holdImage = Part<Image>("Fill", holder);
        holdBar = holdImage.rectTransform;
        holdBar.anchorMin = Vector2.zero;
        holdBar.anchorMax = new Vector2(0f, 1f);
        holdBar.pivot = new Vector2(0f, 0.5f);
        holdBar.offsetMin = holdBar.offsetMax = Vector2.zero;
        holder.gameObject.SetActive(false);

        // label above the icon: bottom-centred, lines stacking upward
        label = Part<TextMeshProUGUI>("Label", rect);
        label.font = layer.font;
        if (layer.fontMaterial != null) label.fontSharedMaterial = layer.fontMaterial;
        label.alignment = TextAlignmentOptions.Bottom;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.gameObject.AddComponent<PixelOutlineText>();   // after the font: its copies take the label's
    }

    // Size and colour everything from the layer's settings (cheap when unchanged).
    void Layout(PromptLayer layer)
    {
        int scale = Mathf.Max(1, layer.iconScale);

        // icon, centred on the point; offsets keep odd sizes' edges on whole pixels
        Vector2 iconSize = icon != null ? icon.rect.size * scale : Vector2.zero;
        float iconTop = Mathf.Round(iconSize.y / 2f);
        float iconLeft = Mathf.Round(iconSize.x / 2f) - iconSize.x;
        iconImage.rectTransform.sizeDelta = iconSize;
        iconImage.rectTransform.anchoredPosition = new Vector2(iconLeft + iconSize.x / 2f, iconTop - iconSize.y / 2f);

        holder.sizeDelta = new Vector2(iconSize.x, scale);
        holder.anchoredPosition = new Vector2(iconLeft, iconTop - iconSize.y - 2 * scale);
        holdImage.color = layer.holdColor;

        label.fontSize = layer.labelSize;
        label.color = layer.labelColor;
        label.rectTransform.sizeDelta = new Vector2(1024f, layer.labelSize);   // even width: centred lines stay on whole pixels
        label.rectTransform.anchoredPosition = new Vector2(0f, iconTop + layer.labelGap);
    }

    static T Part<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        T part = typeof(T) == typeof(RectTransform) ? (T)(Component)rt : go.AddComponent<T>();
        if (part is Graphic g) g.raycastTarget = false;
        return part;
    }

    void LateUpdate()
    {
        if (PromptLayer.Instance != null) PromptLayer.Instance.Place((RectTransform)transform, worldPoint);
    }

    public void Show(Vector3 worldPos) => Show(worldPos, null);

    // `text` null keeps the current label.
    public void Show(Vector3 worldPos, string text)
    {
        worldPoint = worldPos;
        PromptLayer layer = PromptLayer.Instance;
        if (layer == null) return;   // no HUD to draw on (callers may show before the HUD wakes)
        if (label == null) Build(layer);
        Layout(layer);
        if (text != null) label.text = text;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        layer.Place((RectTransform)transform, worldPoint);
    }

    // 0 hides the bar; 0..1 fills it left to right, in whole pixels.
    public void SetHold(float progress01)
    {
        if (holder == null) return;
        if (progress01 <= 0f)
        {
            if (holder.gameObject.activeSelf) holder.gameObject.SetActive(false);
            return;
        }
        if (!holder.gameObject.activeSelf) holder.gameObject.SetActive(true);
        float width = holder.sizeDelta.x;
        float filled = width > 0f ? Mathf.Round(Mathf.Clamp01(progress01) * width) / width : 0f;
        holdBar.anchorMax = new Vector2(filled, 1f);
    }

    public void Hide()
    {
        if (holder != null) holder.gameObject.SetActive(false);
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}

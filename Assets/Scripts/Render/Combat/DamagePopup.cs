using TMPro;
using UnityEngine;

public enum PopupStyle { Enemy, Player, Environment, Experience }

// One floating combat number (or short text, e.g. "+2" XP) on the HUD canvas, built
// and pooled by CombatText, which also holds the tuning. It rises from a fixed world
// point (the hit spot) and fades over CombatText.lifetime.
// Layout: this is a container (pivot = the spawn point) whose CanvasGroup fades the
// label and its PixelOutlineText copies together; the label is a Tiny5 TextMeshProUGUI
// placed top-left on whole reference pixels, so every font pixel lands on whole
// screen pixels like the rest of the HUD text.
[RequireComponent(typeof(RectTransform))]
public class DamagePopup : MonoBehaviour
{
    CombatText owner;
    TextMeshProUGUI label;
    CanvasGroup group;
    float elapsed;
    float rise;          // reference pixels climbed so far
    Rect box;            // the glyphs (plus the outline), relative to this container's pivot

    public Vector3 WorldPoint { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    // Reference pixels this popup was pushed up to clear newer ones (CombatText's stacking).
    public float Lift { get; set; }

    // How far above its world point it sits now.
    public float Offset => rise + Lift;

    // Its glyph box on the canvas, at its current position.
    public Rect Box => new(Rect.anchoredPosition + box.position, box.size);

    public void Build(CombatText home, TMP_FontAsset font, Material material)
    {
        owner = home;
        RectTransform rect = Rect;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = Vector2.zero;

        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(rect, false);
        var lrt = (RectTransform)go.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0f, 1f);

        label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        if (material != null) label.fontSharedMaterial = material;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        go.AddComponent<PixelOutlineText>();   // after the font: its copies take the label's
    }

    public void Play(Vector3 worldPoint, string text, Color color, float fontSize)
    {
        WorldPoint = worldPoint;
        elapsed = 0f;
        rise = 0f;
        Lift = 0f;
        group.alpha = 1f;

        label.text = text;
        label.fontSize = fontSize;
        label.color = color;

        // centred over the spawn point, but top-left on a whole pixel
        Vector2 size = label.GetPreferredValues(text);
        size = new Vector2(Mathf.Ceil(size.x), Mathf.Ceil(size.y));
        RectTransform lrt = label.rectTransform;
        lrt.sizeDelta = size;
        lrt.anchoredPosition = new Vector2(-Mathf.Round(size.x / 2f), size.y);

        // the drawn glyphs, not the line box (no descender gap when stacking)
        label.ForceMeshUpdate();
        Bounds b = label.textBounds;
        Vector2 min = lrt.anchoredPosition + (Vector2)b.min - Vector2.one;   // + the 1px outline
        box = new Rect(min, (Vector2)b.size + 2f * Vector2.one);
    }

    // Advance the rise and fade; false once its lifetime is over.
    public bool Tick(float dt)
    {
        elapsed += dt;
        if (elapsed >= owner.lifetime) return false;
        rise += owner.riseSpeed * dt;
        group.alpha = owner.alpha.Evaluate(elapsed / owner.lifetime);
        return true;
    }
}

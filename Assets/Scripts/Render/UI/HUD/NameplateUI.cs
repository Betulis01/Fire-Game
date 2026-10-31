using UnityEngine;
using UnityEngine.UI;

// One enemy's health plate on the HUD (pooled by NameplatesUI): the player's health
// bar look, small -- a 2px frame around a fill -- built from plain colour quads, so
// every part's colour is NameplatesUI's to set (re-read each frame, so it tunes
// live). Polls the enemy's Health each frame. A hit drops the fill at once and
// flashes it; a chip
// keeps showing the health just lost for `chipHold`, then drains down to the fill.
// Hits during the hold or drain restart the hold, so a flurry reads as one trail.
// Healing moves the fill and chip together (no chip).
// Every layer's width is its anchorMax.x, snapped to whole pixels.
public class NameplateUI : MonoBehaviour
{
    public const int Border = 2;   // the frame's border, in reference pixels

    RectTransform chip, fill, flash;
    Image[] frame;
    Image background, chipImage, fillImage, flashImage;
    NameplatesUI layer;

    float shown;        // health the fill shows, 0..1
    float chipValue;    // health the chip shows, >= shown
    float holdLeft;     // seconds until the chip starts draining
    float flashLeft;    // seconds of white flash left

    public Nameplate Target { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    // Builds the plate's layers under this (a fresh RectTransform).
    public void Build(NameplatesUI owner)
    {
        layer = owner;
        RectTransform rect = Rect;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);

        // the frame: four edges, the sides between the top and bottom so no corner overlaps
        frame = new[]
        {
            Edge("Top", rect, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -Border), Vector2.zero),
            Edge("Bottom", rect, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, Border)),
            Edge("Left", rect, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, Border), new Vector2(Border, -Border)),
            Edge("Right", rect, new Vector2(1f, 0f), Vector2.one, new Vector2(-Border, Border), new Vector2(0f, -Border)),
        };

        RectTransform inner = Stretched("Inner", rect);
        inner.offsetMin = new Vector2(Border, Border);
        inner.offsetMax = new Vector2(-Border, -Border);

        background = Layer("Background", inner);
        chipImage = Layer("Chip", inner);
        chip = chipImage.rectTransform;
        fillImage = Layer("Fill", inner);
        fill = fillImage.rectTransform;
        flashImage = Layer("Flash", inner);
        flash = flashImage.rectTransform;
    }

    static Image Edge(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image image = Layer(name, parent);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return image;
    }

    static RectTransform Stretched(string name, Transform parent)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // a plain colour quad (no sprite) filling `parent`
    static Image Layer(string name, Transform parent)
    {
        Image image = Stretched(name, parent).gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    // Follow `target`, starting from its current health (no stale chip).
    public void Bind(Nameplate target)
    {
        Target = target;
        float n = target != null && target.Health != null ? target.Health.Normalized : 1f;
        shown = chipValue = n;
        holdLeft = flashLeft = 0f;
        Draw();
    }

    // Polls the health and advances the hit animation; call once a frame.
    public void Tick(float dt)
    {
        float n = Target != null && Target.Health != null ? Mathf.Clamp01(Target.Health.Normalized) : 0f;

        if (n < shown)
        {
            chipValue = Mathf.Max(chipValue, shown);   // a hit mid-trail keeps the older top
            holdLeft = layer.chipHold;
            flashLeft = layer.flashTime;
        }
        shown = n;

        if (chipValue <= shown) chipValue = shown;   // healed past it: no chip
        else if (holdLeft > 0f) holdLeft -= dt;
        else
        {
            // eases out, but never slower than chipMinSpeed so small chips finish too
            float step = Mathf.Max((chipValue - shown) * layer.chipEase, layer.chipMinSpeed) * dt;
            chipValue = Mathf.Max(shown, chipValue - step);
        }

        if (flashLeft > 0f) flashLeft = Mathf.Max(0f, flashLeft - dt);
        Draw();
    }

    void Draw()
    {
        SetWidth(fill, shown);
        SetWidth(chip, chipValue);
        SetWidth(flash, shown);
        if (layer == null) return;

        foreach (Image edge in frame) edge.color = layer.frameColor;
        background.color = layer.backgroundColor;
        background.enabled = layer.backgroundColor.a > 0f;
        chipImage.color = layer.chipColor;
        fillImage.color = layer.fillColor;

        float a = layer.flashTime > 0f ? flashLeft / layer.flashTime : 0f;
        Color f = layer.flashColor;
        flashImage.color = new Color(f.r, f.g, f.b, f.a * a);
        flashImage.enabled = a > 0f;
    }

    // `n` of the inner width, snapped to whole reference pixels so edges stay crisp
    void SetWidth(RectTransform rt, float n)
    {
        float w = Rect.sizeDelta.x - 2 * Border;
        rt.anchorMax = new Vector2(w > 0f ? Mathf.Round(n * w) / w : n, 1f);
    }
}

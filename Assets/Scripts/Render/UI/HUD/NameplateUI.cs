using UnityEngine;
using UnityEngine.UI;

// One enemy's health plate on the HUD (pooled by NameplatesUI): the player's health
// bar art, small -- the 9-sliced frame around a stretched fill. Polls the enemy's
// Health each frame. A hit drops the fill at once and flashes it white; a pale chip
// keeps showing the health just lost for `chipHold`, then drains down to the fill.
// Hits during the hold or drain restart the hold, so a flurry reads as one trail.
// Healing moves the fill and chip together (no chip).
// Every layer's width is its anchorMax.x, so the solid sprites stretch cleanly.
public class NameplateUI : MonoBehaviour
{
    public const int Border = 2;   // the frame's border, in reference pixels

    public static readonly Color ChipColor = new(241f / 255f, 226f / 255f, 180f / 255f);

    RectTransform chip, fill, flash;
    Image flashImage;
    NameplatesUI layer;

    float shown;        // health the fill shows, 0..1
    float chipValue;    // health the chip shows, >= shown
    float holdLeft;     // seconds until the chip starts draining
    float flashLeft;    // seconds of white flash left

    public Nameplate Target { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    // Builds the plate's layers under this (a fresh RectTransform).
    public void Build(NameplatesUI owner, Sprite frameSprite, Sprite fillSprite)
    {
        layer = owner;
        RectTransform rect = Rect;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);

        Image frame = gameObject.AddComponent<Image>();
        frame.sprite = frameSprite;
        frame.type = Image.Type.Sliced;
        frame.raycastTarget = false;

        RectTransform inner = Stretched("Inner", rect);
        inner.offsetMin = new Vector2(Border, Border);
        inner.offsetMax = new Vector2(-Border, -Border);

        chip = Layer("Chip", inner, null, ChipColor).rectTransform;
        fill = Layer("Fill", inner, fillSprite, Color.white).rectTransform;
        flashImage = Layer("Flash", inner, null, Color.white);
        flash = flashImage.rectTransform;
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

    // a solid layer (no sprite = a plain colour quad) filling the inner rect
    static Image Layer(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = Stretched(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
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
        float a = layer != null && layer.flashTime > 0f ? flashLeft / layer.flashTime : 0f;
        flashImage.color = new Color(1f, 1f, 1f, a);
        flashImage.enabled = a > 0f;
    }

    // `n` of the inner width, snapped to whole reference pixels so edges stay crisp
    void SetWidth(RectTransform rt, float n)
    {
        float w = Rect.sizeDelta.x - 2 * Border;
        rt.anchorMax = new Vector2(w > 0f ? Mathf.Round(n * w) / w : n, 1f);
    }
}

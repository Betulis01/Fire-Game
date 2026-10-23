using UnityEngine;
using UnityEngine.UI;

// Pixel-art helpers for HUD images. The canvas is laid out in 1920x1080 reference
// pixels; item sprites are drawn at a whole-number multiple of their own pixel size
// so they stay crisp (a 16px icon in a 64px slot draws at x3, not x3.75).
public static class UiPixel
{
    // Show `sprite` on `image` at the largest whole-number scale that fits `box`
    // (scaled down to fit if it's bigger than the box). Hides the image for null.
    public static void FitIntegerScale(Image image, Sprite sprite, Vector2 box)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
        if (sprite == null) return;

        Vector2 size = sprite.rect.size;
        float fit = Mathf.Min(box.x / size.x, box.y / size.y);
        float scale = fit >= 1f ? Mathf.Floor(fit) : fit;
        image.rectTransform.sizeDelta = size * scale;
    }

    // The sprite a held/worn item shows as: its definition's icon, else the world
    // object's own sprite.
    public static Sprite IconOf(GameObject item)
    {
        if (item == null) return null;
        if (item.TryGetComponent(out WorldItem wi) && wi.item != null)
        {
            Sprite icon = wi.item.ResolveIcon();
            if (icon != null) return icon;
        }
        SpriteRenderer sr = item.GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}

using UnityEngine;
using UnityEngine.UI;

// A radial-fill meter: give it a 0..1 value and it fills its Image around the
// circle, tinting from emptyColor to fullColor. The sprite comes from the
// Image, so art can swap the placeholder disc for a proper ring sprite
// without touching code.
[RequireComponent(typeof(Image))]
public class RadialGauge : MonoBehaviour
{
    public Color fullColor = Color.white;
    public Color emptyColor = Color.white;

    Image image;

    public void Set(float value01)
    {
        if (image == null) image = GetComponent<Image>();
        float v = Mathf.Clamp01(value01);
        image.fillAmount = v;
        image.color = Color.Lerp(emptyColor, fullColor, v);
    }
}

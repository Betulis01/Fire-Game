using UnityEngine;

// Drives a sprite-based warmth bar: scales the fill Transform's X to match
// PlayerTemperature.Normalized. Mirrors HealthBar exactly but reads from warmth.
// The fill sprite must pivot on its LEFT edge so it shrinks toward the left.
public class WarmthBar : MonoBehaviour
{
    public PlayerTemperature temperature;  // source of truth; auto-found in parents if left empty
    public Transform fill;                 // the foreground bar whose X scale we drive
    public Transform background;           // the bar's backing sprite, hidden alongside fill
    public bool hideWhenFull = false;

    float baseScaleY;
    float baseScaleZ;
    float lastNormalized = -1f;

    void Awake()
    {
        if (fill != null)
        {
            baseScaleY = fill.localScale.y;
            baseScaleZ = fill.localScale.z;
        }
        if (temperature == null) temperature = GetComponentInParent<PlayerTemperature>();
    }

    void LateUpdate()
    {
        if (temperature == null || fill == null) return;

        float n = temperature.Normalized;
        if (Mathf.Approximately(n, lastNormalized)) return;
        lastNormalized = n;

        fill.localScale = new Vector3(n, baseScaleY, baseScaleZ);

        if (hideWhenFull)
        {
            bool visible = n < 1f;
            fill.gameObject.SetActive(visible);
            if (background != null) background.gameObject.SetActive(visible);
        }
    }
}

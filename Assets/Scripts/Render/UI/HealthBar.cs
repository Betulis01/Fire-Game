using UnityEngine;

// Drives a sprite-based health bar: scales the fill Transform's X to match a Health
// component's Normalized value. Meant to sit on enemies (or any entity). Built from
// plain SpriteRenderers (no Canvas) so it shares the sprite sorting / Y-sort system.
// The fill sprite must pivot on its LEFT edge so it shrinks toward the left.
public class HealthBar : MonoBehaviour
{
    public Health health;             // source of truth; auto-found in parents if left empty
    public Transform fill;            // the foreground bar whose X scale we drive
    public Transform background;      // the bar's backing sprite, hidden alongside fill
    public bool hideWhenFull = false; // optionally hide the whole bar at full health

    float baseScaleY;                 // fill's authored Y/Z scale, preserved while we drive X
    float baseScaleZ;
    float lastNormalized = -1f;       // skip redraws when nothing changed

    void Awake()
    {
        if (fill != null)
        {
            baseScaleY = fill.localScale.y;
            baseScaleZ = fill.localScale.z;
        }
        if (health == null) health = GetComponentInParent<Health>();

        // A Canvas-parented HUD bar (e.g. the player's) isn't a descendant of its
        // target, so GetComponentInParent can't find it -- fall back to the tagged
        // Player. A no-op for enemy bars, which already found it above.
        if (health == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) health = player.GetComponent<Health>();
        }
    }

    // Poll each frame so the bar tracks any change (damage and healing alike).
    void LateUpdate()
    {
        if (health == null || fill == null) return;

        float n = health.Normalized;
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

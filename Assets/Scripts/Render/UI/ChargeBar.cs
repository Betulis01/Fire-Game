using UnityEngine;

// Drives a sprite-based charge meter: scales the fill Transform's X to match
// ToolUser.ChargeProgress (0 at press, 1 once chargeTime has been held). Mirrors
// HealthBar/WarmthBar exactly but reads from ToolUser and, unlike them, defaults to
// hidden whenever nothing is being charged rather than showing an idle empty bar.
// The fill sprite must pivot on its LEFT edge so it grows from the left.
public class ChargeBar : MonoBehaviour
{
    public ToolUser toolUser;         // source of truth; auto-found in parents if left empty
    public Transform fill;            // the foreground bar whose X scale we drive
    public Transform background;      // the bar's backing sprite, hidden alongside fill
    public bool hideWhenIdle = true;  // hide the whole bar while not charging

    [Tooltip("Seconds a press must be held before the bar appears, so a quick tap/click " +
             "never flashes it.")]
    public float showDelay = 0.1f;

    float baseScaleY;
    float baseScaleZ;
    float lastProgress = -1f;
    bool lastCharging;

    void Awake()
    {
        if (fill != null)
        {
            baseScaleY = fill.localScale.y;
            baseScaleZ = fill.localScale.z;
        }
        if (toolUser == null) toolUser = GetComponentInParent<ToolUser>();
    }

    void LateUpdate()
    {
        if (toolUser == null || fill == null) return;

        bool show = toolUser.IsCharging && toolUser.ChargeElapsed >= showDelay;
        float n = toolUser.ChargeProgress;
        if (Mathf.Approximately(n, lastProgress) && show == lastCharging) return;
        lastProgress = n;
        lastCharging = show;

        fill.localScale = new Vector3(n, baseScaleY, baseScaleZ);

        if (hideWhenIdle)
        {
            fill.gameObject.SetActive(show);
            if (background != null) background.gameObject.SetActive(show);
        }
    }
}

using UnityEngine;
using TMPro;

public enum PopupStyle { Enemy, Player, Environment, Experience }

// A single floating number (or short text, e.g. "+2" XP). Scrolls upward and fades out over `lifetime`, then
// returns itself to the pool. Authored on a world-space TMP prefab.
[RequireComponent(typeof(TMP_Text))]
public class DamagePopup : MonoBehaviour
{
    public float lifetime = 0.4f;       // seconds until it returns to the pool
    public float riseSpeed = 1.2f;      // world units climbed per second
    public AnimationCurve alpha = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public Color environmentDmgColor = new Color(1f, 0.5952402f, 0f);   // damage dealt to an enemy
    public Color enemyDmgColor = new Color(1f, 0.5952402f, 0f);   // damage dealt to an enemy
    public Color playerDmgColor = new Color(0.9f, 0.1f, 0.1f);    // damage dealt to the player
    public Color enemyHealColor = new Color(0.2f, 0.9f, 0.3f);    // healing on an enemy
    public Color playerHealColor = new Color(0.2f, 0.9f, 0.3f);   // healing on the player
    public Color experienceColor = new Color(0.66f, 0.42f, 0.95f); // XP gained (purple)
    [Tooltip("Size of Experience popups relative to damage numbers (XP text is the small one).")]
    public float experienceScale = 0.7f;
    [Tooltip("Size of critical-hit numbers relative to normal damage numbers.")]
    public float critScale = 1.4f;

    TMP_Text label;
    CombatText owner;
    float elapsed;
    Color baseColor;
    Vector3 baseScale;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        baseColor = label.color;
        baseScale = transform.localScale;
    }

    // Configure and start the animation. `home` is the pool to return to. A crit
    // shows bigger with a "!".
    public void Play(float amount, PopupStyle style, bool heal, CombatText home, bool crit = false) =>
        Play(Mathf.RoundToInt(amount) + (crit ? "!" : ""), style, heal, home, crit);

    public void Play(string text, PopupStyle style, bool heal, CombatText home, bool crit = false)
    {
        owner = home;
        elapsed = 0f;
        label.text = text;
        baseColor = heal
            ? (style == PopupStyle.Player ? playerHealColor : enemyHealColor)
            : style switch
            {
                PopupStyle.Player => playerDmgColor,
                PopupStyle.Environment => environmentDmgColor,
                PopupStyle.Experience => experienceColor,
                _ => enemyDmgColor,
            };
        // pooled: every Play resets the scale, so a recycled XP popup doesn't stay small
        transform.localScale = style == PopupStyle.Experience ? baseScale * experienceScale
                             : crit ? baseScale * critScale
                             : baseScale;
        SetAlpha(1f);
    }

    void LateUpdate()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            owner.Release(this);
            return;
        }

        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);
        SetAlpha(alpha.Evaluate(elapsed / lifetime));
    }

    void SetAlpha(float a)
    {
        label.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }
}

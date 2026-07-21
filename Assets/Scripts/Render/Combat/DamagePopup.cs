using UnityEngine;
using TMPro;

public enum PopupStyle { Enemy, Player, Environment }

// A single floating number. Scrolls upward and fades out over `lifetime`, then
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

    TMP_Text label;
    CombatText owner;
    float elapsed;
    Color baseColor;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        baseColor = label.color;
    }

    // Configure and start the animation. `home` is the pool to return to.
    public void Play(float amount, PopupStyle style, bool heal, CombatText home)
    {
        owner = home;
        elapsed = 0f;
        label.text = Mathf.RoundToInt(amount).ToString();
        baseColor = heal
            ? (style == PopupStyle.Player ? playerHealColor : enemyHealColor)
            : style switch
            {
                PopupStyle.Player => playerDmgColor,
                PopupStyle.Environment => environmentDmgColor,
                _ => enemyDmgColor,
            };
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

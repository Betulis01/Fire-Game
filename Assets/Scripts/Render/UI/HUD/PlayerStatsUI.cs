using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-left player stats: XP bar with the level beside it, health bar with
// "current/max", and the warmth bar under it. Polled each frame -- PlayerBuffs
// changes max health without an event -- and text is only rebuilt on change.
public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] HudRoot root;
    [SerializeField] Image xpFill;          // Filled, horizontal
    [SerializeField] TMP_Text levelText;
    [SerializeField] Image healthFill;      // Filled, horizontal
    [SerializeField] TMP_Text healthText;
    [SerializeField] Image warmthFill;      // Filled, horizontal

    int shownLevel = -1;
    int shownHealth = -1;
    int shownMax = -1;

    void LateUpdate()
    {
        if (root == null) return;

        PlayerExperience xp = root.Experience;
        if (xp != null)
        {
            xpFill.fillAmount = xp.Normalized;
            if (xp.Level != shownLevel)
            {
                shownLevel = xp.Level;
                levelText.text = shownLevel.ToString();
            }
        }

        Health health = root.Health;
        if (health != null)
        {
            healthFill.fillAmount = health.Normalized;
            int cur = Mathf.CeilToInt(health.current);
            int max = Mathf.CeilToInt(health.maxHealth);
            if (cur != shownHealth || max != shownMax)
            {
                shownHealth = cur;
                shownMax = max;
                healthText.text = cur + "/" + max;
            }
        }

        if (root.Temperature != null && warmthFill != null)
            warmthFill.fillAmount = root.Temperature.Normalized;
    }
}

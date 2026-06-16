using UnityEngine;
using TMPro;

// A single floating number. Scrolls upward and fades out over `lifetime`, then
// returns itself to the pool. Authored on a world-space TMP prefab.
[RequireComponent(typeof(TMP_Text))]
public class DamagePopup : MonoBehaviour
{
    public float lifetime = 0.7f;       // seconds until it returns to the pool
    public float riseSpeed = 1.2f;      // world units climbed per second
    public AnimationCurve alpha = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

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
    public void Play(float amount, CombatText home)
    {
        owner = home;
        elapsed = 0f;
        label.text = Mathf.RoundToInt(amount).ToString();
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

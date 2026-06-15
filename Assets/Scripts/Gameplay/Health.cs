using UnityEngine;

[RequireComponent(typeof(BodyTemperature))]
public class Health : MonoBehaviour
{
    public float current = 100f, max = 100f;
    public float comfortTemperature = 0f;
    public float coldRate = 0.2f;    // health/sec per degree below comfort
    public float warmRate = 0.2f;    // health/sec per degree above comfort

    BodyTemperature body;

    public float Normalized => current / max;

    void Awake()
    {
        body = GetComponent<BodyTemperature>();
    }

    void Update()
    {
        ApplyTemperature();
    }

    void ApplyTemperature()
    {
        // strongest warmth reaching us: 0 at a zone's edge, growing toward its center, 0 outside.
        float warmth = body.Warmth;

        if (warmth > 0f)
        {
            // inside a heat zone: recover, scaling with how close we are to the center.
            // at the very edge warmth -> 0, so this is break-even (no recovery, no loss).
            current += warmth * warmRate * Time.deltaTime;
        }
        else
        {
            // out in the cold: lose health based on how far ambient sits below comfort.
            float deficit = comfortTemperature - Environment.Instance.AmbientTemperature;
            current -= Mathf.Max(0f, deficit) * coldRate * Time.deltaTime;
        }

        current = Mathf.Clamp(current, 0f, max);

        if (current <= 0f) Die();
    }

    public void Die()
    {
        UnityEngine.Debug.Log("You froze.");
        GameStateManager.Instance.SetState(GameState.GameOver);
    } 
}
using UnityEngine;

// The player's Health. Being a Health subclass means a Hurtbox/Hitbox can damage
// the player exactly like any other entity; on top of that, this drains/recovers
// health from body temperature and ends the game on death.
[RequireComponent(typeof(BodyTemperature))]
public class PlayerHealth : Health
{
    public float comfortTemperature = 5f;
    public float coldRate = 0.2f;    // health/sec per degree below comfort
    public float warmRate = 0.2f;    // health/sec per degree above comfort

    BodyTemperature body;

    protected override void Awake()
    {
        base.Awake();   // current = maxHealth
        body = GetComponent<BodyTemperature>();
        Died += Die;
    }

    void Update()
    {
        ApplyTemperature();
    }

    void ApplyTemperature()
    {
        if (DebugInvincibility.Enabled) return;   // debug: player can't lose (or gain) health

        // strongest temp reaching us: 0 at a zone's edge, growing toward its center, 0 outside.
        float temp = body.Temp;

        if (temp > 0f)
        {
            // inside a heat zone: recover, scaling with how close we are to the center.
            // at the very edge temp -> 0, so this is break-even (no recovery, no loss).
            Heal(temp * warmRate * Time.deltaTime);
        }
        else
        {
            // out in the cold: lose health based on how far bodytemp sits below comfort.
            float deficit = comfortTemperature - temp;
            TakeDamage(Mathf.Max(0f, deficit) * coldRate * Time.deltaTime);
        }
    }

    void Die()
    {
        UnityEngine.Debug.Log("You froze.");
        GameStateManager.Instance.GameOver();
    }
}

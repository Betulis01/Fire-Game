using UnityEngine;

// The player's Health. Being a Health subclass means a Hurtbox/Hitbox can damage
// the player exactly like any other entity. Drains health while freezing (warmth == 0)
// and ends the game on death.
[RequireComponent(typeof(PlayerTemperature))]
public class PlayerHealth : Health
{
    PlayerTemperature body;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<PlayerTemperature>();
        body.ColdOverflow += OnColdOverflow;
        Died += Die;
    }

    void OnColdOverflow(float amount)
    {
        if (DebugInvincibility.Enabled) return;
        TakeDamage(amount, DamageType.Environment);
    }

    void Die()
    {
        UnityEngine.Debug.Log("You froze.");
        GameStateManager.Instance.GameOver();
    }
}

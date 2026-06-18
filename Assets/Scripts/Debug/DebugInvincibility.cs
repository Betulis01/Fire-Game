using UnityEngine;

// Debug helper: press X to toggle invincibility. While enabled, the player
// cannot lose health from any source (cold/heat in PlayerHealth, or combat
// damage routed through a Health component on the player).
public class DebugInvincibility : MonoBehaviour
{
    public static bool Enabled { get; private set; }

    void Update()
    {
        if (UserInput.Instance.ToggleInvincibility)
        {
            Enabled = !Enabled;
            UnityEngine.Debug.Log($"[Debug] Invincibility {(Enabled ? "ON" : "OFF")}");
        }
    }
}

using UnityEngine;

// Debug helper: press Z to freeze the day/night cycle. While enabled, DayNightCycle
// stops advancing the clock (time of day, phases, night) so you can hold a moment of
// day. Mirrors DebugInvincibility.
public class DebugDayNightFreeze : MonoBehaviour
{
    public static bool Enabled { get; private set; }

    void Update()
    {
        if (UserInput.Instance.ToggleDayNight)
        {
            Enabled = !Enabled;
            UnityEngine.Debug.Log($"[Debug] Day/Night cycle {(Enabled ? "FROZEN" : "RUNNING")}");
        }
    }
}

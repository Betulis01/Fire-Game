using System.Text;
using UnityEngine;

// Throwaway on-screen readout for tuning heat/temperature. Self-contained:
// reads everything through existing public APIs, so deleting this one file
// removes the feature entirely. Drop it on any GameObject (e.g. the player).
public class DebugReadout : MonoBehaviour
{
    PlayerTemperature body;
    bool show = false;

    void Awake()
    {
        body = FindFirstObjectByType<PlayerTemperature>();
    }

    void Update()
    {
        if (UserInput.Instance.ToggleReadout) show = !show;
    }

    void OnGUI()
    {
        if (!show) return;

        StringBuilder sb = new StringBuilder();

        if (Environment.Instance != null)
        {
            sb.Append($"Ambient: {Environment.Instance.AmbientTemperature:0.0}");
            if (DayNightCycle.Instance != null)
            {
                float hour = DayNightCycle.Instance.Hour;
                int h = (int)hour;
                int m = (int)((hour - h) * 60f);
                sb.Append($"   Time: {h:00}:{m:00} ({DayNightCycle.Instance.CurrentPhase})");
            }
            sb.AppendLine();
        }

        if (body != null)
            sb.AppendLine($"Body temp: {body.Temp:0.0}   Warmth: {body.Warmth:0.0}");

        Vector3 playerPos = body != null ? body.transform.position : transform.position;

        sb.AppendLine($"Heat sources ({HeatSource.All.Count}):");
        foreach (HeatSource s in HeatSource.All)
        {
            Fuel fuel = s.GetComponent<Fuel>();
            float fuelPct = fuel != null ? fuel.fuelLevel * 100f : 0f;
            float burnRate = fuel != null ? fuel.CurrentBurnRate : 0f;
            sb.AppendLine(
                $"  {s.name}: fuel {fuelPct:0}%  burnRate {burnRate:0.0}  range {s.EffectiveRange:0.0}  " +
                $"warmth@player {s.WarmthAt(playerPos):0.0}");
        }

        GUI.skin.box.alignment = TextAnchor.UpperLeft;
        GUILayout.BeginArea(new Rect(10, 10, 460, 400));
        GUILayout.Box(sb.ToString());
        GUILayout.EndArea();
    }
}

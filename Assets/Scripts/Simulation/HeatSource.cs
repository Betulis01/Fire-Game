using System.Collections.Generic;
using UnityEngine;

public class HeatSource : MonoBehaviour
{
    // every live heat source, so systems (e.g. Health) can read warmth from
    // sources that appear at runtime, like a crafted torch.
    public static readonly List<HeatSource> All = new();

    Fuel fuel;
    public float maxWarmth = 40f;     // warmth delivered at point-blank, full fuel
    public float range = 4f;          // how far the heat reaches at full fuel
    public float minRange = 0.5f;       // radius floor at near-empty fuel, so the zone shrinks but never snaps to 0

    // current reach given how much fuel is left (minRange when empty, range when full).

    // exposed so visuals/other systems can read the live radius.
    public float EffectiveRange => Mathf.Lerp(minRange, range, fuel.fuelLevel);

    void Awake()
    {
        fuel = GetComponent<Fuel>();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    // how much warmth this source gives to a point in the world
    public float WarmthAt(Vector3 position)
    {
        if (fuel == null) return 0f;

        float fuelLevel = fuel.fuelLevel;
        if (fuelLevel <= 0f) return 0f;

        // both reach and strength grow with fuel, but the radius bottoms out at
        // minRange instead of collapsing to 0 (that collapse was the old "slap")
        float effectiveRange = EffectiveRange;

        float dist = Vector2.Distance(transform.position, position);
        if (dist >= effectiveRange) return 0f;

        // closer = warmer: 1 at center, 0 at the edge
        float falloff = 1f - (dist / effectiveRange);
        return maxWarmth * fuelLevel * falloff;
    }








    // Scene-view visualization of the heat zone.
    void OnDrawGizmos()
    {
        if (Application.isPlaying && fuel != null)
        {
            // live radius based on current fuel
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f); // orange
            DrawCircle(transform.position, EffectiveRange);
        }
        else
        {
            // edit mode: show the bounds the zone moves between
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f); // max reach
            DrawCircle(transform.position, range);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f); // min reach
            DrawCircle(transform.position, minRange);
        }
    }
    static void DrawCircle(Vector3 center, float radius, int segments = 48)
    {
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
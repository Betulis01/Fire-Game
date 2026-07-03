using System;
using UnityEngine;

// Tracks the player's warmth as a persistent resource (0..maxWarmth).
// Warmth rises near heat sources, drains in the cold.
// Exposes Normalized (0..1) for bar rendering and IsFreezing for health drain.
public class PlayerTemperature : MonoBehaviour
{
    public float maxWarmth = 100f;
    public float comfortTemperature = 5f;  // felt temp above this recovers warmth; below drains it
    public float warmRate = 0.2f;          // warmth/sec per degree above comfort
    public float coldRate = 0.2f;          // warmth/sec per degree below comfort

    public float currentWarmth { get; private set; }

    public float Normalized => currentWarmth / maxWarmth;

    // fired when cold damage overflows past zero warmth; argument is the overflow amount
    public event System.Action<float> ColdOverflow;

    // strongest warmth reaching us right now (0 outside any heat zone)
    public float Warmth
    {
        get
        {
            float w = 0f;
            foreach (HeatSource s in HeatSource.All)
                w = Mathf.Max(w, s.WarmthAt(transform.position));
            return w;
        }
    }

    // world ambient temperature lifted by nearby heat sources
    public float Temp => Environment.Instance.AmbientTemperature + Warmth;

    void Awake()
    {
        currentWarmth = maxWarmth;
    }

    void Update()
    {
        float temp = Temp;
        float dt = Time.deltaTime;

        if (temp > comfortTemperature)
        {
            currentWarmth = Mathf.Min(maxWarmth, currentWarmth + (temp - comfortTemperature) * warmRate * dt);
        }
        else
        {
            float drain = (comfortTemperature - temp) * coldRate * dt;
            float newWarmth = currentWarmth - drain;
            if (newWarmth < 0f)
            {
                ColdOverflow?.Invoke(-newWarmth);  // overflow past zero damages health
                newWarmth = 0f;
            }
            currentWarmth = newWarmth;
        }
    }
}

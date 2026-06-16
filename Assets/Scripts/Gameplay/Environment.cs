using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class Environment : MonoBehaviour
{
    public static Environment Instance;

    // Per-phase values, parallel to DayNightCycle's phase schedule (dawn, morning,
    // noon, afternoon, dusk, night). The *times* of these phases live in
    // DayNightCycle; here we only set what the world feels/looks like at each.
    [Header("Ambient temperature (°C) per phase")]
    public float dawnTemp = -15f;
    public float morningTemp = -10f;
    public float noonTemp = -2f;
    public float afternoonTemp = -5f;
    public float duskTemp = -10f;
    public float nightTemp = -30f;

    // dawn and night both 0 so the night -> dawn segment (wrapping midnight) is a
    // flat zero: fully dark from nightHour through dawnHour. Sunrise then ramps up
    // over dawn -> morning.
    [Header("Global light intensity per phase")]
    public float dawnLight = 0f;
    public float morningLight = 0.05f;
    public float noonLight = 0.75f;
    public float afternoonLight = 0.1f;
    public float duskLight = 0.01f;
    public float nightLight = 0f;

    [Header("Used when there's no DayNightCycle in the scene yet")]
    public float fallbackAmbient = -20f;
    public float fallbackIntensity = 0.02f;

    Light2D light2D;

    // reusable buffers (order matches DayNightCycle's phase schedule) — no per-frame allocs
    readonly float[] temps = new float[6];
    readonly float[] lights = new float[6];

    void Awake()
    {
        Instance = this;
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        if (light2D != null)
            light2D.intensity = DayNightCycle.Instance != null
                ? DayNightCycle.Instance.SampleByPhase(Lights())
                : fallbackIntensity;
    }

    // a property, not a raw field — the clock drives it through the phase values,
    // and everything that reads ambient (BodyTemperature, DebugReadout, ...) is
    // unaffected.
    public float AmbientTemperature =>
        DayNightCycle.Instance != null
            ? DayNightCycle.Instance.SampleByPhase(Temps())
            : fallbackAmbient;

    float[] Temps()
    {
        temps[0] = dawnTemp; temps[1] = morningTemp; temps[2] = noonTemp;
        temps[3] = afternoonTemp; temps[4] = duskTemp; temps[5] = nightTemp;
        return temps;
    }

    float[] Lights()
    {
        lights[0] = dawnLight; lights[1] = morningLight; lights[2] = noonLight;
        lights[3] = afternoonLight; lights[4] = duskLight; lights[5] = nightLight;
        return lights;
    }
}

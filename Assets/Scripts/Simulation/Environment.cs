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

    // dawn and night match, so the night -> dawn segment (wrapping midnight) is a flat,
    // faint moonlight from nightHour through dawnHour -- just enough for silhouettes;
    // fire is still how you see. Sunrise ramps up over dawn -> morning, sunset fades
    // over afternoon -> dusk.
    [Header("Global light intensity per phase")]
    public float dawnLight = 0.04f;
    public float morningLight = 0.3f;
    public float noonLight = 0.75f;
    public float afternoonLight = 0.35f;
    public float duskLight = 0.1f;
    public float nightLight = 0.04f;

    // the light's tint, blended the same way: warm sunrise (dawn -> morning) and
    // sunset (afternoon -> dusk), neutral noon, cold blue moonlight at night. Dusk ->
    // night passes through purple.
    [Header("Global light colour per phase")]
    public Color dawnColor = new(1f, 0.58f, 0.52f);
    public Color morningColor = new(1f, 0.87f, 0.68f);
    public Color noonColor = new(1f, 1f, 0.97f);
    public Color afternoonColor = new(1f, 0.76f, 0.48f);
    public Color duskColor = new(0.95f, 0.42f, 0.3f);
    public Color nightColor = new(0.38f, 0.5f, 0.9f);

    // How each phase hands over to the next, for the light's colour and intensity
    // (see DayNightCycle.SampleByPhase): x = time through the segment, y = 0 (this
    // phase) .. 1 (the next). The default ease-in-out holds each phase near its hour
    // and turns mid-segment; night -> dawn holds the moonlight until late, so the
    // sunrise colour doesn't bleed into the small hours.
    [Header("Global light transition curves")]
    public AnimationCurve dawnToMorning = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve morningToNoon = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve noonToAfternoon = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve afternoonToDusk = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve duskToNight = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve nightToDawn = HoldThenTurn(0.75f);

    [Header("Used when there's no DayNightCycle in the scene yet")]
    public float fallbackAmbient = -20f;
    public float fallbackIntensity = 0.02f;
    public Color fallbackColor = Color.white;

    Light2D light2D;

    // reusable buffers (order matches DayNightCycle's phase schedule) — no per-frame allocs
    readonly float[] temps = new float[6];
    readonly float[] lights = new float[6];
    readonly Color[] colors = new Color[6];
    readonly AnimationCurve[] curves = new AnimationCurve[6];

    void Awake()
    {
        Instance = this;
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        if (light2D == null) return;
        DayNightCycle clock = DayNightCycle.Instance;
        light2D.intensity = clock != null ? clock.SampleByPhase(Lights(), Curves()) : fallbackIntensity;
        light2D.color = clock != null ? clock.SampleByPhase(Colors(), curves) : fallbackColor;
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

    Color[] Colors()
    {
        colors[0] = dawnColor; colors[1] = morningColor; colors[2] = noonColor;
        colors[3] = afternoonColor; colors[4] = duskColor; colors[5] = nightColor;
        return colors;
    }

    AnimationCurve[] Curves()
    {
        curves[0] = dawnToMorning; curves[1] = morningToNoon; curves[2] = noonToAfternoon;
        curves[3] = afternoonToDusk; curves[4] = duskToNight; curves[5] = nightToDawn;
        return curves;
    }

    // flat at 0 until `start`, then an S-turn up to 1 by the segment's end
    static AnimationCurve HoldThenTurn(float start) =>
        new(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(start, 0f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));
}

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FireLight : MonoBehaviour
{
    Light2D light2D;
    Fuel fuel;

    public float maxLightIntensity = 1f;   // brightness at full fuel
    public float maxOuterRadius = 5f;       // outer radius at full fuel
    public float maxInnerRadius = 1f;       // inner radius at full fuel
    public float flickerAmount = 0.09f;
    public float flickerSpeed = 8f;

    [Header("Boost on fuel added")]
    public float boostDuration = 1f;
    public float boostIntensity = 0.8f;     // extra intensity at the peak
    public float boostRange = 2f;           // extra outer radius at the peak
    public AnimationCurve boostCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Scale flicker")]
    public Transform scaleTarget;            // null -> the Light2D's transform
    [Range(0.1f, 5f)]
    public float baseScale = 1f;             // resting (uniform) size the flicker rides around
    public float scaleFlickerAmount = 0.05f; // how much the shared noise flickers the scale
    public float scaleBoost = 0.2f;          // extra scale at the fuel-add boost peak

    float boostTimer;

    void Awake()
    {
        light2D = GetComponentInChildren<Light2D>();
        fuel = GetComponent<Fuel>();

        // the scale flicker rides on the light's own transform by default, so it
        // stays where the light is placed
        if (scaleTarget == null && light2D != null)
            scaleTarget = light2D.transform;
    }

    void OnEnable()  { if (fuel != null) fuel.FuelAdded += OnFuelAdded; }
    void OnDisable() { if (fuel != null) fuel.FuelAdded -= OnFuelAdded; }

    void OnFuelAdded(float amount) => boostTimer = boostDuration;

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float flicker = 1f + (noise - 0.5f) * 2f * flickerAmount;

        // decaying boost, 0..1 shaped by the curve over its lifetime
        float boost = 0f;
        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(boostTimer / boostDuration);
            boost = boostCurve.Evaluate(t);
        }

        light2D.intensity = fuel.fuelLevel * maxLightIntensity * flicker
                          + boost * boostIntensity;
        light2D.pointLightOuterRadius = fuel.fuelLevel * maxOuterRadius * flicker
                          + boost * boostRange;
        light2D.pointLightInnerRadius = fuel.fuelLevel * maxInnerRadius * flicker;

        // scale flickers off the same noise as the intensity (no breathing wave),
        // with the same fuel-added pop layered on top
        if (scaleTarget != null)
        {
            float scaleFlicker = 1f + (noise - 0.5f) * 2f * scaleFlickerAmount * fuel.fuelLevel;
            scaleTarget.localScale = Vector3.one * (baseScale * (fuel.fuelLevel * scaleFlicker + boost * scaleBoost));
        }
    }
}

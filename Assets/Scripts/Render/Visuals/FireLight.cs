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

    float boostTimer;

    void Awake()
    {
        light2D = GetComponentInChildren<Light2D>();
        fuel = GetComponent<Fuel>();
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
    }
}

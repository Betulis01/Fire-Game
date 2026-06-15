using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Fuel : MonoBehaviour
{
    Light2D light2D;

    public float fuel = 100f;          // start with a tank, not empty
    public float maxFuel = 100f;       // the full amount, for scaling the light
    public float drainRate = 1f;       // fuel lost per second (slow for the fire)
    public float fuelLevel => fuel / maxFuel; // 0..1

    public event Action<float> FuelAdded;   // raised when fuel is added; passes the amount


    void Start()
    {
        light2D = GetComponent<Light2D>();
    }

    void Update()
    {
        // drain over time, frame-rate independent
        fuel -= drainRate * Time.deltaTime;
        fuel = Mathf.Max(fuel, 0f);   // never go below zero

        if (fuel <= 0f)
        {
            // fire is dead — for now just log it; we'll handle game-over later
            Debug.Log("The fire has gone out.");
        }
    }

    public void Add(float amount)
    {
        fuel = Mathf.Min(fuel + amount, maxFuel);   // clamp so it can't exceed max
        FuelAdded?.Invoke(amount);
    }
}
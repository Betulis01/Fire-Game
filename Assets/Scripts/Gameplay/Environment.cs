using UnityEngine;

public class Environment : MonoBehaviour
{
    public static Environment Instance;

    public float ambientTemperature = -20f;

    void Awake()
    {
        Instance = this;
    }

    // a property, not the raw field — so day/night can replace this later
    // without anything that reads it having to change
    public float AmbientTemperature => ambientTemperature;
}
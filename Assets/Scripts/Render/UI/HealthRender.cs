using UnityEngine;
using TMPro;

public class HealthRender : MonoBehaviour
{
    public PlayerHealth health;    // the player's health component
    public TMP_Text label;         // the UI text to write into

    void Update()
    {
        if (health == null || label == null) return;
        label.text = "Health: " + Mathf.CeilToInt(health.current);
    }
}
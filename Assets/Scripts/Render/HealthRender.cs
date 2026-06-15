using UnityEngine;
using TMPro;

public class HealthRender : MonoBehaviour
{
    public Health health;          // the player's Health component
    public TMP_Text label;         // the UI text to write into

    void Update()
    {
        if (health == null || label == null) return;
        label.text = "Health: " + Mathf.CeilToInt(health.current);
    }
}
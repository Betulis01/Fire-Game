using UnityEngine;

// Discrete-direction tie-break shared by systems that pick a state/pose by cardinal
// direction (as opposed to SwordSwingEffectOrienter's continuous rotation, which
// doesn't need this). Horizontal wins ties so a mostly sideways diagonal reads as
// east/west.
public static class CardinalDir
{
    public static (string dir, bool flip) Resolve(Vector2 v) =>
        Mathf.Abs(v.x) >= Mathf.Abs(v.y) ? ("e", v.x < 0f) : (v.y >= 0f ? "n" : "s", false);
}

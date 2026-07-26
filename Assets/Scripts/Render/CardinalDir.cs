using UnityEngine;

// Discrete-direction bucket shared by systems that pick a state/pose by isometric
// facing (as opposed to SwordSwingEffectOrienter's continuous rotation, which
// doesn't need this). Only two clips are ever drawn, NE and SE; the other two
// directions are each a horizontal mirror of one of them. North/south picks the
// clip (ties at due east/west go to "se"); east/west picks whether it's mirrored
// (ties at due north/south stay unmirrored).
public static class CardinalDir
{
    public static (string dir, bool flip) Resolve(Vector2 v) =>
        (v.y > 0f ? "ne" : "se", v.x < 0f);
}

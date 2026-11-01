using UnityEngine;

// Where sounds are heard from: the 2D attenuation and pan every World voice gets.
// The camera is orthographic and sits 10 units back, so Unity's 3D distance model
// would hear everything from about the same distance and ignore the zoom. Instead
// every AudioSource is 2D and this computes its gain and pan from the camera's view:
//   radius = max(minRadius, radiusPerHalfWidth x half the visible width) x rangeScale
//   gain   = falloff(distance from the view's centre / radius)
//   pan    = (x offset / half the visible width) x panWidth, clamped
// so zooming out hears more of the world, something just off-screen is faint but
// there, and the pan only suggests left or right. Refreshed once a frame by
// AudioSystem. With no camera, everything plays at full gain, centred.
public static class AudioSpace
{
    static Camera cam;

    public static Vector2 Centre { get; private set; }
    public static float HalfWidth { get; private set; }   // 0 = no camera

    public static void Refresh()
    {
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (cam == null) { HalfWidth = 0f; return; }
        Centre = cam.transform.position;
        HalfWidth = cam.orthographic ? cam.orthographicSize * cam.aspect : 10f;
    }

    public static float Radius(AudioConfig config, float rangeScale) =>
        Mathf.Max(config.minRadius, config.radiusPerHalfWidth * HalfWidth) * rangeScale;

    // Distance from the view's centre in hearing radii (0 = centre, 1 = the edge of hearing).
    public static float RadiiAway(AudioConfig config, Vector2 point, float rangeScale) =>
        HalfWidth <= 0f ? 0f : Vector2.Distance(point, Centre) / Radius(config, rangeScale);

    public static float Gain(AudioConfig config, float radiiAway) =>
        radiiAway >= 1f ? 0f : Mathf.Clamp01(config.falloff.Evaluate(radiiAway));

    public static float Pan(AudioConfig config, Vector2 point) =>
        HalfWidth <= 0f ? 0f : Mathf.Clamp((point.x - Centre.x) / HalfWidth, -1f, 1f) * config.panWidth;
}

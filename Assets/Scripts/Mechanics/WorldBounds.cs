using UnityEngine;

// Single source of truth for the playable rectangle. Movement scripts clamp
// positions to it directly; CameraFollow clamps its view so the edge of the
// map is never shown past min/max.
public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    public Vector2 min = new Vector2(-10f, -10f);
    public Vector2 max = new Vector2(10f, 10f);

    void Awake() => Instance = this;

    public Vector2 ClampPoint(Vector2 point) =>
        new Vector2(Mathf.Clamp(point.x, min.x, max.x), Mathf.Clamp(point.y, min.y, max.y));

    // Same as ClampPoint, but insets the rectangle by halfExtents (a sprite's half
    // width/height) so the sprite itself stays inside min/max instead of just its
    // center, i.e. the entity can't visually poke past the edge.
    public Vector2 ClampPoint(Vector2 point, Vector2 halfExtents)
    {
        float minX = min.x + halfExtents.x, maxX = max.x - halfExtents.x;
        float minY = min.y + halfExtents.y, maxY = max.y - halfExtents.y;

        float x = minX <= maxX ? Mathf.Clamp(point.x, minX, maxX) : (min.x + max.x) * 0.5f;
        float y = minY <= maxY ? Mathf.Clamp(point.y, minY, maxY) : (min.y + max.y) * 0.5f;
        return new Vector2(x, y);
    }

    // Clamps a camera's center so its orthographic view rectangle stays inside
    // min/max. If the bounds are narrower than the viewport on an axis, centers
    // on that axis instead of clamping (avoids an impossible/jittery clamp).
    public Vector2 ClampCamera(Vector2 center, Camera cam)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = min.x + halfWidth, maxX = max.x - halfWidth;
        float minY = min.y + halfHeight, maxY = max.y - halfHeight;

        float x = minX <= maxX ? Mathf.Clamp(center.x, minX, maxX) : (min.x + max.x) * 0.5f;
        float y = minY <= maxY ? Mathf.Clamp(center.y, minY, maxY) : (min.y + max.y) * 0.5f;
        return new Vector2(x, y);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        Gizmos.DrawWireCube(center, size);
    }
}

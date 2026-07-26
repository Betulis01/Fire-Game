using UnityEngine;
using UnityEngine.Serialization;

// Single source of truth for the playable area, defined in the ground Grid's cell
// space (a rectangle there) rather than world space, where the isometric Grid
// renders that same rectangle as a diamond. Movement scripts clamp positions to it
// directly; CameraFollow clamps its view so the edge of the map is never shown
// past bounds.
public class WorldBounds : MonoBehaviour
{
    public static WorldBounds Instance { get; private set; }

    [FormerlySerializedAs("min")]
    public Vector2 minCell = new Vector2(-13f, -13f);
    [FormerlySerializedAs("max")]
    public Vector2 maxCell = new Vector2(15f, 12f);

    [Tooltip("Must match the ground Grid's cell size, or the diamond won't line up with the painted tiles.")]
    public Vector2 cellSize = new Vector2(1f, 0.5f);

    void Awake() => Instance = this;

    public Vector2 ClampPoint(Vector2 point) => ClampInCellSpace(point, 0f);

    // Same as ClampPoint, but insets the diamond by halfExtents (a sprite's half
    // width/height) so the sprite itself stays inside bounds instead of just its
    // center, i.e. the entity can't visually poke past the edge.
    public Vector2 ClampPoint(Vector2 point, Vector2 halfExtents) =>
        ClampInCellSpace(point, CellInset(halfExtents));

    // Clamps a camera's center so its orthographic view rectangle stays inside the
    // diamond. Same inset trick as ClampPoint, using the camera's own half-extents.
    public Vector2 ClampCamera(Vector2 center, Camera cam)
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        return ClampInCellSpace(center, CellInset(new Vector2(halfWidth, halfHeight)));
    }

    // Exact cell-space inset that keeps an axis-aligned world-space box of the given
    // half-extents from crossing any of the diamond's 4 edges. Works out to the same
    // value on both cell axes given the isometric basis below.
    float CellInset(Vector2 halfExtents) =>
        halfExtents.x / cellSize.x + halfExtents.y / cellSize.y;

    // Converts to continuous (unrounded) cell space, clamps per-axis same as the old
    // rectangle version, converts back. Unrounded is deliberate: Grid.WorldToCell
    // snaps to the nearest integer cell, which would make a smoothly-moving player
    // or camera stair-step at the boundary instead of sliding along the diamond's edge.
    Vector2 ClampInCellSpace(Vector2 world, float cellInset)
    {
        Vector2 cell = WorldToCell(world);

        float minX = minCell.x + cellInset, maxX = maxCell.x - cellInset;
        float minY = minCell.y + cellInset, maxY = maxCell.y - cellInset;

        float x = minX <= maxX ? Mathf.Clamp(cell.x, minX, maxX) : (minCell.x + maxCell.x) * 0.5f;
        float y = minY <= maxY ? Mathf.Clamp(cell.y, minY, maxY) : (minCell.y + maxCell.y) * 0.5f;

        return CellToWorld(new Vector2(x, y));
    }

    // Same isometric basis as Unity's Grid.CellToWorld/WorldToCell for CellLayout.Isometric.
    Vector2 WorldToCell(Vector2 world) => new Vector2(
        world.x / cellSize.x + world.y / cellSize.y,
        world.y / cellSize.y - world.x / cellSize.x);

    Vector2 CellToWorld(Vector2 cell) => new Vector2(
        (cell.x - cell.y) * cellSize.x * 0.5f,
        (cell.x + cell.y) * cellSize.y * 0.5f);

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector2 a = CellToWorld(new Vector2(minCell.x, minCell.y));
        Vector2 b = CellToWorld(new Vector2(maxCell.x, minCell.y));
        Vector2 c = CellToWorld(new Vector2(maxCell.x, maxCell.y));
        Vector2 d = CellToWorld(new Vector2(minCell.x, maxCell.y));
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}

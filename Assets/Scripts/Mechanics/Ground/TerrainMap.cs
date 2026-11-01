using UnityEngine;
using UnityEngine.Tilemaps;

// Answers "what ground is at this point?" for the scene, down to the pixel. It sits on
// the Ground Detail tilemap (GroundDetailSetup adds it): the base ground is baseGround
// everywhere, and a Ground Detail TerrainTile overrides it wherever its sprite's baked
// mask covers the point. Patches are bigger than a cell and overlap freely, so a query
// looks at every cell within `reach` of the point, brings the point into each tile's
// sprite space (undoing the cell position and the tile's offset), and asks its mask;
// of the covering tiles, the one the renderer draws last -- the one on top -- wins.
// Also holds the table of how each ground type moves an entity (GroundSensor reads it).
[RequireComponent(typeof(Tilemap))]
public class TerrainMap : MonoBehaviour
{
    public static TerrainMap Instance { get; private set; }

    [Tooltip("The ground wherever no Ground Detail patch covers the point (the base tilemap's).")]
    public GroundType baseGround = GroundType.Snow;

    [Tooltip("How each ground type moves whoever stands on it. A type missing here has no effect.")]
    public GroundProps[] props = GroundProps.Defaults();

    [Tooltip("Cells around the point searched for covering patches. A 4x4-cell patch reaches about 2.2 cells " +
             "from its painted cell.")]
    [SerializeField] int reach = 3;

    Tilemap tilemap;
    TilemapRenderer tilemapRenderer;

    void Awake()
    {
        Instance = this;
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public GroundType At(Vector2 world)
    {
        Vector3 local = tilemap.WorldToLocal(world);
        Vector3Int centre = tilemap.LocalToCell(local);
        Vector3 anchor = tilemap.tileAnchor;

        bool found = false;
        Vector3Int top = default;
        GroundType ground = baseGround;
        for (int dy = -reach; dy <= reach; dy++)
        for (int dx = -reach; dx <= reach; dx++)
        {
            var cell = new Vector3Int(centre.x + dx, centre.y + dy, centre.z);
            if (tilemap.GetTile(cell) is not TerrainTile tile) continue;

            Vector3 fromCell = local - tilemap.CellToLocalInterpolated(cell + anchor);
            Vector3 p = tilemap.GetTransformMatrix(cell).inverse.MultiplyPoint3x4(fromCell);
            if (!tile.Covers(p)) continue;

            if (found && !DrawsAfter(cell, top)) continue;
            found = true;
            top = cell;
            ground = tile.ground;
        }
        return ground;
    }

    public GroundProps Props(GroundType type)
    {
        foreach (GroundProps p in props)
            if (p.type == type) return p;
        return GroundProps.Neutral;
    }

    public GroundProps PropsAt(Vector2 world) => Props(At(world));

    public bool LeavesTrail(Vector2 world) => PropsAt(world).leavesTrail;

    // Whether the renderer draws cell a after cell b (so a is on top). The sort order
    // names the corner rendering starts from; the opposite corner draws last.
    bool DrawsAfter(Vector3Int a, Vector3Int b)
    {
        TilemapRenderer.SortOrder order = tilemapRenderer != null ? tilemapRenderer.sortOrder : TilemapRenderer.SortOrder.BottomLeft;
        bool fromBottom = order is TilemapRenderer.SortOrder.BottomLeft or TilemapRenderer.SortOrder.BottomRight;
        bool fromLeft = order is TilemapRenderer.SortOrder.BottomLeft or TilemapRenderer.SortOrder.TopLeft;
        if (a.y != b.y) return fromBottom ? a.y > b.y : a.y < b.y;
        return fromLeft ? a.x > b.x : a.x < b.x;
    }
}

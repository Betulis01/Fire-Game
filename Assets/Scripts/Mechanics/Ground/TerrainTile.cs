using UnityEngine;
using UnityEngine.Tilemaps;

// A Ground Detail tile that knows its ground type and exactly which of its sprite's
// pixels are that ground: a bit mask, baked by GroundDetailSetup from the sprite's
// alpha (eroded past the soft outline), so nothing reads a texture at runtime. The
// mask lives in the sprite's pixel space with the pivot at (0, 0); maskOrigin is its
// bottom-left there. TerrainMap brings a world point into that space.
public class TerrainTile : Tile
{
    public GroundType ground;

    [SerializeField, HideInInspector] Vector2Int maskOrigin;
    [SerializeField, HideInInspector] Vector2Int maskSize;
    [SerializeField, HideInInspector] byte[] maskBits;
    [SerializeField, HideInInspector] float pixelsPerUnit = 32f;

    // Whether the point (sprite space, units from the pivot) is on this tile's ground.
    public bool Covers(Vector2 spriteLocal)
    {
        if (maskBits == null || maskBits.Length == 0) return false;
        int x = Mathf.FloorToInt(spriteLocal.x * pixelsPerUnit) - maskOrigin.x;
        int y = Mathf.FloorToInt(spriteLocal.y * pixelsPerUnit) - maskOrigin.y;
        if (x < 0 || y < 0 || x >= maskSize.x || y >= maskSize.y) return false;
        int i = y * maskSize.x + x;
        return (maskBits[i >> 3] & (1 << (i & 7))) != 0;
    }

#if UNITY_EDITOR
    // GroundDetailSetup's bake: `mask` is row-major from the bottom-left, maskSize.x wide.
    public void SetMask(bool[] mask, Vector2Int origin, Vector2Int size, float ppu)
    {
        maskOrigin = origin;
        maskSize = size;
        pixelsPerUnit = ppu;
        maskBits = new byte[(mask.Length + 7) / 8];
        for (int i = 0; i < mask.Length; i++)
            if (mask[i]) maskBits[i >> 3] |= (byte)(1 << (i & 7));
    }
#endif
}

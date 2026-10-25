using System.Collections.Generic;
using UnityEngine;

// The ground's trail layer: snow trenches are painted into pixel textures once and
// then just drawn, so a trail costs the same to render however long it gets. The
// world is split into chunks of ChunkSize x ChunkSize art pixels (1 texel = 1 art
// pixel = 1/PixelsPerUnit units), created only where something has been stamped;
// each chunk is one SpriteRenderer.
//
// A stamp (SnowStamp: rim, wall and floor bands from the artist's layers) merges
// with the trench already there through a per-pixel rank -- rim 1, wall 2, floor 3:
// a stamp pixel only lands where its rank is at least the stored one. That is
// "every rim, then every wall, then every floor" resolved once at paint time, so a
// row of overlapping stamps reads as one continuous trench with the rim only along
// its outside edge.
//
// Created on first use by a SnowTrail emitter; one per scene.
public class SnowTrailCanvas : MonoBehaviour
{
    public const int PixelsPerUnit = 32;   // the world art's pixel size (player, tiles)
    const int ChunkSize = 128;             // art pixels per chunk side (4x4 units)

    // Above the ground tilemap (Background, order 0); below the player's
    // Indicators-layer sprite and everything y-sorted on Default.
    const string SortingLayerName = "Background";
    const int SortingOrder = 1;

    class Chunk
    {
        public Texture2D texture;
        public Color32[] pixels;
        public byte[] rank;
        public bool dirty;
    }

    static SnowTrailCanvas instance;

    readonly Dictionary<Vector2Int, Chunk> chunks = new();
    readonly List<Chunk> dirtyChunks = new();

    public static SnowTrailCanvas Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("Snow Trail Canvas").AddComponent<SnowTrailCanvas>();
            return instance;
        }
    }

    // Paint one stamp centred (by its pivot) on a world position, snapped to the
    // art pixel grid.
    public void Stamp(Vector2 worldPos, SnowStamp stamp)
    {
        int px = Mathf.RoundToInt(worldPos.x * PixelsPerUnit);
        int py = Mathf.RoundToInt(worldPos.y * PixelsPerUnit);

        foreach (SnowStamp.Band band in stamp.Bands)
        {
            int ox = px + band.offsetX;
            int oy = py + band.offsetY;
            for (int j = 0; j < band.height; j++)
                for (int i = 0; i < band.width; i++)
                {
                    Color32 c = band.pixels[j * band.width + i];
                    if (c.a != 0) Write(ox + i, oy + j, c, band.rank);
                }
        }
    }

    void Write(int x, int y, Color32 color, byte rank)
    {
        var key = new Vector2Int(FloorDiv(x, ChunkSize), FloorDiv(y, ChunkSize));
        if (!chunks.TryGetValue(key, out Chunk chunk)) chunk = CreateChunk(key);

        int index = (y - key.y * ChunkSize) * ChunkSize + (x - key.x * ChunkSize);
        if (rank < chunk.rank[index]) return;

        chunk.rank[index] = rank;
        chunk.pixels[index] = color;
        if (!chunk.dirty)
        {
            chunk.dirty = true;
            dirtyChunks.Add(chunk);
        }
    }

    // Upload what was painted this frame, once per touched chunk.
    void LateUpdate()
    {
        foreach (Chunk chunk in dirtyChunks)
        {
            chunk.texture.SetPixels32(chunk.pixels);
            chunk.texture.Apply(false);
            chunk.dirty = false;
        }
        dirtyChunks.Clear();
    }

    Chunk CreateChunk(Vector2Int key)
    {
        var chunk = new Chunk
        {
            texture = new Texture2D(ChunkSize, ChunkSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Snow Trail {key}",
            },
            pixels = new Color32[ChunkSize * ChunkSize],
            rank = new byte[ChunkSize * ChunkSize],
        };

        var go = new GameObject($"Chunk {key}");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(key.x * ChunkSize, key.y * ChunkSize, 0f) / PixelsPerUnit;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(chunk.texture, new Rect(0, 0, ChunkSize, ChunkSize), Vector2.zero,
                                  PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sr.sortingLayerName = SortingLayerName;
        sr.sortingOrder = SortingOrder;

        chunks[key] = chunk;
        return chunk;
    }

    static int FloorDiv(int a, int b) => a >= 0 ? a / b : (a - b + 1) / b;

    void OnDestroy()
    {
        foreach (Chunk chunk in chunks.Values) Destroy(chunk.texture);
        if (instance == this) instance = null;
    }
}

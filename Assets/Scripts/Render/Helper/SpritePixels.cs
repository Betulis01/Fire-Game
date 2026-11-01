using UnityEngine;

// A sprite's packed pixels (its textureRect, row-major from the bottom-left). Imported
// Aseprite textures aren't CPU-readable, so unless the texture is, the pixels are copied
// through a temporary RenderTexture (exact at 1:1 size). Used by SnowStamp at runtime
// and by GroundDetailSetup's mask bake in the editor.
public static class SpritePixels
{
    public static Color32[] Read(Sprite sprite, out int width, out int height)
    {
        Rect r = sprite.textureRect;
        int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
        int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y);
        Texture2D source = sprite.texture;
        width = w;
        height = h;

        if (source.isReadable)
        {
            Color[] c = source.GetPixels(x, y, w, h);
            var pixels = new Color32[c.Length];
            for (int i = 0; i < c.Length; i++) pixels[i] = c[i];
            return pixels;
        }

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0,
                                                      RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        copy.ReadPixels(new Rect(x, y, w, h), 0, 0);
        copy.Apply(false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        Color32[] read = copy.GetPixels32();
        if (Application.isPlaying) Object.Destroy(copy);
        else Object.DestroyImmediate(copy);
        return read;
    }
}

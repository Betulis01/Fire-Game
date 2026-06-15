using UnityEngine;
using UnityEngine.Rendering;

// Throwaway combat-visualization overlay. Press C to toggle wireframes for:
//   - every Hurtbox collider (green)
//   - every Hitbox's strike circle at its current position (red)
//   - each weapon's attack range, Tool.range (yellow), plus a preview of where a
//     swing would land right now toward the mouse (red)
// Draws in the Game and Scene views via GL (works under URP). Self-contained:
// delete this one file to remove the feature. Note: DebugReadout also uses C, so
// the two toggle together unless you change a toggleKey.
public class CombatDebug : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.C;

    static readonly Color HurtColor = Color.green;
    static readonly Color HitColor = Color.red;
    static readonly Color RangeColor = new Color(1f, 0.85f, 0.2f);

    bool show = true;
    Material lineMat;

    void OnEnable() => RenderPipelineManager.endCameraRendering += OnEndCamera;
    void OnDisable() => RenderPipelineManager.endCameraRendering -= OnEndCamera;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) show = !show;
    }

    void OnEndCamera(ScriptableRenderContext ctx, Camera camera)
    {
        if (!show) return;
        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;

        EnsureMaterial();

        GL.PushMatrix();
        GL.LoadProjectionMatrix(camera.projectionMatrix);
        GL.modelview = camera.worldToCameraMatrix;
        lineMat.SetPass(0);
        GL.Begin(GL.LINES);

        foreach (Hurtbox hb in FindObjectsByType<Hurtbox>(FindObjectsSortMode.None))
            DrawCollider(hb.GetComponent<Collider2D>(), HurtColor);

        // Only weapons currently held by a player (ToolUser) are "able to hit": show
        // their reach + a live strike preview that follows the mouse. Weapons lying
        // in the world have no ToolUser parent and are skipped.
        foreach (Hitbox hit in FindObjectsByType<Hitbox>(FindObjectsSortMode.None))
        {
            ToolUser user = hit.GetComponentInParent<ToolUser>();
            Tool tool = hit.GetComponent<Tool>();
            if (user == null || tool == null) continue;

            Vector2 origin = user.transform.position;                       // same origin the real swing uses
            Vector2 center = origin + AimDir(origin, camera) * tool.range;   // follows the mouse live

            DrawCircle(origin, tool.range, RangeColor);                // reach
            DrawCircle(center, hit.radius, HitColor);                  // where a swing lands now
        }

        GL.End();
        GL.PopMatrix();
    }

    Vector2 AimDir(Vector2 from, Camera camera)
    {
        if (camera == null) return Vector2.right;

        Vector2 mouse = camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 d = mouse - from;
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
    }

    void DrawCollider(Collider2D col, Color color)
    {
        if (col == null) return;

        if (col is CircleCollider2D circle)
        {
            Vector2 c = circle.transform.TransformPoint(circle.offset);
            Vector3 s = col.transform.lossyScale;
            float r = circle.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
            DrawCircle(c, r, color);
        }
        else
        {
            Bounds b = col.bounds;
            DrawRect(b.min, b.max, color);
        }
    }

    void DrawRect(Vector2 min, Vector2 max, Color color)
    {
        GL.Color(color);
        Line(min.x, min.y, max.x, min.y);
        Line(max.x, min.y, max.x, max.y);
        Line(max.x, max.y, min.x, max.y);
        Line(min.x, max.y, min.x, min.y);
    }

    void DrawCircle(Vector2 center, float radius, Color color, int segments = 32)
    {
        GL.Color(color);
        float step = Mathf.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;
            Line(
                center.x + Mathf.Cos(a0) * radius, center.y + Mathf.Sin(a0) * radius,
                center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius);
        }
    }

    void Line(float x0, float y0, float x1, float y1)
    {
        GL.Vertex3(x0, y0, 0f);
        GL.Vertex3(x1, y1, 0f);
    }

    void EnsureMaterial()
    {
        if (lineMat != null) return;

        lineMat = new Material(Shader.Find("Hidden/Internal-Colored"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        lineMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMat.SetInt("_Cull", (int)CullMode.Off);
        lineMat.SetInt("_ZWrite", 0);
        lineMat.SetInt("_ZTest", (int)CompareFunction.Always);
    }

    void OnDestroy()
    {
        if (lineMat != null) DestroyImmediate(lineMat);
    }
}

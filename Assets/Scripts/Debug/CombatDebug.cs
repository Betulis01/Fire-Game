using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Throwaway combat-visualization overlay. Press C to toggle wireframes for:
//   - every Hurtbox collider (green)
//   - every Hitbox's reach (yellow) and current strike circle (red) -- for a
//     player's WeaponUse this follows the mouse live; for an EnemyAttacker it
//     follows its last swing direction and also shows attackRange (orange); a
//     world-lying weapon with no wielder just shows its own hitbox footprint
//   - every other Collider2D in the scene (cyan) -- blockers, interaction
//     zones, deposit zones, world bounds, anything not already drawn above
// Draws in the Game and Scene views via GL (works under URP). Self-contained:
// delete this one file to remove the feature. Note: DebugReadout also uses C, so
// the two toggle together unless you change a toggleKey.
public class CombatDebug : MonoBehaviour
{
    static readonly Color HurtColor = Color.green;
    static readonly Color HitColor = Color.red;
    static readonly Color RangeColor = new Color(1f, 0.85f, 0.2f);
    static readonly Color EnemyRangeColor = new Color(1f, 0.5f, 0.1f);
    static readonly Color ColliderColor = Color.cyan;

    readonly HashSet<Collider2D> drawnColliders = new();

    bool show = false;
    Material lineMat;

    void OnEnable() => RenderPipelineManager.endCameraRendering += OnEndCamera;
    void OnDisable() => RenderPipelineManager.endCameraRendering -= OnEndCamera;

    void Update()
    {
        if (UserInput.Instance.ToggleReadout) show = !show;
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

        drawnColliders.Clear();

        foreach (Hurtbox hb in FindObjectsByType<Hurtbox>(FindObjectsSortMode.None))
        {
            Collider2D col = hb.HurtCollider != null ? hb.HurtCollider : hb.GetComponentInChildren<Collider2D>();
            if (col == null) continue;
            drawnColliders.Add(col);
            DrawCollider(col, HurtColor);
        }

        // A weapon's reach + strike circle. Wielder determines how the aim direction
        // is resolved: a player's WeaponUse follows the mouse live; an EnemyAttacker
        // follows its last swing direction and also shows its attackRange (the
        // distance at which it starts swinging); a weapon lying in the world has no
        // wielder, so just show its own hitbox footprint with no reach ring.
        foreach (Hitbox hit in FindObjectsByType<Hitbox>(FindObjectsSortMode.None))
        {
            Tool tool = hit.GetComponent<Tool>();
            if (tool == null) continue;

            WeaponUse user = hit.GetComponentInParent<WeaponUse>();
            EnemyAttacker enemy = user == null ? hit.GetComponentInParent<EnemyAttacker>() : null;

            Vector2 origin;
            Vector2 aimDir;

            if (user != null)
            {
                origin = user.Origin;                    // same origin the real swing uses
                aimDir = AimDir(origin, camera);          // follows the mouse live
            }
            else if (enemy != null)
            {
                origin = enemy.Origin;
                aimDir = enemy.AimDir;
                DrawCircle(origin, enemy.attackRange, EnemyRangeColor);   // distance it starts swinging at
            }
            else
            {
                DrawCircle(hit.transform.position, hit.radius, HitColor);   // orphaned/world weapon: footprint only
                continue;
            }

            Vector2 center = origin + aimDir * tool.range;
            DrawCircle(origin, tool.range, RangeColor);                // reach
            DrawCircle(center, hit.radius, HitColor);                  // where a swing lands now
        }

        // Every other physics collider in the scene -- blockers, interaction zones,
        // deposit zones, world bounds, anything not already drawn as a Hurtbox above.
        foreach (Collider2D col in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (drawnColliders.Contains(col)) continue;
            DrawCollider(col, ColliderColor);
        }

        GL.End();
        GL.PopMatrix();
    }

    Vector2 AimDir(Vector2 from, Camera camera)
    {
        if (camera == null) return Vector2.right;
        return UserInput.Instance.AimDirection(from, camera);
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Throwaway combat-visualization overlay. Press C to toggle wireframes for:
//   - every Hurtbox collider (green)
//   - every melee strike cone (red) and full reach circle (yellow) -- the
//     player's follows the mouse live; each enemy's aims at its aggro target (or
//     its armed swing's latched aim) and also shows attackRange (orange)
//   - every EnemyBrain's detection radius (cyan) -- otherwise only an
//     Editor-only OnDrawGizmosSelected, invisible here without this
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
    static readonly Color DetectionColor = Color.cyan;

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

        // The player's strikes, following the mouse live: the armed swing's cone, or
        // idle, each melee hand's (TargetPreview highlights from one of them).
        foreach (WeaponUse user in FindObjectsByType<WeaponUse>(FindObjectsSortMode.None))
        {
            if (user.IsSwingArmed)
            {
                if (user.TryGetArmedMeleePreview(out _, out _, out StrikeShape armed, out Vector2 armedDir))
                    DrawStrike(user.Origin, armedDir, armed);
                continue;
            }
            foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
                if (user.TryGetHandMeleePreview(side, out _, out _, out StrikeShape shape, out Vector2 aimDir))
                    DrawStrike(user.Origin, aimDir, shape);
        }

        // Each enemy's strike cone, aimed where its next (or armed) swing goes, plus
        // its attackRange (the distance at which it starts swinging). Its weapon is a
        // prefab reference rather than a scene object, so it's read off the attacker.
        foreach (EnemyAttacker enemy in FindObjectsByType<EnemyAttacker>(FindObjectsSortMode.None))
        {
            if (!enemy.isActiveAndEnabled) continue;   // dead: no more swings
            Vector2 origin = enemy.Origin;
            Vector2 aimDir = enemy.PreviewAimDir;
            if (aimDir.sqrMagnitude < 1e-6f) aimDir = new Vector2(1f, -1f).normalized;   // never aimed yet: default facing
            StrikeShape shape = enemy.Strike;

            DrawCircle(origin, enemy.attackRange, EnemyRangeColor);
            DrawCircle(origin, shape.range, RangeColor);
            DrawCone(origin, aimDir, shape.range, shape.arcAngle, HitColor);
        }

        // Enemy detection radius -- normally only an OnDrawGizmosSelected wire sphere
        // (Editor-only, needs the object selected), so it's otherwise invisible in
        // this GL-based overlay unlike everything else here.
        foreach (EnemyBrain brain in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            DrawCircle(brain.transform.position, brain.detectionRadius, DetectionColor);

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

    void DrawStrike(Vector2 origin, Vector2 aimDir, in StrikeShape shape)
    {
        DrawCircle(origin, shape.range, RangeColor);                       // reach
        DrawCone(origin, aimDir, shape.range, shape.arcAngle, HitColor);   // where a swing lands now
    }

    // A strike cone: its two edges and the rim arc between them (no edges for a full circle).
    void DrawCone(Vector2 apex, Vector2 dir, float radius, float arcAngle, Color color, int segments = 16)
    {
        if (dir.sqrMagnitude < 1e-6f) return;
        GL.Color(color);

        float half = Mathf.Min(arcAngle, 360f) * 0.5f * Mathf.Deg2Rad;
        float aim = Mathf.Atan2(dir.y, dir.x);
        float step = half * 2f / segments;
        Vector2 Rim(float a) => apex + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;

        Vector2 prev = Rim(aim - half);
        if (arcAngle < 360f) Line(apex.x, apex.y, prev.x, prev.y);
        for (int i = 1; i <= segments; i++)
        {
            Vector2 p = Rim(aim - half + step * i);
            Line(prev.x, prev.y, p.x, p.y);
            prev = p;
        }
        if (arcAngle < 360f) Line(prev.x, prev.y, apex.x, apex.y);
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

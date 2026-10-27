using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Marks a light as one that casts SpriteShadows. Lights register themselves while
// enabled, so shadows never scan the scene for lights, and a torch placed at runtime
// casts from the frame it's enabled. Only lights with this component cast -- e.g. the
// campfire's point light, not its ground-pool sprite light at the same spot.
[RequireComponent(typeof(Light2D))]
public class ShadowLight : MonoBehaviour
{
    public static readonly List<ShadowLight> All = new();

    [Tooltip("Reach of a non-point light (a sprite light has no radius). Point lights use their own outer radius.")]
    [SerializeField] float radius = 3f;

    public Light2D Light { get; private set; }

    void Awake() => Light = GetComponent<Light2D>();

    void OnEnable() => All.Add(this);

    void OnDisable()
    {
        int i = All.IndexOf(this);
        if (i < 0) return;
        int last = All.Count - 1;
        All[i] = All[last];
        All.RemoveAt(last);
    }

    // 1 at the light, 0 at (and beyond) its edge. A point light is measured in its own
    // local space, so FireLight's ground squash stretches the reach into the same
    // ellipse as the light pool, and its live, fuel-driven radius counts.
    public float Reach(Vector2 feet)
    {
        if (Light.lightType == Light2D.LightType.Point)
        {
            if (Light.pointLightOuterRadius <= 0f) return 0f;
            Vector2 local = transform.InverseTransformPoint(feet);
            return Mathf.Max(0f, 1f - local.magnitude / Light.pointLightOuterRadius);
        }
        if (radius <= 0f) return 0f;
        return Mathf.Max(0f, 1f - Vector2.Distance(feet, transform.position) / radius);
    }

    // Times Reach, how hard this light lands somewhere: an out campfire (intensity 0)
    // casts nothing.
    public float Intensity => Light.isActiveAndEnabled ? Light.intensity : 0f;
}

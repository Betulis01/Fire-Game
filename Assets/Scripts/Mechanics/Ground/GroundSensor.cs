using UnityEngine;

// Feels the ground under an entity's feet (TerrainMap) and hands its movers the result:
// a speed multiplier, the traction (SlipTime) and how far a shove slides
// (KnockbackSlide). It samples only once the feet have moved about a pixel, at most
// once a frame, and eases the speed so stepping across a ragged patch edge doesn't
// stutter. Movers fetch it with On(gameObject), which adds one if missing, so no prefab
// needs it set up. Without a TerrainMap in the scene the ground has no effect.
public class GroundSensor : MonoBehaviour
{
    [Tooltip("Where the ground is felt. Falls back to this transform (entities' pivots are at the feet).")]
    [SerializeField] Transform feet;
    [Tooltip("Seconds the speed multiplier takes to ease to a new ground's.")]
    [SerializeField] float easeTime = 0.15f;

    const float SampleStep = 1f / 32f;   // one art pixel

    public GroundType Ground { get; private set; }
    public float SpeedMultiplier { get; private set; } = 1f;
    public float SlipTime { get; private set; }
    public float KnockbackSlide { get; private set; } = 1f;

    GroundProps props = GroundProps.Neutral;
    Vector2 lastSample;
    bool sampled;
    float speedVel;

    public static GroundSensor On(GameObject go) =>
        go.TryGetComponent(out GroundSensor sensor) ? sensor : go.AddComponent<GroundSensor>();

    void Update()
    {
        Vector2 at = feet != null ? feet.position : transform.position;
        bool first = !sampled;
        if (first || (at - lastSample).sqrMagnitude >= SampleStep * SampleStep)
        {
            sampled = true;
            lastSample = at;
            TerrainMap map = TerrainMap.Instance;
            props = map != null ? map.PropsAt(at) : GroundProps.Neutral;
            Ground = props.type;
            SlipTime = props.slipTime;
            KnockbackSlide = props.knockbackSlide;
        }
        // the first ground applies at once (spawning on snow isn't a slowdown)
        SpeedMultiplier = first ? props.speed : Mathf.SmoothDamp(SpeedMultiplier, props.speed, ref speedVel, easeTime);
    }
}

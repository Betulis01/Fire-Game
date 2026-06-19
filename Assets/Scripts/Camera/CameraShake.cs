using UnityEngine;

// Decaying positional camera shake. CameraFollow adds Offset to the camera's final
// position each frame (so position has a single writer); this just maintains the
// offset from a trauma value. Call Shake() to add trauma — combat does so via
// CameraShakeOnHit. Trauma is eased (squared) so light taps stay subtle.
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Tooltip("Max positional offset at full trauma (world units).")]
    public float maxOffset = 0.12f;
    [Tooltip("Trauma (0..1) lost per second.")]
    public float recovery = 2f;
    [Tooltip("How fast the shake jitters.")]
    public float frequency = 22f;

    public Vector3 Offset { get; private set; }

    float trauma;
    float seedX, seedY;

    void Awake()
    {
        Instance = this;
        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    // Add trauma; bigger hits can pass a bigger amount. Clamped to 1.
    public void Shake(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    void LateUpdate()
    {
        if (trauma <= 0f) { Offset = Vector3.zero; return; }

        float s = trauma * trauma;                       // ease: light trauma is gentle
        float t = Time.unscaledTime * frequency;
        Offset = new Vector3(
            Mathf.PerlinNoise(seedX, t) * 2f - 1f,
            Mathf.PerlinNoise(seedY, t) * 2f - 1f,
            0f) * (maxOffset * s);

        trauma = Mathf.Max(0f, trauma - recovery * Time.deltaTime);
    }
}

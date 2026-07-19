using UnityEngine;

// Victim-side reactor: swaps the sprite to a solid-white unlit material for a split
// second whenever this entity takes a hit. Hurtbox discovers it through IHitReactor,
// so it needs no wiring into the hit pipeline. A material swap is used instead of
// SpriteRenderer.color because color multiplies the texture — it can never whiten it.
public class FlashOnHit : MonoBehaviour, IHitReactor
{
    [Tooltip("Unlit solid-white material shown during the flash (SpriteWhiteFlash).")]
    [SerializeField] Material flashMaterial;

    [Tooltip("How long the sprite stays white, in seconds.")]
    [SerializeField] float duration = 0.07f;

    [Tooltip("Renderer to flash. Defaults to the SpriteRenderer on this GameObject.")]
    [SerializeField] SpriteRenderer target;

    Material originalMaterial;
    float elapsed;
    bool flashing;

    void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target != null) originalMaterial = target.sharedMaterial;
    }

    public void OnHit(in HitInfo hit)
    {
        if (target == null || flashMaterial == null) return;
        target.sharedMaterial = flashMaterial;
        elapsed = 0f;
        flashing = true;
    }

    void Update()
    {
        if (!flashing) return;
        elapsed += Time.deltaTime;
        if (elapsed < duration) return;
        target.sharedMaterial = originalMaterial;
        flashing = false;
    }
}

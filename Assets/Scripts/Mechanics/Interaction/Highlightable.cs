using UnityEngine;

// Lights up an "interact" overlay sprite while the player is focused on this object.
// PlayerInteractor toggles it for whatever is showing an interact prompt (E + Q alike).
// When `variants` is set, the overlay auto-matches whichever base sprite RandomSprite
// randomly picked at spawn, so the correct hand-drawn outline follows the variant.
[DisallowMultipleComponent]
public class Highlightable : MonoBehaviour
{
    [System.Serializable]
    struct Variant
    {
        public Sprite baseSprite;   // a sprite RandomSprite may assign to the base renderer
        public Sprite highlight;    // matching interact-layer overlay (null = no outline)
    }

    [Tooltip("Interact-layer sprite shown while focused. Starts hidden.")]
    [SerializeField] SpriteRenderer overlay;

    [Tooltip("Renderer whose (possibly randomized) sprite selects the overlay variant. " +
             "Defaults to a SpriteRenderer on this GameObject.")]
    [SerializeField] SpriteRenderer baseRenderer;

    [Tooltip("Optional: pair each random base sprite with its outline. When set, the " +
             "overlay follows the spawned variant. Leave empty for a fixed overlay sprite.")]
    [SerializeField] Variant[] variants;

    void Awake() => Set(false);

    // Runs after every Awake, so RandomSprite has already assigned the base sprite.
    void Start()
    {
        if (variants == null || variants.Length == 0 || overlay == null) return;

        SpriteRenderer source = baseRenderer != null ? baseRenderer : GetComponent<SpriteRenderer>();
        if (source == null) return;

        Sprite current = source.sprite;
        overlay.sprite = null;   // no matching variant => no outline (safe default)
        foreach (Variant v in variants)
            if (v.baseSprite == current) { overlay.sprite = v.highlight; break; }
    }

    public void Set(bool on)
    {
        if (overlay != null) overlay.enabled = on;
    }
}

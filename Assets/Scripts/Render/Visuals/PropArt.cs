using UnityEngine;

// A prop's art is the Aseprite importer's own model prefab, nested as the "Art"
// child: one SpriteRenderer per Aseprite layer (Snow, Stone, Highlight, Shadow, ...)
// under a SortingGroup, plus an Animator playing the importer's clips (one per tag,
// keying every layer). The importer rebuilds all of it on every save in Aseprite, so
// nothing here is hand-linked: the art is found by name at runtime, never through a
// serialized reference into it, and adding layers or frames can't leave one dangling.
// The only references are to whole sub-assets (the model and its tag clips), which
// the importer keys by name -- they hold until a layer or tag is renamed.
//
//   - Body: the base layer's renderer (bodyLayer), what icons, the fade and the
//     shadow treat as "the" sprite. A one-layer file's art root is the renderer.
//   - controller: the prop's own controller (idle / hit / collide and their
//     triggers), whose states play the importer's clips. Assigned on Awake; without
//     one the importer's own controller stays (RandomVariant uses it).
// Also attaches TargetHighlight when the art has a Highlight layer, so the outline
// is owned (hidden) from the start.
//
// Tools/Aseprite/Convert Props To Importer Art (PropArtConverter) sets a prop up.
public class PropArt : MonoBehaviour
{
    public const string ArtName = "Art";
    public const string ShadowLayer = "Shadow";   // a drawn ground shadow: SpriteShadow doesn't cast it

    [Tooltip("The Aseprite layer that is the prop's body, e.g. \"Stone\" or \"Layer 1\".")]
    [SerializeField] string bodyLayer = "Layer 1";

    [Tooltip("The prop's controller; its states play the importer's tag clips. Optional.")]
    [SerializeField] RuntimeAnimatorController controller;

    Transform art;
    SpriteRenderer body;
    Animator animator;
    SpriteRenderer[] renderers;

    public Transform Art => art != null ? art : art = transform.Find(ArtName);
    public SpriteRenderer Body => body != null ? body : body = FindBody(Art, bodyLayer);
    public Animator Animator => animator != null ? animator : animator = Art != null ? Art.GetComponent<Animator>() : null;
    public SpriteRenderer[] Renderers =>
        renderers ??= Art != null ? Art.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];

    // The art's renderer for the given Aseprite layer; null when there's none.
    public SpriteRenderer Layer(string layerName)
    {
        foreach (SpriteRenderer sr in Renderers)
            if (sr.gameObject.name == layerName) return sr;
        return null;
    }

    // The body of a prop -- an instance or a prefab asset (no Awake needed) -- or null
    // when it has no PropArt.
    public static SpriteRenderer BodyOf(GameObject prop) =>
        prop != null && prop.TryGetComponent(out PropArt pa) ? FindBody(pa.transform.Find(ArtName), pa.bodyLayer) : null;

    static SpriteRenderer FindBody(Transform art, string layerName)
    {
        if (art == null) return null;
        Transform layer = art.Find(layerName);
        if (layer != null && layer.TryGetComponent(out SpriteRenderer sr)) return sr;
        return art.GetComponent<SpriteRenderer>();   // a one-layer file: the art root is the layer
    }

    void Awake()
    {
        if (controller != null && Animator != null) Animator.runtimeAnimatorController = controller;
        TargetHighlight.Attach(gameObject);
    }
}

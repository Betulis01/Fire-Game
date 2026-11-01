using UnityEngine;

// A structure's own sounds (sound.md §8): finishing being built -- the campfire's first
// flame, the furnace's stones settling, the workbench's hammering, after BuildAudio's
// shared flourish -- and, for the workbench, E opening and closing the crafting menu
// there. A placement ghost keeps this disabled until it's built, and Complete enables
// it before raising PlacementGhost.Completed, so it hears its own completion.
public class StructureAudio : MonoBehaviour
{
    public SoundEvent buildComplete;
    public SoundEvent open;
    public SoundEvent close;

    WorkbenchInteractable bench;

    void Awake() => bench = GetComponent<WorkbenchInteractable>();

    void OnEnable()
    {
        PlacementGhost.Completed += OnCompleted;
        if (bench != null) { bench.Opened += OnOpened; bench.Closed += OnClosed; }
    }

    void OnDisable()
    {
        PlacementGhost.Completed -= OnCompleted;
        if (bench != null) { bench.Opened -= OnOpened; bench.Closed -= OnClosed; }
    }

    void OnCompleted(GameObject built)
    {
        if (built == gameObject) Sfx.Play(buildComplete, transform.position);
    }

    void OnOpened() => Sfx.Play(open, transform.position);
    void OnClosed() => Sfx.Play(close, transform.position);
}

using UnityEngine;

// Base for anything the player can interact with by pressing E.
// Add a subclass + a 2D collider to an object to make it interactable;
// PlayerInteractor finds it, shows the prompt, and routes the E press here.
public abstract class Interactable : MonoBehaviour
{
    // how high above the anchor the "E" prompt floats, in world units
    public float promptYOffset = 0.5f;

    // where the prompt sits; override if the "E" should hover over a child sprite
    public virtual Transform PromptAnchor => transform;

    // world position the prompt should be drawn at
    public Vector3 PromptPosition => PromptAnchor.position + Vector3.up * promptYOffset;

    // false hides the prompt and blocks the E press (e.g. nothing left to give)
    public virtual bool CanInteract(PlayerController player) => true;

    // what actually happens on E
    public abstract void Interact(PlayerController player);
}

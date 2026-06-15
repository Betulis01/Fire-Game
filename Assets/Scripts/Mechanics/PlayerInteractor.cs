using System.Collections.Generic;
using UnityEngine;

// Drives the whole interaction loop from the player's side:
// tracks interactables in range, focuses the nearest usable one, floats the "E"
// prompt over it, and routes the E press into it. The player never needs to know
// what the interactable actually is.
[RequireComponent(typeof(PlayerController), typeof(Hands))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("World-space pickup prompt prefab; one instance is spawned and reused.")]
    public InteractPrompt promptPrefab;

    readonly HashSet<Interactable> inRange = new HashSet<Interactable>();
    PlayerController player;
    Hands hands;
    InteractPrompt prompt;
    Interactable current;

    void Start()
    {
        player = GetComponent<PlayerController>();
        hands = GetComponent<Hands>();
        if (promptPrefab != null)
            prompt = Instantiate(promptPrefab);
    }

    void Update()
    {
        current = FindNearestUsable();

        if (prompt != null)
        {
            if (current != null) prompt.Show(current.PromptPosition);
            else prompt.Hide();
        }

        // Q drives the left hand, E the right: drop if that hand is full,
        // otherwise pick up the nearest item into it.
        if (Input.GetKeyDown(KeyCode.Q)) UseHand(HandSide.Left);
        if (Input.GetKeyDown(KeyCode.E)) UseHand(HandSide.Right);
    }

    void UseHand(HandSide side)
    {
        // try to pick up / stack from the nearest item first; if nothing was taken
        // (no item in range, or it doesn't stack into this hand) and the hand is
        // full, drop one instead.
        if (current != null && current.Interact(player, side)) return;
        if (hands.IsHolding(side)) hands.Drop(side);
    }

    // closest interactable that currently allows interaction; null if none
    Interactable FindNearestUsable()
    {
        Interactable nearest = null;
        float best = float.MaxValue;

        foreach (Interactable it in inRange)
        {
            // collider may have been deactivated/destroyed (e.g. picked-up wood)
            if (it == null || !it.isActiveAndEnabled) continue;
            if (!it.CanInteract(player)) continue;

            float d = (it.PromptAnchor.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = it;
            }
        }

        return nearest;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable it = other.GetComponentInParent<Interactable>();
        if (it != null) inRange.Add(it);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Interactable it = other.GetComponentInParent<Interactable>();
        if (it != null) inRange.Remove(it);
    }
}

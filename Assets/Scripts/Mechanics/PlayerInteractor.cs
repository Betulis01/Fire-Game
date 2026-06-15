using System.Collections.Generic;
using UnityEngine;

// Drives the whole interaction loop from the player's side:
// tracks interactables in range, focuses the nearest usable one, floats the "E"
// prompt over it, and routes the E press into it. The player never needs to know
// what the interactable actually is.
[RequireComponent(typeof(PlayerController), typeof(Inventory))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("World-space 'E' prompt prefab; one instance is spawned and reused.")]
    public InteractPrompt promptPrefab;

    readonly HashSet<Interactable> inRange = new HashSet<Interactable>();
    PlayerController player;
    InteractPrompt prompt;
    Interactable current;

    void Start()
    {
        player = GetComponent<PlayerController>();
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

        if (current != null && Input.GetKeyDown(KeyCode.E))
            current.Interact(player);
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

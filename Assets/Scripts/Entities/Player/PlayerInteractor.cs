using System;
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

    public HandSide ActiveHand { get; private set; } = HandSide.Right;
    public event Action<HandSide> ActiveHandChanged;

    readonly HashSet<Interactable> inRange = new HashSet<Interactable>();
    PlayerController player;
    Hands hands;
    InteractPrompt prompt;
    Interactable current;
    Interactable prevCurrent;
    int interactableLayer;

    void Start()
    {
        player = GetComponent<PlayerController>();
        hands = GetComponent<Hands>();
        interactableLayer = LayerMask.NameToLayer("Interactable");
        if (promptPrefab != null)
            prompt = Instantiate(promptPrefab);
    }

    void Update()
    {
        current = FindNearestUsable();

        // Close a station menu if the player moved focus away from it.
        if (current != prevCurrent && prevCurrent is StationInteractable prev)
            prev.Close();
        prevCurrent = current;

        // Pause also closes any open station menu.
        if (UserInput.Instance.Pause && current is StationInteractable focused && focused.IsOpen)
            focused.Close();

        // 1/2 selects active hand.
        if (UserInput.Instance.SelectLeft) SetActiveHand(HandSide.Left);
        if (UserInput.Instance.SelectRight) SetActiveHand(HandSide.Right);

        if (prompt != null)
        {
            bool menuOpen = current is StationInteractable s && s.IsOpen;
            if (current != null && !menuOpen) prompt.Show(current.PromptPosition, current.promptText);
            else prompt.Hide();
        }

        // Q drops from active hand; E interacts or picks up into active hand.
        if (UserInput.Instance.InteractLeft) hands.Drop(ActiveHand);
        if (UserInput.Instance.InteractRight) UseHand(ActiveHand);
    }

    void UseHand(HandSide side)
    {
        if (current == null) return;
        if (current.Interact(player, side)) return;
        if (hands.IsHolding(side))
        {
            HandSide other = side == HandSide.Left ? HandSide.Right : HandSide.Left;
            current.Interact(player, other);
        }
    }

    void SetActiveHand(HandSide side)
    {
        if (ActiveHand == side) return;
        ActiveHand = side;
        ActiveHandChanged?.Invoke(side);
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

    // Interaction range is defined by each item's dedicated "InteractZone" child
    // (a generous trigger on the Interactable layer), not by its physics/hurtbox
    // collider. Ignoring every other collider keeps range to a single zone per item
    // so there are no dead-zone bands between two differently sized colliders.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != interactableLayer) return;
        Interactable it = other.GetComponentInParent<Interactable>();
        if (it != null) inRange.Add(it);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != interactableLayer) return;
        Interactable it = other.GetComponentInParent<Interactable>();
        if (it != null) inRange.Remove(it);
    }
}

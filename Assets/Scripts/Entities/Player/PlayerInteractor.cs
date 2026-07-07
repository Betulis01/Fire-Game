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

    [Tooltip("Max distance from the player the drop ghost can be placed.")]
    public float maxDropDistance = 1.5f;

    public HandSide ActiveHand { get; private set; } = HandSide.Left;
    public event Action<HandSide> ActiveHandChanged;

    readonly HashSet<Interactable> inRange = new HashSet<Interactable>();
    PlayerController player;
    Hands hands;
    InteractPrompt prompt;
    DropGhost dropGhost;
    ItemReceiver currentReceiver;   // receiver whose deposit prompt is currently shown
    Interactable current;
    Interactable prevCurrent;
    int interactableLayer;

    readonly HashSet<Highlightable> lit = new();       // currently-lit overlays
    readonly HashSet<Highlightable> desired = new();   // reused scratch, per frame

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

        // 1/2 selects active hand. Switching hands cancels an in-progress drop so
        // the ghost never previews the wrong hand's item.
        if (UserInput.Instance.SelectLeft) { CancelDropGhost(); SetActiveHand(HandSide.Left); }
        if (UserInput.Instance.SelectRight) { CancelDropGhost(); SetActiveHand(HandSide.Right); }
        if (UserInput.Instance.Pause) CancelDropGhost();

        bool menuOpen = current is StationInteractable s && s.IsOpen;

        Interactable promptInteractable = (current != null && !menuOpen) ? current : null;
        if (prompt != null)
        {
            if (promptInteractable != null) prompt.Show(promptInteractable.PromptPosition, promptInteractable.promptText);
            else prompt.Hide();
        }

        // A deposit target the player is standing in while holding an accepted item
        // (fire to feed, build site to supply). Drives the drop-ghost / Q-deposit.
        WorldItem held = hands.Held(ActiveHand)?.GetComponent<WorldItem>();
        ItemReceiver inZone = menuOpen ? null : ItemReceiver.FindInZone(transform.position, held);

        // The prompt can advertise more than we can deposit into right now: a placed
        // build site shows what it still needs even when the player is empty-handed.
        ItemReceiver promptZone = menuOpen ? null : ItemReceiver.FindPromptTarget(transform.position, held);
        UpdateReceiverPrompt(promptZone, held);

        // Light the interact overlay on whatever is currently showing a prompt.
        UpdateHighlights(promptInteractable, promptZone);

        // Hold Q to aim a drop ghost, release to commit. E interacts / picks up.
        UpdateDropGhost(inZone, held);
        if (UserInput.Instance.InteractRight) UseHand(ActiveHand);
    }

    // Show the in-zone receiver's prompt (and hide the previous one when it changes).
    void UpdateReceiverPrompt(ItemReceiver inZone, WorldItem held)
    {
        if (inZone != currentReceiver && currentReceiver != null) currentReceiver.HidePrompt();
        currentReceiver = inZone;
        if (inZone != null) inZone.ShowPrompt(held);
    }

    // Light the interact overlay on whatever is showing a prompt (the focused E
    // interactable and/or the Q receiver zone) and clear it from anything that no longer
    // is. Set-based so an object that is BOTH (a station: Interactable + FuelReceiver) is
    // lit once and stays lit until neither channel wants it.
    void UpdateHighlights(Interactable interactable, ItemReceiver receiver)
    {
        desired.Clear();
        AddHighlightable(interactable);
        AddHighlightable(receiver);

        // null-guard: the focused object may have been destroyed (e.g. picked-up item)
        foreach (Highlightable h in lit)
            if (h != null && !desired.Contains(h)) h.Set(false);

        foreach (Highlightable h in desired)
            if (!lit.Contains(h)) h.Set(true);

        lit.Clear();
        lit.UnionWith(desired);
    }

    void AddHighlightable(Component source)
    {
        if (source == null) return;
        Highlightable h = source.GetComponent<Highlightable>();
        if (h != null) desired.Add(h);
    }

    // Q held -> spawn/track a translucent preview of the active-hand item at the
    // aimed position; Q released -> deposit into a receiver if released over one,
    // else drop. When standing in a receiver's zone holding an accepted item, a Q
    // press deposits directly instead of starting a drop -- depositing takes
    // priority in the zone.
    void UpdateDropGhost(ItemReceiver inZone, WorldItem held)
    {
        if (UserInput.Instance.InteractLeft && inZone != null && dropGhost == null)
        {
            if (inZone.Deposit(held)) hands.Consume(ActiveHand, 1);
            return;
        }

        if (UserInput.Instance.InteractLeft && dropGhost == null && held != null)
            dropGhost = DropGhost.Begin(held.item.prefab);

        if (dropGhost == null) return;

        // safety: item left the hand mid-drag (crafted, consumed, ...)
        if (!hands.IsHolding(ActiveHand)) { CancelDropGhost(); return; }

        Vector3 pos = DropPreviewPosition();
        dropGhost.MoveTo(pos);

        if (UserInput.Instance.InteractLeftReleased || UserInput.Instance.Attack)
        {
            ResolveDrop(pos);
            CancelDropGhost();
        }
    }

    void ResolveDrop(Vector3 pos)
    {
        // Deposit when the ghost is released over a receiver that accepts the item,
        // no matter how far the player is standing; otherwise drop at that position.
        WorldItem held = hands.Held(ActiveHand)?.GetComponent<WorldItem>();
        ItemReceiver receiver = ItemReceiver.FindInZone(pos, held);
        if (receiver != null && receiver.Deposit(held)) hands.Consume(ActiveHand, 1);
        else hands.DropAt(ActiveHand, pos);
    }

    void CancelDropGhost()
    {
        if (dropGhost == null) return;
        dropGhost.Dismiss();
        dropGhost = null;
    }

    // Where the drop ghost sits: under the cursor, clamped to maxDropDistance.
    Vector3 DropPreviewPosition() =>
        UserInput.Instance.AimPoint(transform.position, hands.cam, maxDropDistance);

    void UseHand(HandSide side)
    {
        if (current == null) return;
        if (current.Interact(player, side)) return;
        if (hands.IsHolding(side))
        {
            HandSide other = side == HandSide.Left ? HandSide.Right : HandSide.Left;
            if (current.Interact(player, other)) return;
            // both hands full and neither could take it -> swap into the active hand
            current.Swap(player, side);
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

using System.Collections.Generic;
using UnityEngine;

// Drives the whole interaction loop from the player's side:
// tracks interactables in range, focuses the one the player aims at, floats the "E"
// prompt over it, and routes E into it. The player never needs to know what the
// interactable actually is.
//
// Focus: of the usable interactables in range, the one whose prompt anchor is nearest
// the aim point (cursor, or the stick) wins, and keeps focus until a rival is
// stickyMargin closer, so near-equal items don't flicker. Gear the player is missing
// (it would go straight into an empty loadout or armour slot) counts as closer, an
// item they just dropped as further.
//
// E: on gear, E acts on release -- a tap takes it (Inventory routing: into the bag),
// holding it for holdTime equips it there and then (Inventory.TryEquipFromWorld),
// with the prompt's bar filling meanwhile. Anything else acts on the press.
//
// Q, on the item in the hand filled last (Hands.TryGetLastFilled): a tap deposits it
// into a receiver zone the player stands in (a fire, a build site), else drops it at
// their feet; holding it past throwHoldTime shows the drop ghost at the aimed spot
// (up to throwRange away) and releasing -- or a left click -- throws it there
// (ThrownItem), landing in a receiver if it falls in one.
[RequireComponent(typeof(PlayerController), typeof(Hands))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("World-space pickup prompt prefab; one instance is spawned and reused.")]
    public InteractPrompt promptPrefab;

    [Header("Focus")]
    [Tooltip("The aim point is the cursor (or stick) clamped to this distance from the player.")]
    [SerializeField] float aimReach = 2f;
    [Tooltip("How much closer to the aim a rival must be to take focus from the current one.")]
    [SerializeField] float stickyMargin = 0.15f;
    [Tooltip("Gear the player is missing counts this much closer to the aim.")]
    [SerializeField] float missingGearBias = 0.5f;
    [Tooltip("An item the player just dropped counts this much further from the aim.")]
    [SerializeField] float droppedPenalty = 0.5f;

    [Tooltip("Colour a pickup item's Highlight layer shows in while it's the E focus.")]
    [SerializeField] Color pickupHighlightColor = new(0.3f, 0.9f, 0.3f, 1f);

    [Header("E")]
    [Tooltip("Seconds E is held on gear to equip it instead of taking it.")]
    [SerializeField] float holdTime = 0.35f;
    [Tooltip("Colour of the \"Hold E: Equip\" line under a gear item's name.")]
    [SerializeField] Color holdHintColor = new(190f / 255f, 204f / 255f, 222f / 255f);

    [Header("Q")]
    [Tooltip("Seconds Q is held before a release throws instead of dropping.")]
    [SerializeField] float throwHoldTime = 0.2f;
    [Tooltip("Max distance from the player a throw can land.")]
    public float throwRange = 3f;
    [SerializeField] float throwDuration = 0.35f;
    [SerializeField] float throwArc = 0.4f;

    [Tooltip("Max distance from the player a bag item dragged into the world lands.")]
    public float maxDropDistance = 1.5f;

    readonly HashSet<Interactable> inRange = new HashSet<Interactable>();
    PlayerController player;
    Hands hands;
    Inventory inventory;
    InteractPrompt prompt;
    DropGhost dropGhost;
    ItemReceiver currentReceiver;   // receiver whose deposit prompt is currently shown
    Interactable current;
    Interactable prevCurrent;
    int interactableLayer;

    Interactable holding;   // gear E is held on (equip once holdTime passes)
    float holdStart;
    WorldItem qItem;        // the carried item Q is held on
    float qStart;

    readonly HashSet<Highlightable> lit = new();       // currently-lit overlays
    readonly HashSet<Highlightable> desired = new();   // reused scratch, per frame
    TargetHighlight litPickup;                         // the focused pickup's Highlight layer

    void Start()
    {
        player = GetComponent<PlayerController>();
        hands = GetComponent<Hands>();
        inventory = GetComponent<Inventory>();
        interactableLayer = LayerMask.NameToLayer("Interactable");
        if (promptPrefab != null)
            prompt = Instantiate(promptPrefab);
        TargetHighlight.PickupColor = pickupHighlightColor;
    }

    void Update()
    {
        current = FindAimed();

        // Close a station menu if the player moved focus away from it.
        if (current != prevCurrent && prevCurrent is StationInteractable prev)
            prev.Close();
        prevCurrent = current;

        // Pause also closes any open station menu.
        if (UserInput.Instance.Pause && current is StationInteractable focused && focused.IsOpen)
            focused.Close();

        if (UserInput.Instance.Pause) CancelQ();

        bool menuOpen = current is StationInteractable s && s.IsOpen;

        Interactable promptInteractable = (current != null && !menuOpen) ? current : null;
        if (prompt != null)
        {
            if (promptInteractable != null) prompt.Show(promptInteractable.PromptPosition, PromptText(promptInteractable));
            else prompt.Hide();
        }

        // A deposit target the player is standing in while holding an accepted item
        // (fire to feed, build site to supply). Drives the Q tap's deposit. Works
        // regardless of any open station menu, so the player can feed a station while
        // watching its gauges.
        bool carrying = hands.TryGetLastFilled(out HandSide side);
        WorldItem held = carrying ? hands.Held(side).GetComponent<WorldItem>() : null;
        ItemReceiver inZone = ItemReceiver.FindInZone(transform.position, held);

        // The prompt can advertise more than we can deposit into right now: a placed
        // build site shows what it still needs even when the player is empty-handed.
        ItemReceiver promptZone = ItemReceiver.FindPromptTarget(transform.position, held);
        UpdateReceiverPrompt(promptZone, held);

        // Light the interact overlay on whatever is currently showing a prompt.
        UpdateHighlights(promptInteractable, promptZone);

        UpdateQ(inZone, held, side);
        UpdateE();
    }

    // ------------------------------------------------------------------ E

    void UpdateE()
    {
        if (UserInput.Instance.InteractRight && current != null)
        {
            if (IsGear(current))
            {
                holding = current;
                holdStart = Time.time;
            }
            else current.Interact(player);
        }

        if (holding == null) return;

        // focus moved off it (or it's gone): nothing happens
        if (holding != current)
        {
            holding = null;
            if (prompt != null) prompt.SetHold(0f);
            return;
        }

        float t = (Time.time - holdStart) / Mathf.Max(0.0001f, holdTime);
        if (t >= 1f)
        {
            if (inventory != null && holding.TryGetComponent(out WorldItem wi)) inventory.TryEquipFromWorld(wi);
            EndHold();
        }
        else if (!UserInput.Instance.InteractRightHeld)
        {
            holding.Interact(player);   // a tap: take it
            EndHold();
        }
        else if (prompt != null) prompt.SetHold(t);
    }

    void EndHold()
    {
        holding = null;
        if (prompt != null) prompt.SetHold(0f);
    }

    static bool IsGear(Interactable it) =>
        it is Pickupable && (Loadout.Fits(it.gameObject) || it.TryGetComponent(out Equippable _));

    // gear gets a dimmer second line under its name (the prompt's text is bottom-
    // aligned, so the name sits above it)
    string PromptText(Interactable it) =>
        IsGear(it) ? $"{it.promptText}\n<color=#{ColorUtility.ToHtmlStringRGB(holdHintColor)}>Hold E: Equip</color>"
                   : it.promptText;

    // ------------------------------------------------------------------ Q

    void UpdateQ(ItemReceiver inZone, WorldItem held, HandSide side)
    {
        if (UserInput.Instance.InteractLeft && held != null && qItem == null)
        {
            qItem = held;
            qStart = Time.time;
        }
        if (qItem == null) return;

        // safety: the item left the hand mid-hold (crafted, consumed, a second pick-up
        // changed which hand Q acts on)
        if (held != qItem) { CancelQ(); return; }

        if (dropGhost == null && Time.time - qStart >= throwHoldTime)
            dropGhost = DropGhost.Begin(held.item.prefab);

        Vector3 target = UserInput.Instance.AimPoint(transform.position, hands.cam, throwRange);
        if (dropGhost != null) dropGhost.MoveTo(target);

        // LMB also commits a throw; claim the press so it doesn't put the load down too
        bool click = dropGhost != null && UserInput.Instance.UseLeft;
        if (UserInput.Instance.InteractLeftHeld && !click) return;
        if (click) UserInput.Instance.ConsumeUse(HandSide.Left);

        if (dropGhost != null) Throw(side, target);
        else if (inZone != null && inZone.Deposit(held)) hands.Consume(side, 1);
        else hands.Drop(side);
        CancelQ();
    }

    void Throw(HandSide side, Vector3 target)
    {
        GameObject item = hands.Release(side);
        if (item != null) ThrownItem.Launch(item, item.transform.position, target, throwDuration, throwArc);
    }

    void CancelQ()
    {
        qItem = null;
        if (dropGhost == null) return;
        dropGhost.Dismiss();
        dropGhost = null;
    }

    // ------------------------------------------------------------------ prompts

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
    // lit once and stays lit until neither channel wants it. A focused pickup item lights
    // the Highlight layer of its own sprite instead, green (TargetHighlight).
    void UpdateHighlights(Interactable interactable, ItemReceiver receiver)
    {
        TargetHighlight pickup = interactable is Pickupable ? TargetHighlight.Attach(interactable.gameObject) : null;
        if (pickup != litPickup)
        {
            if (litPickup != null) litPickup.SetInteract(false);   // null too once destroyed
            litPickup = pickup;
            if (pickup != null) pickup.SetInteract(true);
        }

        desired.Clear();
        if (pickup == null) AddHighlightable(interactable);
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

    // ------------------------------------------------------------------ focus

    // The usable interactable in range nearest the aim point (see the header), or null.
    Interactable FindAimed()
    {
        Vector3 aim = UserInput.Instance.AimPoint(transform.position, hands.cam, aimReach);
        Interactable best = null;
        float bestScore = float.MaxValue;
        float currentScore = float.MaxValue;

        foreach (Interactable it in inRange)
        {
            // collider may have been deactivated/destroyed (e.g. picked-up wood)
            if (it == null || !it.isActiveAndEnabled) continue;
            if (!it.CanInteract(player)) continue;

            float score = Vector2.Distance(it.PromptAnchor.position, aim) + Bias(it);
            if (it == current) currentScore = score;
            if (score < bestScore)
            {
                bestScore = score;
                best = it;
            }
        }

        // sticky: the current focus holds unless the best beats it by the margin
        if (currentScore < float.MaxValue && currentScore - bestScore < stickyMargin) return current;
        return best;
    }

    float Bias(Interactable it)
    {
        if (!(it is Pickupable) || !it.TryGetComponent(out WorldItem wi)) return 0f;
        float bias = 0f;
        if (inventory != null && inventory.IsMissingGear(wi.gameObject)) bias -= missingGearBias;
        if (wi.IsDropped) bias += droppedPenalty;
        return bias;
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

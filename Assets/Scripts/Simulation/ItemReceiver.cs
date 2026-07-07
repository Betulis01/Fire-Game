using System.Collections.Generic;
using UnityEngine;

// A place the player can deposit items into by standing in its trigger zone
// holding an accepted item and pressing the drop key (or releasing the drop ghost
// over it). PlayerInteractor drives all receivers uniformly via the static
// registry, so deposit targets don't need to be Interactables. FuelReceiver (feed
// a fire) and BuildRequirement (supply a structure) are the concrete kinds.
public abstract class ItemReceiver : MonoBehaviour
{
    // Every enabled receiver, so PlayerInteractor can find the one the player (or a
    // dropped ghost) is standing over without a scene physics query.
    public static readonly List<ItemReceiver> All = new();

    [Tooltip("World-space prompt shown while the player is in the zone holding an " +
             "accepted item. Each receiver kind supplies its own icon prefab.")]
    public InteractPrompt promptPrefab;

    [Tooltip("Offset from this transform where the prompt floats.")]
    public Vector3 promptOffset = new Vector3(0f, 0.5f, 0f);

    Collider2D[] zones;      // the trigger colliders that define the deposit area
    InteractPrompt prompt;   // lazily instantiated when first shown

    // When true, this receiver's prompt is shown whenever the player is in the zone,
    // even empty-handed (e.g. a build site advertising what it still needs). When
    // false (the default), the prompt only appears while holding an accepted item.
    protected virtual bool AdvertisesWhenEmpty => false;

    protected virtual void Awake() => RefreshZones();

    protected virtual void OnEnable() => All.Add(this);

    protected virtual void OnDisable()
    {
        All.Remove(this);
        HidePrompt();
    }

    void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }

    // Re-scan for trigger colliders. Call again after adding a collider at runtime.
    protected void RefreshZones()
    {
        List<Collider2D> triggers = new();
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            if (c.isTrigger) triggers.Add(c);
        zones = triggers.ToArray();
    }

    public bool IsInZone(Vector3 worldPos)
    {
        foreach (Collider2D c in zones)
            if (c != null && c.OverlapPoint(worldPos)) return true;
        return false;
    }

    public void ShowPrompt(WorldItem held)
    {
        if (promptPrefab == null) return;
        if (prompt == null) prompt = Instantiate(promptPrefab);
        prompt.Show(transform.position + promptOffset, PromptLabel(held));
    }

    public void HidePrompt()
    {
        if (prompt != null) prompt.Hide();
    }

    // True if this receiver would accept one of the given held item.
    public abstract bool Accepts(WorldItem item);

    // Consume one item's worth of effect (caller removes the item from the hand).
    // Returns true if a unit was accepted.
    public abstract bool Deposit(WorldItem item);

    // Label for the contextual prompt, given the currently held (accepted) item.
    protected abstract string PromptLabel(WorldItem held);

    // The enabled receiver whose zone contains pos and that accepts item, or null.
    public static ItemReceiver FindInZone(Vector3 pos, WorldItem item)
    {
        if (item == null) return null;
        foreach (ItemReceiver r in All)
            if (r.IsInZone(pos) && r.Accepts(item)) return r;
        return null;
    }

    // The receiver whose prompt should be shown at pos: prefer one that accepts the
    // held item (so the label offers a deposit action), else one that advertises
    // even empty-handed (a build site listing what it still needs). Display only —
    // deposits still go through FindInZone, which requires a held, accepted item.
    public static ItemReceiver FindPromptTarget(Vector3 pos, WorldItem held)
    {
        ItemReceiver deposit = FindInZone(pos, held);
        if (deposit != null) return deposit;
        foreach (ItemReceiver r in All)
            if (r.AdvertisesWhenEmpty && r.IsInZone(pos)) return r;
        return null;
    }
}

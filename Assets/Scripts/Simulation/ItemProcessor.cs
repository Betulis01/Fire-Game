using System.Collections.Generic;
using UnityEngine;

// Turns queued input items into output items over time on a fueled station.
// Progress only ticks while the station's Fuel is burning; when the fire goes
// out the current item pauses (never resets) until it's refueled. Items are
// processed one at a time in deposit order, and each finished output is held in
// the station's output (the station panel's output box) until the player takes it.
// While the output is full, or holds a different item than the next job makes, the
// current item waits at full progress.
// Subclasses only define which items are accepted and what they become
// (SmeltingProcessor for the furnace, CookingProcessor for the campfire).
[RequireComponent(typeof(Fuel))]
public abstract class ItemProcessor : MonoBehaviour
{
    protected class Job
    {
        public ItemDefinition input;
        public ItemDefinition output;
        public float duration;
        public Sprite icon;   // taken from the deposited instance for UI display
    }

    [Tooltip("Max items waiting in the input, including the one in progress.")]
    public int queueCapacity = 8;

    [Tooltip("Max finished items held in the output before processing waits.")]
    public int outputCapacity = 8;

    Fuel fuel;
    readonly List<Job> queue = new();
    float elapsed;   // time spent burning on the front job

    public int QueueCount => queue.Count;
    public bool HasRoom => queue.Count < queueCapacity;
    public ItemDefinition CurrentInput => queue.Count > 0 ? queue[0].input : null;
    public Sprite CurrentIcon => queue.Count > 0 ? queue[0].icon : null;

    // the finished items waiting to be taken (OutputItem is null when empty)
    public ItemDefinition OutputItem { get; private set; }
    public int OutputCount { get; private set; }

    // 0..1 progress of the item currently being processed (0 when idle)
    public float Progress =>
        queue.Count > 0 && queue[0].duration > 0f
            ? Mathf.Clamp01(elapsed / queue[0].duration) : 0f;

    protected virtual void Awake() => fuel = GetComponent<Fuel>();

    void Update()
    {
        if (queue.Count == 0 || fuel.fuelLevel <= 0f) return;   // pause while the fire is out

        Job job = queue[0];
        elapsed = Mathf.Min(elapsed + Time.deltaTime, job.duration);
        if (elapsed < job.duration || !OutputTakes(job.output)) return;   // done, waits for room

        queue.RemoveAt(0);
        elapsed = 0f;
        if (job.output == null) return;
        OutputItem = job.output;
        OutputCount++;
    }

    bool OutputTakes(ItemDefinition item) =>
        item == null || OutputCount == 0 || (OutputItem == item && OutputCount < outputCapacity);

    // Move the output into the player's bag (a Big item into a free hand), one at a
    // time until it's empty or they have no room; the rest stays. Returns how many.
    public int TakeOutput(Inventory inventory)
    {
        int taken = 0;
        while (OutputCount > 0 && inventory != null && OutputItem.prefab != null)
        {
            if (!OutputItem.IsBig && !inventory.HasRoomFor(OutputItem, null)) break;

            GameObject item = Instantiate(OutputItem.prefab, transform.position, Quaternion.identity);
            if (!inventory.TryTake(item.GetComponent<WorldItem>()))
            {
                Destroy(item);
                break;
            }
            taken++;
            if (--OutputCount == 0) OutputItem = null;
        }
        return taken;
    }

    // True if this station can convert the item (ignores queue space).
    public abstract bool CanProcess(WorldItem item);

    // The conversion for an accepted item, or null if it can't be processed.
    protected abstract Job MakeJob(WorldItem item);

    // Queue one item's worth of processing; the caller removes it from the hand.
    public bool TryEnqueue(WorldItem item)
    {
        if (!HasRoom || item == null) return false;
        Job job = MakeJob(item);
        if (job == null) return false;

        // (true): an item fed from the bag is inactive
        SpriteRenderer sr = PropArt.BodyOf(item.gameObject);
        if (sr == null) sr = item.GetComponentInChildren<SpriteRenderer>(true);
        job.icon = sr != null ? sr.sprite : null;
        queue.Add(job);
        return true;
    }
}

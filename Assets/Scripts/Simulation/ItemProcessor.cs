using System.Collections.Generic;
using UnityEngine;

// Turns queued input items into output items over time on a fueled station.
// Progress only ticks while the station's Fuel is burning; when the fire goes
// out the current item pauses (never resets) until it's refueled. Items are
// processed one at a time in deposit order, and each finished output is
// spawned into the world at outputOffset from the station.
// Subclasses only define which items are accepted and what they become
// (SmeltingProcessor now, a cooking processor for the campfire later).
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

    [Tooltip("Where finished output spawns, relative to the station.")]
    public Vector2 outputOffset = new Vector2(0f, -0.5f);

    Fuel fuel;
    readonly List<Job> queue = new();
    float elapsed;   // time spent burning on the front job

    public int QueueCount => queue.Count;
    public bool HasRoom => queue.Count < queueCapacity;
    public ItemDefinition CurrentInput => queue.Count > 0 ? queue[0].input : null;
    public Sprite CurrentIcon => queue.Count > 0 ? queue[0].icon : null;

    // 0..1 progress of the item currently being processed (0 when idle)
    public float Progress =>
        queue.Count > 0 && queue[0].duration > 0f
            ? Mathf.Clamp01(elapsed / queue[0].duration) : 0f;

    protected virtual void Awake() => fuel = GetComponent<Fuel>();

    void Update()
    {
        if (queue.Count == 0 || fuel.fuelLevel <= 0f) return;   // pause while the fire is out

        elapsed += Time.deltaTime;
        if (elapsed < queue[0].duration) return;

        Finish(queue[0]);
        queue.RemoveAt(0);
        elapsed = 0f;
    }

    void Finish(Job job)
    {
        if (job.output == null || job.output.prefab == null) return;
        Instantiate(job.output.prefab,
            transform.position + (Vector3)outputOffset, Quaternion.identity);
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

        SpriteRenderer sr = item.GetComponentInChildren<SpriteRenderer>();
        job.icon = sr != null ? sr.sprite : null;
        queue.Add(job);
        return true;
    }
}

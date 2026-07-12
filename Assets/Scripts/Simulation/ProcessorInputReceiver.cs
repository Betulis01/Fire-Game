using UnityEngine;

// Lets the player Q-feed processable items into a station (ore into a furnace,
// food onto a campfire) the same way fuel is fed. Pure routing: accepts
// whatever the station's ItemProcessor can convert and enqueues it there.
[RequireComponent(typeof(ItemProcessor))]
public class ProcessorInputReceiver : ItemReceiver
{
    [Tooltip("Verb shown in the deposit prompt, e.g. \"Smelt\" or \"Cook\".")]
    public string verb = "Smelt";

    ItemProcessor processor;

    protected override void Awake()
    {
        base.Awake();
        processor = GetComponent<ItemProcessor>();
    }

    public override bool Accepts(WorldItem item) =>
        processor.HasRoom && processor.CanProcess(item);

    public override bool Deposit(WorldItem item) => processor.TryEnqueue(item);

    protected override string PromptLabel(WorldItem held) =>
        $"{verb} {held.item.displayName}";
}

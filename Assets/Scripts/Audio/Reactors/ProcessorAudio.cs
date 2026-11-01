using UnityEngine;

// A station's processing, heard (sound.md §8.1 cooking, §8.2 smelting): a loop while a
// job progresses (a job queued, the fire burning, room for its output), the "done" as
// its output lands in the output box, a knock when a finished job is held back by a
// full output (once per stall), and the input going in (ore into the crucible). Fuel
// going in is the fire's own (FireAudio). Beside ItemProcessor on the campfire / furnace.
[RequireComponent(typeof(ItemProcessor))]
public class ProcessorAudio : MonoBehaviour
{
    public SoundEvent workingLoop;
    public SoundEvent done;
    public SoundEvent stalled;
    public SoundEvent input;

    ItemProcessor processor;
    ProcessorInputReceiver receiver;
    SoundHandle loop;

    void Awake()
    {
        processor = GetComponent<ItemProcessor>();
        receiver = GetComponent<ProcessorInputReceiver>();
    }

    void OnEnable()
    {
        processor.WorkingChanged += OnWorking;
        processor.JobFinished += OnDone;
        processor.Stalled += OnStalled;
        if (receiver != null) receiver.Deposited += OnInput;
    }

    void OnDisable()
    {
        processor.WorkingChanged -= OnWorking;
        processor.JobFinished -= OnDone;
        processor.Stalled -= OnStalled;
        if (receiver != null) receiver.Deposited -= OnInput;
        loop.Stop(0.2f);
    }

    void OnWorking(bool working)
    {
        if (!working) { loop.Stop(0.4f); return; }
        if (!loop.IsValid && workingLoop != null) loop = Sfx.PlayLoop(workingLoop, transform);
    }

    void OnDone(ItemDefinition output) => Sfx.Play(done, transform.position);
    void OnStalled() => Sfx.Play(stalled, transform.position);
    void OnInput(ItemDefinition item) => Sfx.Play(input, transform.position);
}

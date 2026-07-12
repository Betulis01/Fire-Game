using UnityEngine;

// Furnace wiring for ItemProcessor: accepts items with a Smeltable and turns
// them into that Smeltable's output over its smeltTime.
public class SmeltingProcessor : ItemProcessor
{
    public override bool CanProcess(WorldItem item)
    {
        if (item == null) return false;
        Smeltable s = item.GetComponent<Smeltable>();
        return s != null && s.output != null;
    }

    protected override Job MakeJob(WorldItem item)
    {
        Smeltable s = item != null ? item.GetComponent<Smeltable>() : null;
        if (s == null || s.output == null) return null;
        return new Job { input = item.item, output = s.output, duration = s.smeltTime };
    }
}

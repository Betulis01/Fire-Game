using UnityEngine;

// Campfire wiring for ItemProcessor: accepts items with a Cookable and turns
// them into that Cookable's output over its cookTime.
public class CookingProcessor : ItemProcessor
{
    public override bool CanProcess(WorldItem item)
    {
        if (item == null) return false;
        Cookable c = item.GetComponent<Cookable>();
        return c != null && c.output != null;
    }

    protected override Job MakeJob(WorldItem item)
    {
        Cookable c = item != null ? item.GetComponent<Cookable>() : null;
        if (c == null || c.output == null) return null;
        return new Job { input = item.item, output = c.output, duration = c.cookTime };
    }
}

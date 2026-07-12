using UnityEngine;

// Marks an item prefab as smeltable in a furnace: what it becomes and how
// long it takes. A ProcessorInputReceiver + SmeltingProcessor consume it.
public class Smeltable : MonoBehaviour
{
    public ItemDefinition output;
    public float smeltTime = 10f;   // seconds of burning fire to finish
}

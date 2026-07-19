using UnityEngine;

// Marks an item prefab as cookable on a campfire: what it becomes and how
// long it takes. A ProcessorInputReceiver + CookingProcessor consume it.
public class Cookable : MonoBehaviour
{
    public ItemDefinition output;
    public float cookTime = 8f;   // seconds of burning fire to finish
}

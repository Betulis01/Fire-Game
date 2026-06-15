using UnityEngine;

// Identity for an inventory item: a stable key, a display name, and the prefab
// that represents it. Item *behaviors* (fuel, weapon, ...) live as components on
// that prefab, not here. Create assets via Create -> Items -> Item Definition.
[CreateAssetMenu(menuName = "Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string id;            // stable key, e.g. "Wood"
    public string displayName;
    public GameObject prefab;
}

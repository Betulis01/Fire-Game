using UnityEngine;

// Data-only identity for an inventory item. Create assets via
// Create -> Fire Game -> Item Definition, one per item type (Wood, Stone, ...).
[CreateAssetMenu(menuName = "Fire Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string id;            // stable key, e.g. "Wood"
    public string displayName;
    public GameObject prefab;         
}

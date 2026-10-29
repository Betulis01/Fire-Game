using UnityEngine;

// Where an item travels: Small items go into the bag (gear included, one slot each),
// Big ones (logs, stone) are carried in the hands, one per hand.
public enum ItemSize { Small, Big }

// Identity for an inventory item: a stable key, a display name, the prefab that
// represents it, and its size (bag or hands). Item *behaviors* (fuel, weapon, ...)
// live as components on that prefab, not here. Create assets via Create -> Items ->
// Item Definition.
[CreateAssetMenu(menuName = "Items/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string id;            // stable key, e.g. "Wood"
    public string displayName;
    public GameObject prefab;
    public Sprite icon;          // optional; falls back to prefab's SpriteRenderer sprite if unset
    public ItemSize size;        // Small = bag, Big = carried in the hands

    public bool IsBig => size == ItemSize.Big;

    // The same item for recipes and build sites: this asset, or another with the same
    // id (Scots Pine and Grey Alder logs are both "Wood").
    public bool Matches(ItemDefinition other) =>
        other != null && (other == this || (!string.IsNullOrEmpty(id) && other.id == id));

    public Sprite ResolveIcon()
    {
        if (icon != null) return icon;
        if (prefab == null) return null;
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}

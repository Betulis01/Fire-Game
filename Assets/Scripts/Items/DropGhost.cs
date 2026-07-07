using UnityEngine;

// A transparent preview of an item about to be dropped. PlayerInteractor spawns
// one while the drop key is held, moves it to the aimed position each frame, and
// dismisses it on release (after resolving into a real drop or a fuel feed).
// Purely visual: all behaviours and colliders on the clone are disabled, mirroring
// PlacementGhost, but this one is driven externally rather than reading input.
public class DropGhost : MonoBehaviour
{
    SpriteRenderer[] renderers;

    public static DropGhost Begin(GameObject itemPrefab)
    {
        GameObject go = Instantiate(itemPrefab);
        DropGhost ghost = go.AddComponent<DropGhost>();
        ghost.Init();
        return ghost;
    }

    void Init()
    {
        // strip everything that would make this act like a real world item
        foreach (Behaviour b in GetComponentsInChildren<Behaviour>())
            if (b != null && b != this) b.enabled = false;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            if (c != null) c.enabled = false;

        renderers = GetComponentsInChildren<SpriteRenderer>();
        SetOpacity(0.5f);
    }

    public void MoveTo(Vector3 worldPos) => transform.position = worldPos;

    public void Dismiss() => Destroy(gameObject);

    void SetOpacity(float alpha)
    {
        foreach (SpriteRenderer r in renderers)
        {
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }
}

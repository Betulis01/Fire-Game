using System.Collections.Generic;
using UnityEngine;

// Marks an entity as one that shows a health nameplate on the HUD (NameplatesUI draws
// it on the screen canvas). Only holds where the plate sits -- `offset` is in world
// units from the root, so the gap above the head follows the camera's zoom.
// Nameplates register themselves while enabled, so the HUD never scans the scene and
// a spawned enemy gets its plate from the frame it's enabled.
public class Nameplate : MonoBehaviour
{
    public static readonly List<Nameplate> All = new();

    [Tooltip("Where the plate's bottom centre sits, in world units from this object.")]
    [SerializeField] Vector2 offset = new(0f, 1f);

    public Health Health { get; private set; }
    public Vector3 Anchor => transform.position + (Vector3)offset;

    void Awake()
    {
        Health = GetComponent<Health>();
        if (Health == null) Health = GetComponentInParent<Health>();
    }

    void OnEnable() => All.Add(this);

    void OnDisable()
    {
        int i = All.IndexOf(this);
        if (i < 0) return;
        int last = All.Count - 1;
        All[i] = All[last];
        All.RemoveAt(last);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.71f, 0.35f, 0.27f);
        Vector3 a = Anchor;
        Gizmos.DrawLine(a + Vector3.left * 0.25f, a + Vector3.right * 0.25f);
    }
}

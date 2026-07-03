using UnityEngine;

// A transparent preview of a Placeable recipe's output. CraftingController spawns
// one when the player picks a Placeable recipe in the Journal; it follows the
// player's aim until placed (Attack) or cancelled (Journal key again). Once placed
// it adds a BuildRequirement that waits for materials dropped on top of it and
// calls back into Complete() to turn the ghost into the real, opaque structure.
public class PlacementGhost : MonoBehaviour
{
    public static bool AnyActive { get; private set; }

    Recipe recipe;
    Transform follow;
    Camera cam;
    float distance;
    InteractPrompt buildPromptPrefab;
    bool placed;

    SpriteRenderer[] renderers;
    Collider2D[] colliders;
    Behaviour[] behaviours;   // every other script on the structure, off until built

    public static PlacementGhost Begin(Recipe recipe, Transform follow, Camera cam, float distance, InteractPrompt buildPromptPrefab)
    {
        GameObject go = Instantiate(recipe.output.prefab);
        PlacementGhost ghost = go.AddComponent<PlacementGhost>();
        ghost.Init(recipe, follow, cam, distance, buildPromptPrefab);
        return ghost;
    }

    void Init(Recipe recipe, Transform follow, Camera cam, float distance, InteractPrompt buildPromptPrefab)
    {
        this.recipe = recipe;
        this.follow = follow;
        this.cam = cam;
        this.distance = distance;
        this.buildPromptPrefab = buildPromptPrefab;
        AnyActive = true;

        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider2D>();
        behaviours = GetComponentsInChildren<Behaviour>();

        foreach (Behaviour b in behaviours)
            if (b != null && b != this) b.enabled = false;
        foreach (Collider2D c in colliders)
            if (c != null) c.enabled = false;

        SetOpacity(0.45f);
    }

    void Update()
    {
        if (placed) return;

        Vector2 aim = UserInput.Instance.AimDirection(follow.position, cam);
        transform.position = follow.position + (Vector3)(aim * distance);

        if (UserInput.Instance.Journal) { Cancel(); return; }
        if (UserInput.Instance.Attack) Place();
    }

    void Place()
    {
        placed = true;
        AnyActive = false;   // ghost is no longer following; journal can reopen now
        BuildRequirement req = gameObject.AddComponent<BuildRequirement>();
        req.Init(recipe, this, buildPromptPrefab);
    }

    // called by BuildRequirement once every ingredient has been deposited
    public void Complete()
    {
        AnyActive = false;

        foreach (Behaviour b in behaviours)
            if (b != null && b != this) b.enabled = true;
        foreach (Collider2D c in colliders)
            if (c != null) c.enabled = true;

        SetOpacity(1f);

        WorldItem worldItem = GetComponent<WorldItem>();
        if (worldItem != null) worldItem.SetHeld(false);

        Destroy(this);
    }

    void Cancel()
    {
        AnyActive = false;
        Destroy(gameObject);
    }

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

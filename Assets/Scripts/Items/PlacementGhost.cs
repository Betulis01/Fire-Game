using UnityEngine;

// A transparent preview of a Placeable recipe's output. CraftingController spawns
// one when the player picks a Placeable recipe on the HUD menu's Crafting tab; it
// follows the player's aim until placed (left click) or cancelled (Esc or the menu
// key, via CancelActive -- GameStateManager / HudMenu own those keys). Once placed
// it adds a BuildRequirement that waits for materials dropped on top of it and
// calls back into Complete() to turn the ghost into the real, opaque structure.
public class PlacementGhost : MonoBehaviour
{
    public static bool AnyActive { get; private set; }

    static PlacementGhost active;   // the ghost still following the cursor, if any

    Recipe recipe;
    Transform follow;
    Camera cam;
    float distance;   // max range from the player the ghost can be placed at
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
        active = this;

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

        transform.position = UserInput.Instance.AimPoint(follow.position, cam, distance);

        // claim the click so the left hand doesn't also swing at the build spot
        if (UserInput.Instance.UseLeft)
        {
            UserInput.Instance.ConsumeUse(HandSide.Left);
            Place();
        }
    }

    // Cancel the ghost that's still following the cursor. Returns whether there was
    // one, so a key that cancels (Esc, the menu key) can skip its normal action.
    public static bool CancelActive()
    {
        if (active == null || active.placed) return false;
        active.Cancel();
        return true;
    }

    void Place()
    {
        placed = true;
        AnyActive = false;   // ghost is no longer following
        if (active == this) active = null;
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
        if (active == this) active = null;
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

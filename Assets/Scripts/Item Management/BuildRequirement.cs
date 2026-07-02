using UnityEngine;

// Added to a structure once its PlacementGhost has been placed. Tracks how much of
// each recipe ingredient has been dropped on top of it (FuelReceiver is the model
// this follows: a trigger zone that consumes matching dropped WorldItems) and shows
// a "Drop X (n/total)" prompt while incomplete. Calls back into the ghost once
// every ingredient is satisfied, then removes itself.
[RequireComponent(typeof(Rigidbody2D))]
public class BuildRequirement : MonoBehaviour
{
    Recipe recipe;
    PlacementGhost ghost;
    InteractPrompt prompt;
    int[] deposited;   // parallel to recipe.ingredients

    public void Init(Recipe recipe, PlacementGhost ghost, InteractPrompt promptPrefab)
    {
        this.recipe = recipe;
        this.ghost = ghost;
        deposited = new int[recipe.ingredients.Length];

        CircleCollider2D trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.75f;

        if (promptPrefab != null)
        {
            prompt = Instantiate(promptPrefab);
            UpdatePrompt();
        }
    }

    // Enter catches items dropped into the trigger normally; Stay catches the edge
    // case where an item re-enables its collider while already overlapping (same
    // pattern as FuelReceiver). The worldItem.enabled guard prevents double-counting
    // when both fire in the same physics step.
    void OnTriggerEnter2D(Collider2D other) => HandleOverlap(other);
    void OnTriggerStay2D(Collider2D other) => HandleOverlap(other);

    void HandleOverlap(Collider2D other)
    {
        WorldItem worldItem = other.GetComponentInParent<WorldItem>();
        if (worldItem == null || !worldItem.enabled || worldItem.IsHeld) return;

        int index = NextNeededIndex(worldItem.item);
        if (index < 0) return;

        worldItem.enabled = false;   // ignore the rest of this item's colliders this step
        deposited[index]++;
        Destroy(worldItem.gameObject);

        UpdatePrompt();
        if (IsComplete()) Finish();
    }

    int NextNeededIndex(ItemDefinition item)
    {
        for (int i = 0; i < recipe.ingredients.Length; i++)
            if (recipe.ingredients[i].item == item && deposited[i] < recipe.ingredients[i].amount)
                return i;
        return -1;
    }

    bool IsComplete()
    {
        for (int i = 0; i < recipe.ingredients.Length; i++)
            if (deposited[i] < recipe.ingredients[i].amount) return false;
        return true;
    }

    void UpdatePrompt()
    {
        if (prompt == null) return;

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            if (deposited[i] < recipe.ingredients[i].amount)
            {
                Ingredient ingredient = recipe.ingredients[i];
                prompt.Show(transform.position + Vector3.up * 0.5f,
                    $"Drop {ingredient.item.displayName} ({deposited[i]}/{ingredient.amount})");
                return;
            }
        }
    }

    void Finish()
    {
        if (prompt != null) Destroy(prompt.gameObject);
        prompt = null;
        ghost.Complete();
        Destroy(this);
    }

    void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }
}

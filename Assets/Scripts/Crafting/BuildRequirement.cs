using System.Collections.Generic;
using UnityEngine;

// Added to a structure once its PlacementGhost has been placed. Tracks how much of
// each recipe ingredient has been supplied and, as an ItemReceiver, lets the player
// deposit needed items by standing in its zone and pressing the drop key (or
// releasing the drop ghost over it) — the same intentional model as FuelReceiver.
// Calls back into the ghost once every ingredient is satisfied, then removes itself.
public class BuildRequirement : ItemReceiver
{
    Recipe recipe;
    PlacementGhost ghost;
    int[] deposited;   // parallel to recipe.ingredients

    public void Init(Recipe recipe, PlacementGhost ghost, InteractPrompt promptPrefab)
    {
        this.recipe = recipe;
        this.ghost = ghost;
        this.promptPrefab = promptPrefab;
        deposited = new int[recipe.ingredients.Length];

        CircleCollider2D trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.75f;
        RefreshZones();   // pick up the deposit-zone collider we just added
    }

    // A placed build site always shows its outstanding ingredients when the player
    // is nearby, even empty-handed, so they know what to bring.
    protected override bool AdvertisesWhenEmpty => true;

    public override bool Accepts(WorldItem item) =>
        item != null && NextNeededIndex(item.item) >= 0;

    public override bool Deposit(WorldItem item)
    {
        if (item == null) return false;
        int index = NextNeededIndex(item.item);
        if (index < 0) return false;

        deposited[index]++;
        if (IsComplete()) Finish();
        return true;
    }

    // With an accepted item in hand, offer to drop it; otherwise list what's left.
    protected override string PromptLabel(WorldItem held)
    {
        int index = held != null ? NextNeededIndex(held.item) : -1;
        if (index >= 0)
        {
            Ingredient ingredient = recipe.ingredients[index];
            return $"Drop {ingredient.item.displayName} ({deposited[index]}/{ingredient.amount})";
        }
        return RemainingSummary();
    }

    string RemainingSummary()
    {
        List<string> parts = new();
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            Ingredient ingredient = recipe.ingredients[i];
            if (deposited[i] < ingredient.amount)
                parts.Add($"{ingredient.item.displayName} {deposited[i]}/{ingredient.amount}");
        }
        return "Needs " + string.Join(", ", parts);
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

    void Finish()
    {
        ghost.Complete();
        Destroy(this);   // base OnDestroy tears down the prompt
    }
}

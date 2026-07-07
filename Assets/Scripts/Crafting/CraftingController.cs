using System.Collections.Generic;
using UnityEngine;

// Player-side crafting driven by the Survival Journal (JournalUI). The Journal
// only needs to know what's craftable (HasIngredients/StationSatisfied); clicking a
// recipe calls TryCraft here, which either hands the player an Instant output or
// spawns a PlacementGhost for a Placeable one. Recipes live in a central RecipeBook.
[RequireComponent(typeof(Hands))]
public class CraftingController : MonoBehaviour
{
    public RecipeBook book;
    public float placeDistance = 1.5f;   // max range from the player a placement ghost can be placed
    public Camera cam;

    [Tooltip("World-space prompt used by a placed structure to ask for materials.")]
    public InteractPrompt buildPromptPrefab;

    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
    }

    public StationType NearbyStationTypes()
    {
        StationType types = StationType.None;
        foreach (Station station in FindObjectsByType<Station>(FindObjectsInactive.Exclude))
        {
            float distance = Vector2.Distance(transform.position, station.transform.position);
            if (distance <= station.reach) types |= station.type;
        }
        return types;
    }

    public bool StationSatisfied(Recipe recipe, StationType stations) =>
        recipe.requiredStation == StationType.None || (stations & recipe.requiredStation) != StationType.None;

    // Whether the player currently holds enough of every ingredient (Instant
    // recipes draw straight from hands; Placeable ones are fed after placement so
    // this always reads true for them).
    public bool HasIngredients(Recipe recipe) =>
        recipe.kind == RecipeKind.Placeable || TryMatchIngredients(recipe, out _);

    // Attempt to craft/begin-placing a recipe; false if a requirement isn't met.
    public bool TryCraft(Recipe recipe)
    {
        if (recipe == null || recipe.output == null || recipe.output.prefab == null) return false;
        if (!StationSatisfied(recipe, NearbyStationTypes())) return false;
        if (PlacementGhost.AnyActive) return false;   // one placement at a time

        if (recipe.kind == RecipeKind.Placeable)
        {
            PlacementGhost.Begin(recipe, transform, cam, placeDistance, buildPromptPrefab);
            return true;
        }

        if (!TryMatchIngredients(recipe, out List<(HandSide side, int amount)> draws)) return false;

        HandSide? outputHand = ChooseOutputHand(draws);
        if (outputHand == null) return false;

        foreach ((HandSide side, int amount) in draws)
            hands.Consume(side, amount);

        hands.TryHold(Instantiate(recipe.output.prefab), outputHand.Value);
        return true;
    }

    // Group the recipe's ingredients by item and assign each distinct item to a
    // distinct hand holding at least its amount. `draws` lists the matched hand and
    // the amount to consume per item. Since there are only two hands, a recipe can
    // use at most two distinct item types.
    bool TryMatchIngredients(Recipe recipe, out List<(HandSide side, int amount)> draws)
    {
        draws = new List<(HandSide, int)>();
        if (recipe.ingredients == null || recipe.ingredients.Length == 0) return false;

        List<HandSide> available = new() { HandSide.Left, HandSide.Right };

        foreach (Ingredient ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) return false;
            int remaining = ingredient.amount;
            List<HandSide> matched = new();

            foreach (HandSide side in available)
            {
                if (ItemIn(side) != ingredient.item) continue;

                int take = Mathf.Min(remaining, hands.Count(side));
                if (take <= 0) continue;

                matched.Add(side);
                draws.Add((side, take));
                remaining -= take;
                if (remaining == 0) break;
            }

            if (remaining > 0) return false;   // not enough of an ingredient across hands
            foreach (HandSide side in matched) available.Remove(side);
        }

        return true;
    }

    // A hand that will be empty for the output: prefer an input hand whose stack
    // empties after consuming its amount, else a hand not used as input that's
    // already empty. Null means there's nowhere to put the result.
    HandSide? ChooseOutputHand(List<(HandSide side, int amount)> draws)
    {
        foreach ((HandSide side, int amount) in draws)
            if (hands.Count(side) <= amount) return side;

        List<HandSide> drawHands = draws.ConvertAll(d => d.side);
        foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
            if (!drawHands.Contains(side) && !hands.IsHolding(side)) return side;

        return null;
    }

    ItemDefinition ItemIn(HandSide side)
    {
        GameObject held = hands.Held(side);
        if (held == null) return null;

        WorldItem worldItem = held.GetComponent<WorldItem>();
        return worldItem != null ? worldItem.item : null;
    }
}

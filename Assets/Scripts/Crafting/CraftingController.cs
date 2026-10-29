using System.Collections.Generic;
using UnityEngine;

// Player-side crafting driven by the HUD menu's Crafting tab (CraftingTabUI). The
// tab only needs to know what's craftable (HasIngredients/StationSatisfied); clicking
// a blueprint calls TryCraft here, which either hands the player an Instant output or
// spawns a PlacementGhost for a Placeable one. Instant recipes take their ingredients
// from the hands and the worn bag (Inventory) together, and hand the output to the
// player the way an E press would (Inventory.TryTake: a Big item into a free hand,
// the rest -- gear included -- into the bag); a recipe whose output
// would have nowhere to go isn't crafted. Recipes live in a central RecipeBook.
[RequireComponent(typeof(Hands))]
public class CraftingController : MonoBehaviour
{
    public RecipeBook book;
    public float placeDistance = 1.5f;   // max range from the player a placement ghost can be placed
    public Camera cam;

    [Tooltip("World-space prompt used by a placed structure to ask for materials.")]
    public InteractPrompt buildPromptPrefab;

    Hands hands;
    Inventory inventory;   // the bag: ingredients too, and the router for the output

    void Awake()
    {
        hands = GetComponent<Hands>();
        inventory = GetComponent<Inventory>();
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

    // Whether the player carries enough of every ingredient, in the hands and the
    // bag together (Instant recipes draw from both; Placeable ones are fed after
    // placement so this always reads true for them).
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

        if (!TryMatchIngredients(recipe, out List<Draw> draws)) return false;

        if (!HasRoomForOutput(recipe.output, draws)) return false;

        foreach (Draw d in draws)
        {
            if (d.fromBag) inventory.Consume(d.slot, d.amount);
            else hands.Consume(d.side, d.amount);
        }

        GameObject output = Instantiate(recipe.output.prefab, transform.position, Quaternion.identity);
        // checked above, so this only misses if something changed in between; then
        // the output simply lies at the player's feet
        if (inventory != null) inventory.TryTake(output.GetComponent<WorldItem>());
        return true;
    }

    // Where an ingredient is drawn from: a hand, or a bag slot.
    struct Draw
    {
        public bool fromBag;
        public HandSide side;
        public int slot;
        public int amount;
    }

    // Find every ingredient in the hands first, then the bag, splitting an amount
    // across as many stacks as it takes. What's left of each stack is tracked, so two
    // ingredients of the same item never count one stack twice.
    bool TryMatchIngredients(Recipe recipe, out List<Draw> draws)
    {
        draws = new List<Draw>();
        if (recipe.ingredients == null || recipe.ingredients.Length == 0) return false;

        int[] handLeft = { hands.IsHolding(HandSide.Left) ? 1 : 0, hands.IsHolding(HandSide.Right) ? 1 : 0 };
        int capacity = inventory != null ? inventory.Capacity : 0;
        int[] slotLeft = new int[capacity];
        for (int i = 0; i < capacity; i++) slotLeft[i] = inventory.Count(i);

        foreach (Ingredient ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) return false;
            int remaining = ingredient.amount;

            foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
            {
                if (remaining == 0) break;
                if (!ingredient.item.Matches(ItemIn(hands.Held(side)))) continue;
                int take = Mathf.Min(remaining, handLeft[(int)side]);
                if (take <= 0) continue;
                handLeft[(int)side] -= take;
                remaining -= take;
                draws.Add(new Draw { side = side, amount = take });
            }

            for (int i = 0; i < capacity && remaining > 0; i++)
            {
                if (!ingredient.item.Matches(ItemIn(inventory.Item(i)))) continue;
                int take = Mathf.Min(remaining, slotLeft[i]);
                if (take <= 0) continue;
                slotLeft[i] -= take;
                remaining -= take;
                draws.Add(new Draw { fromBag = true, slot = i, amount = take });
            }

            if (remaining > 0) return false;   // not enough of it in hands and bag together
        }

        return true;
    }

    // Whether the output will have somewhere to go once the ingredients are used,
    // by the same routing as Inventory.TryTake: a Big item needs a hand that's free
    // (or that the recipe frees), anything else room in the bag.
    bool HasRoomForOutput(ItemDefinition output, List<Draw> draws)
    {
        if (inventory == null) return false;

        if (output.IsBig)
        {
            foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
                if (!hands.IsHolding(side) || draws.Exists(d => !d.fromBag && d.side == side)) return true;
            return false;
        }

        int[] freed = new int[inventory.Capacity];
        foreach (Draw d in draws)
            if (d.fromBag) freed[d.slot] += d.amount;
        return inventory.HasRoomFor(output, freed);
    }

    static ItemDefinition ItemIn(GameObject item)
    {
        if (item == null) return null;
        WorldItem worldItem = item.GetComponent<WorldItem>();
        return worldItem != null ? worldItem.item : null;
    }
}

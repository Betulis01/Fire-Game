using System.Collections.Generic;
using UnityEngine;

// Player-side crafting driven by the HUD menu's Crafting tab (CraftingTabUI). The
// tab only needs to know what's craftable (HasIngredients/StationSatisfied); clicking
// a blueprint calls TryCraft here, which either hands the player an Instant output or
// spawns a PlacementGhost for a Placeable one. Instant recipes take their ingredients
// from the hands and the worn bag (Inventory) together, and put the output in a free
// hand, else in the bag. Recipes live in a central RecipeBook.
[RequireComponent(typeof(Hands))]
public class CraftingController : MonoBehaviour
{
    public RecipeBook book;
    public float placeDistance = 1.5f;   // max range from the player a placement ghost can be placed
    public Camera cam;

    [Tooltip("World-space prompt used by a placed structure to ask for materials.")]
    public InteractPrompt buildPromptPrefab;

    Hands hands;
    Inventory inventory;           // the bag: ingredients too, and the output's fallback
    PlayerInteractor interactor;   // selected hand: where crafted output prefers to go

    void Awake()
    {
        hands = GetComponent<Hands>();
        inventory = GetComponent<Inventory>();
        interactor = GetComponent<PlayerInteractor>();
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

        HandSide? outputHand = ChooseOutputHand(draws);
        bool intoBag = outputHand == null && BagHasRoom(draws, recipe.output);
        if (outputHand == null && !intoBag) return false;

        foreach (Draw d in draws)
        {
            if (d.fromBag) inventory.Consume(d.slot, d.amount);
            else hands.Consume(d.side, d.amount);
        }

        GameObject output = Instantiate(recipe.output.prefab);
        if (intoBag) inventory.TryAdd(output.GetComponent<WorldItem>());
        else hands.TryHold(output, outputHand.Value);
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

        int[] handLeft = { hands.Count(HandSide.Left), hands.Count(HandSide.Right) };
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
                if (ItemIn(hands.Held(side)) != ingredient.item) continue;
                int take = Mathf.Min(remaining, handLeft[(int)side]);
                if (take <= 0) continue;
                handLeft[(int)side] -= take;
                remaining -= take;
                draws.Add(new Draw { side = side, amount = take });
            }

            for (int i = 0; i < capacity && remaining > 0; i++)
            {
                if (ItemIn(inventory.Item(i)) != ingredient.item) continue;
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

    // A hand that will be empty for the output: the selected hand if it will be
    // free, else the other one. Null means both stay full (the output then goes
    // into the bag, if it has room).
    HandSide? ChooseOutputHand(List<Draw> draws)
    {
        HandSide active = interactor != null ? interactor.ActiveHand : HandSide.Left;
        foreach (HandSide side in new[] { active, active == HandSide.Left ? HandSide.Right : HandSide.Left })
            if (WillBeFree(side, draws)) return side;
        return null;
    }

    bool WillBeFree(HandSide side, List<Draw> draws)
    {
        int drawn = 0;
        foreach (Draw d in draws)
            if (!d.fromBag && d.side == side) drawn += d.amount;
        return !hands.IsHolding(side) || hands.Count(side) <= drawn;
    }

    // Room in the bag for one output: an empty slot (or one the recipe empties), or a
    // matching stack that isn't full.
    bool BagHasRoom(List<Draw> draws, ItemDefinition output)
    {
        if (inventory == null) return false;
        int maxStack = output.prefab.TryGetComponent(out Stackable s) ? s.maxStack : 1;

        for (int i = 0; i < inventory.Capacity; i++)
        {
            int count = inventory.Count(i);
            foreach (Draw d in draws)
                if (d.fromBag && d.slot == i) count -= d.amount;

            if (count <= 0) return true;
            if (ItemIn(inventory.Item(i)) == output && count < maxStack) return true;
        }
        return false;
    }

    static ItemDefinition ItemIn(GameObject item)
    {
        if (item == null) return null;
        WorldItem worldItem = item.GetComponent<WorldItem>();
        return worldItem != null ? worldItem.item : null;
    }
}

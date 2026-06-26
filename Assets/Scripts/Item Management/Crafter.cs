using System.Collections.Generic;
using UnityEngine;

// Player-side crafting: press F to turn the items in your hands into a recipe's
// output. A recipe matches when its inputs are held (one per hand) and its
// required station (if any) is within reach. Recipes live in a central RecipeBook.
[RequireComponent(typeof(Hands))]
public class Crafter : MonoBehaviour
{
    public RecipeBook book;        // all known recipes
    public float dropDistance = 1f; // how far in front a placed output spawns
    public Camera cam;

    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
    }

    void Update()
    {
        if (UserInput.Instance.Craft)
            TryCraft();
    }

    void TryCraft()
    {
        if (book == null) return;

        HashSet<StationType> stations = NearbyStationTypes();

        foreach (Recipe recipe in book.recipes)
        {
            if (recipe == null || recipe.output == null || recipe.output.prefab == null)
                continue;

            if (recipe.requiredStation != StationType.None && !stations.Contains(recipe.requiredStation))
                continue;

            if (TryMatchInputs(recipe, out List<(HandSide side, int amount)> draws) && Craft(recipe, draws))
                return;   // one craft per press
        }
    }

    // Group the recipe's inputs by item (repeats mean quantity, e.g. two "stick"
    // entries require 2 sticks) and assign each distinct item to a distinct hand
    // holding at least that many. `draws` lists the matched hand and the amount to
    // consume per item. Since there are only two hands, a recipe can use at most
    // two distinct item types.
    bool TryMatchInputs(Recipe recipe, out List<(HandSide side, int amount)> draws)
    {
        draws = new List<(HandSide, int)>();

        if (recipe.inputs == null || recipe.inputs.Length == 0)
            return false;

        Dictionary<ItemDefinition, int> needed = new();
        foreach (ItemDefinition input in recipe.inputs)
        {
            if (input == null) return false;
            needed[input] = needed.TryGetValue(input, out int n) ? n + 1 : 1;
        }

        List<HandSide> available = new() { HandSide.Left, HandSide.Right };

        foreach (KeyValuePair<ItemDefinition, int> req in needed)
        {
            int remaining = req.Value;
            List<HandSide> matched = new();

            foreach (HandSide side in available)
            {
                if (ItemIn(side) != req.Key) continue;

                int take = Mathf.Min(remaining, hands.Count(side));
                if (take <= 0) continue;

                matched.Add(side);
                draws.Add((side, take));
                remaining -= take;
                if (remaining == 0) break;
            }

            if (remaining > 0) return false;   // not enough of an input across hands

            foreach (HandSide side in matched) available.Remove(side);
        }

        return true;
    }

    // Consume each input's amount and produce the output. An output that can be
    // picked up (has a Pickupable) goes into a free hand; one that can't (a placed
    // structure like a campfire) is dropped in the world in front of the player and
    // needs no free hand. A hand output is refused if no hand is free (every input
    // hand keeps a remainder and the other hand is full). Returns true if crafted.
    bool Craft(Recipe recipe, List<(HandSide side, int amount)> draws)
    {
        bool placeInWorld = recipe.output.prefab.GetComponent<Pickupable>() == null;

        HandSide? outputHand = null;
        if (!placeInWorld)
        {
            outputHand = ChooseOutputHand(draws);
            if (outputHand == null) return false;
        }

        foreach ((HandSide side, int amount) in draws)
            hands.Consume(side, amount);

        if (placeInWorld)
            PlaceInWorld(recipe.output.prefab);
        else
            hands.TryHold(Instantiate(recipe.output.prefab), outputHand.Value);

        return true;
    }

    // Spawn a structure output on the ground in the direction of the mouse cursor,
    // with its pickup collider active so it behaves as a placed world object.
    void PlaceInWorld(GameObject prefab)
    {
        // aim toward the mouse cursor (or gamepad aim stick)
        Vector2 facing = UserInput.Instance.AimDirection(transform.position, cam);

        Vector3 where = transform.position + (Vector3)(facing * dropDistance);

        GameObject placed = Instantiate(prefab, where, Quaternion.identity);
        WorldItem worldItem = placed.GetComponent<WorldItem>();
        if (worldItem != null) worldItem.SetHeld(false);
    }

    // A hand that will be empty for the output: prefer an input hand whose stack
    // empties after consuming its amount, else a hand not used as input that's
    // already empty. Null means there's nowhere to put the result.
    HandSide? ChooseOutputHand(List<(HandSide side, int amount)> draws)
    {
        foreach ((HandSide side, int amount) in draws)
            if (hands.Count(side) <= amount) return side;   // empties after consuming

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

    HashSet<StationType> NearbyStationTypes()
    {
        HashSet<StationType> types = new();
        foreach (Station station in FindObjectsByType<Station>(FindObjectsInactive.Exclude))
        {
            float distance = Vector2.Distance(transform.position, station.transform.position);
            if (distance <= station.reach) types.Add(station.type);
        }
        return types;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Player-side crafting: press F to turn the items in your hands into a recipe's
// output. A recipe matches when its inputs are held (one per hand) and its
// required station (if any) is within reach. Recipes live in a central RecipeBook.
[RequireComponent(typeof(Hands))]
public class Crafter : MonoBehaviour
{
    public RecipeBook book;       // all known recipes
    public float reach = 1.5f;    // how close a required station must be

    Hands hands;

    void Awake()
    {
        hands = GetComponent<Hands>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
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

            if (TryMatchInputs(recipe, out List<HandSide> inputHands) && Craft(recipe, inputHands))
                return;   // one craft per press
        }
    }

    // Match each recipe input to a distinct hand holding that item; inputHands
    // lists the matched hand per input (in recipe order).
    bool TryMatchInputs(Recipe recipe, out List<HandSide> inputHands)
    {
        inputHands = new List<HandSide>();

        if (recipe.inputs == null || recipe.inputs.Length == 0)
            return false;

        List<HandSide> available = new() { HandSide.Left, HandSide.Right };

        foreach (ItemDefinition input in recipe.inputs)
        {
            HandSide? match = null;
            foreach (HandSide side in available)
            {
                if (ItemIn(side) == input)
                {
                    match = side;
                    break;
                }
            }

            if (match == null) return false;   // an input isn't held

            available.Remove(match.Value);
            inputHands.Add(match.Value);
        }

        return true;
    }

    // Consume one of each input and place the output in a free hand. Each input
    // hand loses one from its stack; a stacked input keeps its remainder. If no
    // hand is free for the result (a stack remains and the other hand is full),
    // the craft is refused. Returns true if it crafted.
    bool Craft(Recipe recipe, List<HandSide> inputHands)
    {
        HandSide? outputHand = ChooseOutputHand(inputHands);
        if (outputHand == null) return false;

        foreach (HandSide side in inputHands)
            hands.Consume(side, 1);

        hands.TryHold(Instantiate(recipe.output.prefab), outputHand.Value);
        return true;
    }

    // A hand that will be empty for the output: prefer an input hand whose stack
    // empties after consuming one, else a hand not used as input that's already
    // empty. Null means there's nowhere to put the result.
    HandSide? ChooseOutputHand(List<HandSide> inputHands)
    {
        foreach (HandSide side in inputHands)
            if (hands.Count(side) <= 1) return side;   // empties after consuming one

        foreach (HandSide side in new[] { HandSide.Left, HandSide.Right })
            if (!inputHands.Contains(side) && !hands.IsHolding(side)) return side;

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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, reach);
        foreach (Collider2D hit in hits)
        {
            Station station = hit.GetComponentInParent<Station>();
            if (station != null) types.Add(station.type);
        }
        return types;
    }
}

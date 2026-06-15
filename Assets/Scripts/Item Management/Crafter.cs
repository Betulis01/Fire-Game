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

            if (TryAssignInputs(recipe, out HandSide outputHand, out List<HandSide> consumed))
            {
                Craft(recipe, outputHand, consumed);
                return;   // one craft per press
            }
        }
    }

    // Match each recipe input to a distinct hand holding that item. On success,
    // outputHand is where the result goes (first matched input's hand) and
    // consumed lists the other hands to empty.
    bool TryAssignInputs(Recipe recipe, out HandSide outputHand, out List<HandSide> consumed)
    {
        outputHand = HandSide.Left;
        consumed = new List<HandSide>();

        if (recipe.inputs == null || recipe.inputs.Length == 0)
            return false;

        List<HandSide> available = new() { HandSide.Left, HandSide.Right };
        List<HandSide> used = new();

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
            used.Add(match.Value);
        }

        outputHand = used[0];
        for (int i = 1; i < used.Count; i++)
            consumed.Add(used[i]);
        return true;
    }

    void Craft(Recipe recipe, HandSide outputHand, List<HandSide> consumed)
    {
        foreach (HandSide side in consumed)
            hands.Clear(side);

        hands.Replace(outputHand, Instantiate(recipe.output.prefab));
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

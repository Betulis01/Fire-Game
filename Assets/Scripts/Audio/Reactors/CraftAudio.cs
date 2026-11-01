using UnityEngine;

// Crafting, heard (sound.md §10.5): a crafted recipe plays its own sound (Recipe.craftSound
// by category: twigs binding, stone lashed to a haft, weaving, hammered iron, sap), else
// the UI's place; a craft a requirement refused is the UI's denied. A Placeable recipe
// starts a placement ghost instead, which is BuildAudio's. On the player, beside
// CraftingController.
[RequireComponent(typeof(CraftingController))]
public class CraftAudio : MonoBehaviour
{
    CraftingController crafting;

    void Awake() => crafting = GetComponent<CraftingController>();

    void OnEnable()
    {
        crafting.Crafted += OnCrafted;
        crafting.CraftFailed += OnFailed;
    }

    void OnDisable()
    {
        crafting.Crafted -= OnCrafted;
        crafting.CraftFailed -= OnFailed;
    }

    void OnCrafted(Recipe recipe)
    {
        if (recipe.craftSound != null) Sfx.Play2D(recipe.craftSound);
        else UiSfx.Play(UiSound.Place);
    }

    void OnFailed(Recipe recipe) => UiSfx.Play(UiSound.Denied);
}

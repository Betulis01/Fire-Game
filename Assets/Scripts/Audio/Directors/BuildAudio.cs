using System.Collections.Generic;
using UnityEngine;

// Building, heard (sound.md §8.4), from AudioConfig.world: the blueprint appearing, the
// stake going in where it's placed, a cancel, every ingredient delivered to a build site
// (the item's own landing, pitched up as that site fills), one ingredient complete, and
// the finished structure (its own StructureAudio adds its sound). A director on the
// AudioSystem, since build sites only exist at runtime.
public class BuildAudio : MonoBehaviour
{
    readonly Dictionary<ItemReceiver, int> delivered = new();

    void OnEnable()
    {
        PlacementGhost.Began += OnBegan;
        PlacementGhost.Placed += OnPlaced;
        PlacementGhost.Cancelled += OnCancelled;
        PlacementGhost.Completed += OnCompleted;
        ItemReceiver.AnyDeposited += OnDeposited;
        BuildRequirement.IngredientSatisfied += OnIngredient;
    }

    void OnDisable()
    {
        PlacementGhost.Began -= OnBegan;
        PlacementGhost.Placed -= OnPlaced;
        PlacementGhost.Cancelled -= OnCancelled;
        PlacementGhost.Completed -= OnCompleted;
        ItemReceiver.AnyDeposited -= OnDeposited;
        BuildRequirement.IngredientSatisfied -= OnIngredient;
    }

    static WorldSounds World => AudioSystem.ActiveConfig != null ? AudioSystem.ActiveConfig.world : null;

    void OnBegan(PlacementGhost ghost) { if (World != null) Sfx.Play2D(World.ghostBegin); }
    void OnPlaced(PlacementGhost ghost) { if (World != null) Sfx.Play(World.ghostPlace, ghost.transform.position); }
    void OnCancelled(Vector3 at) { if (World != null) Sfx.Play2D(World.ghostCancel); }

    void OnCompleted(GameObject built)
    {
        if (World != null) Sfx.Play(World.buildComplete, built.transform.position);
        // the site's tally is done with
        delivered.Remove(built.GetComponent<BuildRequirement>());
    }

    void OnDeposited(ItemReceiver receiver, ItemDefinition item)
    {
        if (receiver is not BuildRequirement) return;
        delivered.TryGetValue(receiver, out int n);
        delivered[receiver] = ++n;
        ItemSounds s = ItemAudioDirector.SoundsOf(item);
        Sfx.Play(s != null ? s.land : null, receiver.transform.position, 1f, Mathf.Pow(2f, Mathf.Min(n - 1, 8) / 12f));
    }

    void OnIngredient(BuildRequirement site, ItemDefinition item)
    {
        if (World != null) Sfx.Play(World.ingredientDone, site.transform.position);
    }
}

using UnityEngine;

// Items being handled, heard (sound.md §3.3, §3.4, §3.6, §7), from ItemEvents: each
// item's own ItemSounds (by material, on its ItemDefinition; WorldSounds.fallback when it
// has none), with the shared layers on top from AudioConfig.world:
//   picked up    the item's pickup + "into the pocket" (E), a lighter tick (walked over),
//                or the lift of a Big item into the hands
//   landed       the item's landing + the ground's own landing layer (snow puff, ...)
//   thrown       the throw's whoosh (the landing follows when it comes down)
//   spilled      a bag stack tipped out (each item's landing plays too)
//   worn/removed the gear's equip / unequip; a bag adds its swap
//   eaten        the food's own bite
// A director on the AudioSystem, since ItemEvents are static. Only while playing: the
// player puts on the starting Pouch behind the main menu, and that isn't a sound.
public class ItemAudioDirector : MonoBehaviour
{
    void OnEnable()
    {
        ItemEvents.PickedUp += OnPickedUp;
        ItemEvents.Landed += OnLanded;
        ItemEvents.Thrown += OnThrown;
        ItemEvents.Spilled += OnSpilled;
        ItemEvents.Worn += OnWorn;
        ItemEvents.Removed += OnRemoved;
        ItemEvents.Eaten += OnEaten;
    }

    void OnDisable()
    {
        ItemEvents.PickedUp -= OnPickedUp;
        ItemEvents.Landed -= OnLanded;
        ItemEvents.Thrown -= OnThrown;
        ItemEvents.Spilled -= OnSpilled;
        ItemEvents.Worn -= OnWorn;
        ItemEvents.Removed -= OnRemoved;
        ItemEvents.Eaten -= OnEaten;
    }

    static bool Live => GameStateManager.Instance == null || GameStateManager.Instance.State == GameState.Playing;

    static WorldSounds World => AudioSystem.ActiveConfig != null ? AudioSystem.ActiveConfig.world : null;

    // The item's sounds: its own set, else the world's fallback.
    public static ItemSounds SoundsOf(ItemDefinition item)
    {
        if (item != null && item.sounds != null) return item.sounds;
        WorldSounds w = World;
        return w != null && w.fallback != null ? w.fallback : null;
    }

    static ItemSounds SoundsOf(GameObject item) =>
        SoundsOf(item != null && item.TryGetComponent(out WorldItem wi) ? wi.item : null);

    void OnPickedUp(ItemDefinition item, Vector3 at, bool auto)
    {
        if (!Live) return;
        ItemSounds s = SoundsOf(item);
        WorldSounds w = World;
        Sfx.Play(s != null ? s.pickup : null, at, auto ? 0.7f : 1f);
        if (w == null) return;
        if (auto) Sfx.Play(w.autoPickup, at);
        else Sfx.Play(item != null && item.IsBig ? w.liftHeavy : w.pocket, at);
    }

    void OnLanded(ItemDefinition item, Vector3 at)
    {
        if (!Live) return;
        ItemSounds s = SoundsOf(item);
        Sfx.Play(s != null ? s.land : null, at);

        AudioConfig config = AudioSystem.ActiveConfig;
        if (config != null && config.groundSounds != null)
        {
            GroundType ground = TerrainMap.Instance != null ? TerrainMap.Instance.At(at) : GroundType.Snow;
            Sfx.Play(config.groundSounds.Land(ground), at, item != null && item.IsBig ? 1f : 0.6f);
        }
    }

    void OnThrown(ItemDefinition item, Vector3 at) { if (Live && World != null) Sfx.Play(World.thrown, at); }

    void OnSpilled(ItemDefinition item, Vector3 at, int count)
    {
        if (Live && count > 1 && World != null) Sfx.Play(World.spill, at);
    }

    void OnWorn(GameObject item)
    {
        if (!Live) return;
        ItemSounds s = SoundsOf(item);
        Sfx.Play(s != null ? s.equip : null, item.transform.position);
        if (item.TryGetComponent(out Bag _) && World != null) Sfx.Play(World.bagSwap, item.transform.position);
    }

    void OnRemoved(GameObject item)
    {
        if (!Live) return;
        ItemSounds s = SoundsOf(item);
        Sfx.Play(s != null ? s.unequip : null, item.transform.position);
    }

    void OnEaten(GameObject item)
    {
        if (!Live) return;
        ItemSounds s = SoundsOf(item);
        Sfx.Play(s != null ? s.use : null, item.transform.position);
    }
}

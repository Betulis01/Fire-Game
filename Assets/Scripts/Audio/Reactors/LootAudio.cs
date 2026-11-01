using UnityEngine;

// Loot knocked loose, heard (sound.md §7.2): the pop of it flying out, from
// AudioConfig.world, whenever this entity's DropOnHit or DropOnDeath spawns something
// (a log off a tree, stone off ore, meat off a bear).
public class LootAudio : MonoBehaviour
{
    DropOnHit onHit;
    DropOnDeath onDeath;

    void Awake()
    {
        onHit = GetComponent<DropOnHit>();
        onDeath = GetComponent<DropOnDeath>();
    }

    void OnEnable()
    {
        if (onHit != null) onHit.Dropped += OnDropped;
        if (onDeath != null) onDeath.Dropped += OnDropped;
    }

    void OnDisable()
    {
        if (onHit != null) onHit.Dropped -= OnDropped;
        if (onDeath != null) onDeath.Dropped -= OnDropped;
    }

    void OnDropped(Vector3 at, int count)
    {
        WorldSounds world = AudioSystem.ActiveConfig != null ? AudioSystem.ActiveConfig.world : null;
        if (world != null) Sfx.Play(world.lootPop, at, Mathf.Clamp01(0.7f + 0.1f * count));
    }
}

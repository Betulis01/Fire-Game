using UnityEngine;

// What happens when a given material is struck: the contact VFX and sound. Shared
// as an asset so every entity made of the same material (all trees, all flesh, ...)
// points at one file and retuning it is a single edit. Selected by the victim, not
// the weapon, so a hit always looks/sounds like what was actually struck.
[CreateAssetMenu(menuName = "Fire Game/Hit Effect", fileName = "Hit Effect")]
public class HitEffect : ScriptableObject
{
    [Tooltip("Contact VFX spawned at the hit point when this material is struck.")]
    public GameObject effectPrefab;

    [Tooltip("Sound played when this material is struck. Optional until sfx are authored.")]
    public AudioClip sfx;
}

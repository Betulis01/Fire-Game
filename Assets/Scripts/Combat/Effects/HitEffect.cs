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

    [Tooltip("Turn the VFX to fly along the hit (attacker -> victim), e.g. a blood spray. " +
             "Off for effects drawn upright that shouldn't tilt.")]
    public bool alignToHit;

    [Tooltip("The direction the VFX art travels as drawn, in degrees (0 = right, 45 = up-right).")]
    public float drawnAngle = 45f;

    [Tooltip("Spawn the VFX at the centre of the struck entity's sprite instead of where " +
             "the blow landed (the near edge of its hurtbox).")]
    public bool atSpriteCentre;

    [Tooltip("Sort the VFX with the struck entity's body (needs a SortingGroup on it): in " +
             "front when it flies down-screen, behind when it flies up-screen. Off = the " +
             "prefab's own sorting (drawn over everything on the Effects layer).")]
    public bool sortWithBody;

    [Tooltip("Sound played when this material is struck. Optional until sfx are authored.")]
    public AudioClip sfx;
}

using UnityEngine;

// What a tool or weapon prefab sounds like (its ToolSounds). Data only: the wielder's
// WeaponAudio reads it off whatever the swinging hand holds, so a new weapon gets its
// sounds by adding this and pointing it at a set.
public class ToolAudio : MonoBehaviour
{
    public ToolSounds sounds;

    // A real null when there's none (never Unity's fake null), so callers can use ?.
    public static ToolSounds Of(GameObject item) =>
        item != null && item.TryGetComponent(out ToolAudio a) && a.sounds != null ? a.sounds : null;

    public static ToolSounds Of(Component item) => item != null ? Of(item.gameObject) : null;
}

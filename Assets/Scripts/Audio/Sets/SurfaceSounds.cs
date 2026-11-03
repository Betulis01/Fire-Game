using UnityEngine;

// What a material sounds like when struck (sound.md §13): flesh, bone, wood, stone, ...
// One asset per material, shared by everything made of it; SurfaceAudio on the victim
// picks it, so a hit sounds like what was struck. A material can also voice each tool
// differently (byTool: an axe chops a tree, a pickaxe thunks it, a fist smacks it);
// tools it doesn't list play its impact, or its glance when they can't damage it.
[CreateAssetMenu(menuName = "Audio/Surface Sounds", fileName = "Surface_")]
public class SurfaceSounds : ScriptableObject
{
    [System.Serializable]
    public struct ToolHit
    {
        [Tooltip("The tools this entry voices (one entry can cover several).")]
        public ToolKind tools;
        public SoundEvent sound;
    }

    [Tooltip("A hit that deals damage.")]
    public SoundEvent impact;
    [Tooltip("A hit the striking tool can't damage (ToolDamageFilter): an axe on stone. Empty = only the weapon's own glance plays.")]
    public SoundEvent glance;
    [Tooltip("A hit by these tools plays this instead of impact or glance. An entry whose event has no clips yet " +
             "falls back to them, so nothing goes silent while sounds are missing.")]
    public ToolHit[] byTool = new ToolHit[0];

    // The sound of a hit by this tool kind; `glanced` = the tool can't damage this material.
    public SoundEvent For(ToolKind kind, bool glanced)
    {
        foreach (ToolHit t in byTool)
            if ((t.tools & kind) != 0 && t.sound != null && t.sound.HasClips) return t.sound;
        return glanced ? glance : impact;
    }
}

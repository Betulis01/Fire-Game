using UnityEngine;

// What an entity is made of, visually: the color a landed hit's contact VFX takes
// on (red for flesh, brown for wood, grey for stone). The *animation* comes from
// the weapon (Tool.hitEffectPrefab, carried through HitInfo); this asset only
// tints it. Shared as an asset so every fleshy entity points at the same Flesh
// material and retuning blood is one file. Future per-material reactions (hit
// sounds, decals) belong here too.
[CreateAssetMenu(menuName = "Fire Game/Surface Material", fileName = "Surface Material")]
public class SurfaceMaterial : ScriptableObject
{
    [Tooltip("Multiplied onto every sprite of the weapon's contact effect. Effect art " +
             "should be authored bright/white-ish so tints read well.")]
    public Color tint = Color.white;
}

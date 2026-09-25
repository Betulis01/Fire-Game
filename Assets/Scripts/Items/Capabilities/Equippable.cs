using UnityEngine;

// Capability: this world item can be worn in an equipment slot. While worn, the
// player's PaperDoll draws `frames` -- the item's layer from the player's Aseprite
// file ("<layerName>_Frame_N"), one sprite per body frame -- over the body.
//
// `frames` is filled in the editor by GearSpriteSync (by layer name, after every
// Aseprite import), so it stays correct when the importer reshuffles sprite IDs.
[RequireComponent(typeof(WorldItem))]
public class Equippable : MonoBehaviour
{
    public EquipSlot slot = EquipSlot.Head;

    [Tooltip("Aseprite layer in the player's file this item draws when worn, e.g. Iron_Helmet.")]
    public string layerName;

    [Tooltip("The layer's sprites indexed by body frame. Filled automatically.")]
    public Sprite[] frames;
}

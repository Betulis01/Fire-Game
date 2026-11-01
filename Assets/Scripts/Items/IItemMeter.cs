using UnityEngine;

// Something on an item that the HUD shows as a small bar on the item's icon
// (ItemMeterUI, under bag, loadout, hand and armour boxes): a torch's fuel now;
// durability, enchantment charge and the like later. The box shows the first one
// the item has.
public interface IItemMeter
{
    bool MeterVisible { get; }
    float MeterFill { get; }      // 0..1
    Color MeterColor { get; }
}

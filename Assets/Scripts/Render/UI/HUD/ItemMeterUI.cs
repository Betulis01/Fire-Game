using UnityEngine;
using UnityEngine.UI;

// The small bar along the bottom of an item box (bag slot, loadout box, hand slot,
// armour box): whatever IItemMeter the shown item has -- a torch's fuel now. The box
// hands it the item whenever its content changes (Show); the bar then follows the
// meter every frame, so a burning torch drains while you watch. Hidden for an item
// without a meter, or none.
public class ItemMeterUI : MonoBehaviour
{
    [SerializeField] Image fill;    // Filled, horizontal, white: the meter tints it
    [SerializeField] Image frame;

    IItemMeter meter;

    public void Show(GameObject item)
    {
        meter = item != null ? item.GetComponent<IItemMeter>() : null;
        LateUpdate();
    }

    void LateUpdate()
    {
        bool visible = meter is Object o && o != null && meter.MeterVisible;
        if (fill.enabled != visible) fill.enabled = frame.enabled = visible;
        if (!visible) return;

        fill.fillAmount = meter.MeterFill;
        fill.color = meter.MeterColor;
    }
}

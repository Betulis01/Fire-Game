using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One hand slot in the UI bar: shows the held item's sprite (or nothing) and a
// stack count when more than one is held.
public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public Image selectionBorder;  // border image shown when this hand is active
    public TMP_Text countLabel;   // optional; shown only when count > 1

    // show an item's sprite (and stack count) in this slot
    public void Set(Sprite sprite, int count)
    {
        icon.sprite = sprite;
        icon.enabled = true;

        if (countLabel != null)
        {
            bool show = count > 1;
            countLabel.enabled = show;
            if (show) countLabel.text = count.ToString();
        }
    }

    public void SetSelected(bool on)
    {
        if (selectionBorder != null) selectionBorder.enabled = on;
    }

    // empty-hand look
    public void Clear()
    {
        icon.sprite = null;
        icon.enabled = false;
        if (countLabel != null) countLabel.enabled = false;
    }
}

using UnityEngine;
using UnityEngine.UI;

// One hand slot in the UI bar: shows the held item's sprite (or nothing).
public class InventorySlotUI : MonoBehaviour
{
    public Image icon;

    // show an item's sprite in this slot
    public void Set(Sprite sprite)
    {
        icon.sprite = sprite;
        icon.enabled = true;
    }

    // empty-hand look
    public void Clear()
    {
        icon.sprite = null;
        icon.enabled = false;
    }
}

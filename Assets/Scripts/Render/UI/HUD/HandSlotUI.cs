using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One hand frame on the hand bar: the held item's icon plus its stack count. The
// selected hand (Tab / scroll) uses the taller frame; the rect's pivot sits at the
// bottom, so it grows upward and the mouse hint anchored to its top rides along.
public class HandSlotUI : MonoBehaviour
{
    [SerializeField] Image frame;
    [SerializeField] Sprite normalSprite;      // 96x64
    [SerializeField] Sprite selectedSprite;    // 96x84
    [SerializeField] Image icon;
    [SerializeField] TMP_Text countText;
    [Tooltip("Box the item icon is integer-scaled to fit (the frame's inside).")]
    [SerializeField] Vector2 iconBox = new(56f, 56f);

    public RectTransform Rect => (RectTransform)transform;

    public void SetSelected(bool selected)
    {
        frame.sprite = selected ? selectedSprite : normalSprite;
        Rect.sizeDelta = frame.sprite.rect.size;
    }

    public void Set(Sprite sprite, int count)
    {
        UiPixel.FitIntegerScale(icon, sprite, iconBox);
        countText.text = count > 1 ? count.ToString() : "";
    }
}

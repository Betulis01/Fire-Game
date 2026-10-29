using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The icon that follows the cursor while a bag slot or a loadout item is dragged
// around the HUD (BagDrag). One sits on top of the HUD; drag sources show, move and
// hide it through Instance.
[RequireComponent(typeof(Image))]
public class DragIconUI : MonoBehaviour
{
    public static DragIconUI Instance { get; private set; }

    [SerializeField] Vector2 box = new(56f, 56f);

    Image image;

    void Awake()
    {
        Instance = this;
        image = GetComponent<Image>();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(Sprite sprite, PointerEventData e)
    {
        UiPixel.FitIntegerScale(image, sprite, box);
        transform.SetAsLastSibling();
        Move(e);
    }

    public void Move(PointerEventData e)
    {
        var parent = (RectTransform)transform.parent;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, e.position, e.pressEventCamera, out Vector3 world))
            transform.position = world;
    }

    public void Hide()
    {
        if (image != null) image.enabled = false;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;

// A tab or sub-tab button on the HUD menu. Selected = outlined frame (the world
// shows through), unselected = solid frame; its icon or label is a child that
// stays as is. Disabled tabs (e.g. a weapon with no skill tree yet) dim and
// ignore clicks.
[RequireComponent(typeof(Image), typeof(Button), typeof(CanvasGroup))]
public class TabButtonUI : MonoBehaviour
{
    [SerializeField] Sprite selectedSprite;
    [SerializeField] Sprite unselectedSprite;
    [SerializeField] float disabledAlpha = 0.4f;

    Image image;
    Button button;
    CanvasGroup group;

    public event Action Clicked;

    void Awake()
    {
        Cache();
        button.onClick.AddListener(() => Clicked?.Invoke());
    }

    void Cache()
    {
        if (image != null) return;
        image = GetComponent<Image>();
        button = GetComponent<Button>();
        group = GetComponent<CanvasGroup>();
    }

    public void SetSelected(bool selected)
    {
        Cache();
        image.sprite = selected ? selectedSprite : unselectedSprite;
    }

    public void SetInteractable(bool on)
    {
        Cache();
        button.interactable = on;
        group.alpha = on ? 1f : disabledAlpha;
    }
}

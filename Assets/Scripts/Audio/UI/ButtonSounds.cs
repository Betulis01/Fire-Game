using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A menu button's sounds (sound.md §10.9): a faint tick on hover and the click when it's
// pressed (not when it's disabled). Added to every Button on the main, pause, game-over
// and won panels by Tools/Audio/Set Up Menu Audio.
[RequireComponent(typeof(Button))]
public class ButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => UiSfx.Play(UiSound.Click));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable) UiSfx.Play(UiSound.Hover);
    }
}

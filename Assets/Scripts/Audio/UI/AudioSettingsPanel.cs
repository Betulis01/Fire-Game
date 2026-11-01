using UnityEngine;
using UnityEngine.UI;

// The pause menu's Audio section (SoundSystemPlan §3.7): one slider per channel (Master,
// Music, Ambience, SFX, UI) and the "mute when unfocused" toggle, bound to
// AudioSettingsStore, which saves them and sets the mixer. Built into the pause panel by
// Tools/Audio/Set Up Menu Audio. Dragging a slider ticks quietly (throttled), louder the
// higher it's set, so you hear the level you're choosing.
public class AudioSettingsPanel : MonoBehaviour
{
    [Tooltip("Master, Music, Ambience, SFX, UI -- in AudioChannel order.")]
    [SerializeField] Slider[] sliders = new Slider[0];
    [SerializeField] Toggle muteUnfocused;
    [SerializeField] float tickInterval = 0.08f;

    float nextTick;

    void Awake()
    {
        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] == null) continue;
            AudioChannel channel = (AudioChannel)i;
            sliders[i].onValueChanged.AddListener(v => OnSlider(channel, v));
        }
        if (muteUnfocused != null) muteUnfocused.onValueChanged.AddListener(OnMute);
    }

    // show the saved values each time the pause panel opens
    void OnEnable()
    {
        for (int i = 0; i < sliders.Length; i++)
            if (sliders[i] != null) sliders[i].SetValueWithoutNotify(AudioSettingsStore.Get((AudioChannel)i));
        if (muteUnfocused != null) muteUnfocused.SetIsOnWithoutNotify(AudioSettingsStore.MuteWhenUnfocused);
    }

    void OnSlider(AudioChannel channel, float value)
    {
        AudioSettingsStore.Set(channel, value);
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + tickInterval;
        UiSfx.Play(UiSound.SliderTick, Mathf.Max(0.15f, value));
    }

    void OnMute(bool on)
    {
        AudioSettingsStore.MuteWhenUnfocused = on;
        UiSfx.Play(UiSound.Click);
    }
}

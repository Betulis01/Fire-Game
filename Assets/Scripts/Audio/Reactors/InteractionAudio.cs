using UnityEngine;

// The player's hands-on moments that aren't an item's own sound (sound.md §3.3–3.5): the
// Hold E bar filling on gear (a rising loop, pitching up with the bar), an item fumbled
// for want of room, and Q held long enough to aim a throw. Picking up, dropping and
// throwing themselves are ItemAudioDirector's.
[RequireComponent(typeof(PlayerInteractor))]
public class InteractionAudio : MonoBehaviour
{
    public SoundEvent holdFill;
    public SoundEvent fumble;
    public SoundEvent throwAim;

    PlayerInteractor interactor;
    SoundHandle hold;

    void Awake() => interactor = GetComponent<PlayerInteractor>();

    void OnEnable()
    {
        interactor.Fumbled += OnFumbled;
        interactor.DropGhostShown += OnAim;
    }

    void OnDisable()
    {
        interactor.Fumbled -= OnFumbled;
        interactor.DropGhostShown -= OnAim;
        hold.Stop();
    }

    void Update()
    {
        float t = interactor.HoldProgress;
        if (t <= 0f) { hold.Stop(0.05f); return; }
        if (!hold.IsValid && holdFill != null) hold = Sfx.PlayLoop(holdFill, transform);
        hold.SetPitch(Mathf.Lerp(0.9f, 1.4f, t));
    }

    void OnFumbled(WorldItem item) => Sfx.Play(fumble, transform);
    void OnAim() => Sfx.Play(throwAim, transform);
}

using UnityEngine;

// Whirlwind, heard (sound.md §5): a shout and wind-up as the cast starts, one circular
// whoosh per turn -- pitching up a little each turn, so the spin builds -- and a slowing
// tail when it ends. Hits are the victims' own (SurfaceAudio). On the Whirlwind prefab,
// which sits under the player once spawned, so it plays from the player.
[RequireComponent(typeof(WhirlwindSkill))]
public class WhirlwindAudio : MonoBehaviour
{
    public SoundEvent start;
    public SoundEvent spin;
    [Tooltip("Semitones each turn rises over the first.")]
    [Range(0f, 2f)] public float risePerTurn = 0.5f;
    public SoundEvent end;

    WhirlwindSkill skill;

    void Awake() => skill = GetComponent<WhirlwindSkill>();

    void OnEnable()
    {
        skill.Started += OnStarted;
        skill.Spun += OnSpun;
        skill.Ended += OnEnded;
    }

    void OnDisable()
    {
        skill.Started -= OnStarted;
        skill.Spun -= OnSpun;
        skill.Ended -= OnEnded;
    }

    void OnStarted() => Sfx.Play(start, transform);

    void OnSpun(int turn, StrikeResult result) =>
        Sfx.Play(spin, transform, 1f, Mathf.Pow(2f, (turn - 1) * risePerTurn / 12f));

    void OnEnded() => Sfx.Play(end, transform);
}

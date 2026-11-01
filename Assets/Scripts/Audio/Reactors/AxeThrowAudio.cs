using UnityEngine;

// Axe Throw, heard (sound.md §5): the overhand grunt, the release, a "whup-whup" spin
// that follows the flying axe (faster on the way home), the thunk as it sinks into its
// target (the victim's impact plays too), the rip as it pulls free -- or the swing round
// of a miss turning back -- and the slap of the catch. On the Axe Throw prefab; the
// flying axe (ThrownAxe) is listened to from here, so it needs nothing of its own.
[RequireComponent(typeof(AxeThrowSkill))]
public class AxeThrowAudio : MonoBehaviour
{
    public SoundEvent windup;
    public SoundEvent release;
    public SoundEvent spinLoop;
    [Tooltip("The spin's pitch on the way home (it comes back faster).")]
    [Range(1f, 2f)] public float returnPitch = 1.25f;
    public SoundEvent stick;
    public SoundEvent rip;
    public SoundEvent missTurn;
    public SoundEvent catchSlap;

    AxeThrowSkill skill;
    ThrownAxe flying;
    SoundHandle spin;

    void Awake() => skill = GetComponent<AxeThrowSkill>();

    void OnEnable()
    {
        skill.WindupStarted += OnWindup;
        skill.Released += OnReleased;
        skill.CaughtBack += OnCaught;
    }

    void OnDisable()
    {
        skill.WindupStarted -= OnWindup;
        skill.Released -= OnReleased;
        skill.CaughtBack -= OnCaught;
        Unhook();
        spin.Stop(0.1f);
    }

    void OnWindup() => Sfx.Play(windup, transform);

    void OnReleased(ThrownAxe axe)
    {
        Sfx.Play(release, transform);
        Unhook();
        flying = axe;
        if (flying == null) return;
        flying.Stuck += OnStuck;
        flying.Returning += OnReturning;
        spin.Stop(0.05f);
        if (spinLoop != null) spin = Sfx.PlayLoop(spinLoop, flying.transform);
    }

    void OnStuck(Vector2 point)
    {
        Sfx.Play(stick, point);
        spin.SetVolume(0f, 0.05f);   // no spin while it hangs in the target
    }

    void OnReturning(bool fromTarget)
    {
        Sfx.Play(fromTarget ? rip : missTurn, flying != null ? (Vector2)flying.transform.position : (Vector2)transform.position);
        spin.SetVolume(1f, 0.1f);
        spin.SetPitch(returnPitch);
    }

    void OnCaught(bool inHand)
    {
        spin.Stop(0.05f);
        Unhook();
        if (inHand) Sfx.Play(catchSlap, transform);
    }

    void Unhook()
    {
        if (flying == null) return;
        flying.Stuck -= OnStuck;
        flying.Returning -= OnReturning;
        flying = null;
    }
}

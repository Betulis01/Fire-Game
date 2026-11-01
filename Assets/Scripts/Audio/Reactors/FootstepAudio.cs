using UnityEngine;

// Footsteps (sound.md §3.1, the creatures' foot layers in §6): each step plays the
// shared ground step for the surface under the feet (AudioConfig.groundSounds, by
// GroundSensor) at the profile's groundVolume, the walker's own foot layer, and the
// player's load creak while carrying (the ground step a little lower then).
//
// Steps come from OnFootstep Animation Events on the run / walk clips' contact frames
// (Tools/Audio/Bake Footstep Events; EnemyArtSetup writes the wolf's, boar's and deer's),
// so they land with the art. Moving with no event for a beat -- a clip without them, or
// one playing backwards (the player's backpedal), which may not fire them -- falls back
// to a step every strideLength travelled, until events come again.
//
// The player also gets a soft slide loop on ice while moving (profile.iceSlide).
public class FootstepAudio : MonoBehaviour
{
    public FootstepProfile profile;

    [Tooltip("Moving with no footstep event for this long switches to stride steps.")]
    [Min(0.1f)] public float eventTimeout = 0.5f;

    const float MovingSpeed = 0.15f;   // units/s below which nothing steps

    GroundSensor ground;
    Hands hands;
    Health health;
    Vector2 lastPos;
    float speed, travelled, lastEvent = float.NegativeInfinity;
    SoundHandle slide;

    void Awake()
    {
        ground = GroundSensor.On(gameObject);
        hands = GetComponent<Hands>();
        health = GetComponent<Health>();
    }

    void OnEnable() => lastPos = transform.position;

    void OnDisable() => slide.Stop(0.1f);

    // Animation Event on the run / walk clips' contact frames.
    public void OnFootstep()
    {
        lastEvent = Time.time;
        travelled = 0f;
        if (speed >= MovingSpeed) Step();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Vector2 pos = transform.position;
        float d = (pos - lastPos).magnitude;
        lastPos = pos;
        speed = Mathf.Lerp(speed, d / dt, 1f - Mathf.Exp(-dt * 12f));

        bool alive = health == null || !health.IsDead;
        UpdateSlide(alive);
        if (!alive || profile == null || speed < MovingSpeed) { travelled = 0f; return; }

        // no events lately: step by distance
        if (Time.time - lastEvent < eventTimeout) return;
        travelled += d;
        if (travelled >= profile.strideLength)
        {
            travelled -= profile.strideLength;
            Step();
        }
    }

    void Step()
    {
        if (profile == null) return;
        AudioConfig config = AudioSystem.ActiveConfig;
        bool carrying = hands != null && hands.IsCarrying && profile.carryLayer != null;

        SoundEvent groundStep = config != null && config.groundSounds != null ? config.groundSounds.Step(ground.Ground) : null;
        Sfx.Play(groundStep, transform.position, profile.groundVolume, carrying ? 0.92f : 1f);
        Sfx.Play(profile.foot, transform.position);
        if (carrying) Sfx.Play(profile.carryLayer, transform.position);
    }

    void UpdateSlide(bool alive)
    {
        if (profile == null || profile.iceSlide == null) return;
        bool sliding = alive && ground.Ground == GroundType.Ice && ground.SlipTime > 0f && speed >= MovingSpeed;
        if (sliding && !slide.IsValid) slide = Sfx.PlayLoop(profile.iceSlide, transform, 0f);
        if (!slide.IsValid) return;
        if (sliding) slide.SetVolume(Mathf.Clamp01(speed / 3f), 0.1f);
        else slide.Stop(0.25f);
    }
}

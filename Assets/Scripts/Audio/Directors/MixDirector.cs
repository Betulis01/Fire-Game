using UnityEngine;
using UnityEngine.Audio;

// The continuous mix (SoundSystemPlan §3.6), through three exposed mixer parameters that
// no snapshot touches (a parameter set from code stops following snapshots):
//   shelter     the closer the player is to a fire's warmth, the more the wind is
//               lowpassed -- you hear the shelter (and, away from fire, the exposure)
//   freezing    while freezing, the world thins out under a highpass
//   low health  below lowHealth the effects muffle towards the edge of death (the
//               heartbeat is PlayerAudio's)
// Each eases over about a second, in log-frequency so the sweep sounds even. A missing
// parameter is warned about once (the mixer checklist in SoundSystemPlan Phase 6).
// The exclusive states -- Gameplay, Paused, Menu, End -- are snapshots, set by AudioSystem.
public class MixDirector : MonoBehaviour
{
    const float Open = 22000f, Closed = 10f;

    float shelter, freeze, hurt;   // 0..1, eased
    Transform player;
    PlayerTemperature temperature;
    Health health;
    float nextScan;
    bool warned;

    void Update()
    {
        AudioConfig c = AudioSystem.ActiveConfig;
        AudioMixer mixer = c != null ? c.mixer : null;
        if (mixer == null) return;

        if (player == null && Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.5f;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            player = p != null ? p.transform : null;
            temperature = p != null ? p.GetComponent<PlayerTemperature>() : null;
            health = p != null ? p.GetComponent<Health>() : null;
        }

        bool live = GameStateManager.Instance == null || GameStateManager.Instance.State == GameState.Playing
                    || GameStateManager.Instance.State == GameState.Paused;
        float wantShelter = live && temperature != null ? Mathf.Clamp01(temperature.Warmth / c.fullShelterWarmth) : 0f;
        float wantFreeze = live && temperature != null && temperature.IsFreezing ? 1f : 0f;
        float wantHurt = live && health != null && c.lowHealth > 0f && health.Normalized < c.lowHealth
            ? 1f - health.Normalized / c.lowHealth : 0f;

        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 3f);
        shelter = Mathf.Lerp(shelter, wantShelter, k);
        freeze = Mathf.Lerp(freeze, wantFreeze, k);
        hurt = Mathf.Lerp(hurt, wantHurt, k);

        bool ok = mixer.SetFloat(c.heatCutoffParam, LogLerp(Open, c.shelterCutoff, shelter))
                & mixer.SetFloat(c.freezeCutoffParam, LogLerp(Closed, c.freezeCutoff, freeze))
                & mixer.SetFloat(c.lowHealthCutoffParam, LogLerp(Open, c.lowHealthCutoff, hurt));
        if (!ok && !warned)
        {
            warned = true;
            Debug.LogWarning($"MixDirector: the mixer is missing an exposed parameter ({c.heatCutoffParam}, " +
                             $"{c.freezeCutoffParam} or {c.lowHealthCutoffParam}); see SoundSystemPlan Phase 6.");
        }
    }

    static float LogLerp(float a, float b, float t) => Mathf.Exp(Mathf.Lerp(Mathf.Log(a), Mathf.Log(b), t));
}

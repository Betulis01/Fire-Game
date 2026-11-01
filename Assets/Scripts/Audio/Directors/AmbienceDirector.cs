using UnityEngine;

// The world's air (sound.md §11.1, §11.2), from AudioConfig.ambience and the clock
// (DayNightCycle, found again after every scene load):
//   beds       the day and night winds, crossfaded by the hour: night rises over dusk ->
//              night (19 -> 22) and falls over dawn -> morning (5 -> 8)
//   scattered  gusts (more often at night), a trunk creaking at a random tree within
//              hearing, birds over dawn -> morning, owls and far howls at night -- each on
//              its own random clock, in game time, so they hold still while paused
//   stingers   dusk, night (the Night phase, 22:00 -- IsNight already starts at dusk)
//              and dawn, while playing
// The beds play in the menus too (under the Menu snapshot) and keep blowing while paused.
// Being near a fire softens them: that's MixDirector's heat lowpass.
public class AmbienceDirector : MonoBehaviour
{
    SoundHandle dayBed, nightBed;
    DayNightCycle clock;
    float nextGust, nextCreak, nextBirds, nextOwl, nextHowl;

    void OnDisable()
    {
        Unbind();
        dayBed.Stop(1f);
        nightBed.Stop(1f);
    }

    void Update()
    {
        AudioConfig config = AudioSystem.ActiveConfig;
        AmbienceSounds a = config != null ? config.ambience : null;
        if (a == null) return;

        if (clock != DayNightCycle.Instance) Bind(DayNightCycle.Instance, a);

        float night = NightWeight();
        if (!dayBed.IsValid && a.windDay != null) dayBed = Sfx.PlayLoop(a.windDay, null, 0f);
        if (!nightBed.IsValid && a.windNight != null) nightBed = Sfx.PlayLoop(a.windNight, null, 0f);
        dayBed.SetVolume(Mathf.Cos(night * Mathf.PI * 0.5f), 0.5f);
        nightBed.SetVolume(Mathf.Sin(night * Mathf.PI * 0.5f), 0.5f);

        float now = Time.time;
        bool isNight = night >= 0.5f;
        if (now >= nextGust)
        {
            Sfx.Play2D(a.gust.sound);
            nextGust = now + Interval(a.gust) * (isNight ? a.nightGustFactor : 1f);
        }
        if (now >= nextCreak)
        {
            if (TryNearTree(config, a.forestCreak.sound, out Vector2 at)) Sfx.Play(a.forestCreak.sound, at);
            nextCreak = now + Interval(a.forestCreak);
        }
        if (now >= nextBirds)
        {
            if (IsDawn()) Sfx.Play2D(a.dawnBirds.sound);
            nextBirds = now + Interval(a.dawnBirds);
        }
        if (now >= nextOwl)
        {
            if (isNight) Sfx.Play2D(a.owl.sound);
            nextOwl = now + Interval(a.owl);
        }
        if (now >= nextHowl)
        {
            if (isNight) Sfx.Play2D(a.howl.sound);
            nextHowl = now + Interval(a.howl);
        }
    }

    static float Interval(AmbienceSounds.Scatter s) =>
        Random.Range(Mathf.Max(1f, s.interval.x), Mathf.Max(1f, s.interval.x, s.interval.y));

    // 0 by day, 1 at night, eased across dusk -> night and dawn -> morning.
    float NightWeight()
    {
        if (clock == null) return 0f;
        float h = clock.Hour;
        if (h >= clock.duskHour && h < clock.nightHour) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(clock.duskHour, clock.nightHour, h));
        if (h >= clock.nightHour || h < clock.dawnHour) return 1f;
        if (h >= clock.dawnHour && h < clock.morningHour) return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(clock.dawnHour, clock.morningHour, h));
        return 0f;
    }

    bool IsDawn() => clock != null && clock.Hour >= clock.dawnHour && clock.Hour < clock.morningHour;

    // A tree within the creak's hearing, at random.
    static bool TryNearTree(AudioConfig config, SoundEvent creak, out Vector2 at)
    {
        at = default;
        var trees = DestructibleAudio.Creakers;
        if (creak == null || trees.Count == 0) return false;
        for (int tries = 0; tries < 6; tries++)
        {
            Transform t = trees[Random.Range(0, trees.Count)];
            if (t == null) continue;
            if (AudioSpace.RadiiAway(config, t.position, creak.rangeScale) < 0.9f) { at = t.position; return true; }
        }
        return false;
    }

    void Bind(DayNightCycle next, AmbienceSounds a)
    {
        Unbind();
        clock = next;
        if (clock != null) clock.PhaseChanged += OnPhase;
        float now = Time.time;
        nextGust = now + Interval(a.gust);
        nextCreak = now + Interval(a.forestCreak);
        nextBirds = now + Interval(a.dawnBirds);
        nextOwl = now + Interval(a.owl);
        nextHowl = now + Interval(a.howl);
    }

    void Unbind()
    {
        if (clock != null) clock.PhaseChanged -= OnPhase;
        clock = null;
    }

    void OnPhase(DayPhase phase)
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.State != GameState.Playing) return;
        AmbienceSounds a = AudioSystem.ActiveConfig != null ? AudioSystem.ActiveConfig.ambience : null;
        if (a == null) return;
        if (phase == DayPhase.Dusk) Sfx.Play2D(a.duskSting);
        else if (phase == DayPhase.Night) Sfx.Play2D(a.nightSting);
        else if (phase == DayPhase.Dawn) Sfx.Play2D(a.dawnSting);
    }
}

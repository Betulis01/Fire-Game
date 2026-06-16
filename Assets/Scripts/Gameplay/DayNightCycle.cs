using System;
using UnityEngine;

// the phases of the day, in the same chronological order as the schedule below
public enum DayPhase { Dawn, Morning, Noon, Afternoon, Dusk, Night }

// The game clock. Owns the time of day on a repeating 24-hour cycle, the phase
// schedule (the hour each phase begins), and whether it's currently night.
// Environment maps these phases onto ambient temperature and the global light;
// later systems (monster spawns, music) can hook NightChanged instead of polling.
// Uses Time.deltaTime, so it freezes on pause along with the rest of the world.
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    public float dayLengthSeconds = 300f;          // real seconds for one full 24h cycle
    [Range(0f, 24f)] public float startHour = 16f;  // time of day at play start

    // The hour each phase begins, in chronological order. This is the single source
    // of phase timing: Environment's per-phase temperature/light values are sampled
    // against this schedule, so the times live here only.
    [Header("Phase schedule (hour each phase begins)")]
    [Range(0f, 24f)] public float dawnHour = 5f;
    [Range(0f, 24f)] public float morningHour = 8f;
    [Range(0f, 24f)] public float noonHour = 12f;
    [Range(0f, 24f)] public float afternoonHour = 16f;
    [Range(0f, 24f)] public float duskHour = 19f;
    [Range(0f, 24f)] public float nightHour = 22f;

    // current time of day, 0..24
    public float Hour { get; private set; }

    // position in the cycle, 0..1. Handy for a clock readout later.
    public float NormalizedTime { get; private set; }

    // true from dusk through the following dawn (the dark, dangerous hours)
    public bool IsNight { get; private set; }

    // which phase the current hour falls in
    public DayPhase CurrentPhase
    {
        get
        {
            RefreshSchedule();
            int n = phaseHours.Length;
            for (int i = 0; i < n; i++)
            {
                float span = Mathf.Repeat(phaseHours[(i + 1) % n] - phaseHours[i], 24f);
                if (span <= 0f) continue;
                if (Mathf.Repeat(Hour - phaseHours[i], 24f) < span) return (DayPhase)i;
            }
            return DayPhase.Dawn;
        }
    }

    // raised when the cycle crosses into night (true) or back into day (false)
    public event Action<bool> NightChanged;

    // reusable buffer of the phase hours, in the same order Environment supplies
    // its values: dawn, morning, noon, afternoon, dusk, night. Avoids per-frame allocs.
    readonly float[] phaseHours = new float[6];

    float elapsed;

    void Awake() => Instance = this;

    void Update()
    {
        elapsed += Time.deltaTime;

        // fraction of the cycle, offset so the clock starts at startHour
        NormalizedTime = Mathf.Repeat(startHour / 24f + elapsed / dayLengthSeconds, 1f);
        Hour = NormalizedTime * 24f;

        UpdatePhase();
    }

    void UpdatePhase()
    {
        // night spans dusk -> following dawn, wrapping midnight when dusk > dawn
        bool night = duskHour > dawnHour
            ? (Hour >= duskHour || Hour < dawnHour)
            : (Hour >= duskHour && Hour < dawnHour);

        if (night == IsNight) return;

        IsNight = night;
        NightChanged?.Invoke(night);
    }

    // Sample per-phase values at the current hour. `values` is parallel to the phase
    // schedule, in the same chronological order (dawn, morning, noon, afternoon,
    // dusk, night). Interpolates linearly between adjacent phases and treats the day
    // as periodic, so the night->dawn segment wraps correctly across midnight. This
    // is how Environment turns phases into temperature/light without re-stating the times.
    public float SampleByPhase(float[] values)
    {
        RefreshSchedule();
        int n = phaseHours.Length;

        for (int i = 0; i < n; i++)
        {
            float h0 = phaseHours[i];
            float h1 = phaseHours[(i + 1) % n];

            float span = Mathf.Repeat(h1 - h0, 24f);  // length of this segment
            if (span <= 0f) continue;                  // skip zero/degenerate segments

            float into = Mathf.Repeat(Hour - h0, 24f); // how far we are into it
            if (into < span)
                return Mathf.Lerp(values[i], values[(i + 1) % n], into / span);
        }

        return values[0];
    }

    void RefreshSchedule()
    {
        phaseHours[0] = dawnHour;
        phaseHours[1] = morningHour;
        phaseHours[2] = noonHour;
        phaseHours[3] = afternoonHour;
        phaseHours[4] = duskHour;
        phaseHours[5] = nightHour;
    }
}

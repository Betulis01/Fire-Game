using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Throwaway audio overlay, added by AudioSystem in the editor and development builds.
// F9 toggles it: every live voice (event, gain, pan, what it follows), loops audible
// or virtual, steals and drops per second, and a ring at the hearing radius around the
// view's centre. While it's up, F8 picks one of AudioConfig.debugTestEvents, F10
// plays it at the cursor (a loop toggles on and off there) and F11 fires it 100 times
// at once -- the voice cap, cooldown and frame budget should hold that to a few.
// The mix meter (top line): the output's peak and RMS this frame, a 3 s RMS average as a
// rough loudness (not true LUFS), and the danger duck; under it, each bus's voice count
// and loudest voice. Each voice line shows its bus and mix tier.
// Reads the keyboard directly rather than through UserInput, so deleting this file
// removes the feature entirely.
public class AudioDebugOverlay : MonoBehaviour
{
    const float AverageSeconds = 3f;

    bool show;
    int selected;
    SoundHandle testLoop;
    GUIStyle style;
    Texture2D dot;
    readonly float[] samples = new float[1024];
    float peakDb = -80f, rmsDb = -80f, meanSquare;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f9Key.wasPressedThisFrame) show = !show;
        if (!show) return;
        Meter();

        SoundEvent[] tests = AudioSystem.Instance != null ? AudioSystem.Instance.Config.debugTestEvents : null;
        if (tests == null || tests.Length == 0) return;
        if (kb.f8Key.wasPressedThisFrame) selected = (selected + 1) % tests.Length;
        SoundEvent e = tests[Mathf.Clamp(selected, 0, tests.Length - 1)];
        if (e == null) return;

        Vector2 at = CursorWorld();
        if (kb.f10Key.wasPressedThisFrame)
        {
            if (!e.loop) Sfx.Play(e, at);
            else if (testLoop.IsValid) testLoop.Stop(0.3f);
            else testLoop = Sfx.PlayLoopAt(e, at);
        }
        if (kb.f11Key.wasPressedThisFrame && !e.loop)
            for (int i = 0; i < 100; i++) Sfx.Play(e, at + Random.insideUnitCircle * 0.5f);
    }

    // the listener's output, both channels: this frame's peak and RMS, and an exponential
    // average of the power over AverageSeconds
    void Meter()
    {
        float peak = 0f, sum = 0f;
        int n = 0;
        for (int ch = 0; ch < 2; ch++)
        {
            AudioListener.GetOutputData(samples, ch);
            foreach (float s in samples) { float a = Mathf.Abs(s); if (a > peak) peak = a; sum += s * s; }
            n += samples.Length;
        }
        float ms = sum / n;
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / AverageSeconds);
        meanSquare = Mathf.Lerp(meanSquare, ms, k);
        peakDb = ToDb(peak);
        rmsDb = ms > 1e-10f ? 10f * Mathf.Log10(ms) : -80f;
    }

    static float ToDb(float linear) => linear > 1e-4f ? 20f * Mathf.Log10(linear) : -80f;

    static string Bus(AudioSystem.Voice v) => v.ev.output != null ? v.ev.output.name : "Master";

    static Vector2 CursorWorld()
    {
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null) return AudioSpace.Centre;
        return cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
    }

    void OnGUI()
    {
        if (!show || AudioSystem.Instance == null) return;
        AudioSystem sys = AudioSystem.Instance;
        AudioConfig cfg = sys.Config;
        style ??= new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
        if (dot == null) { dot = new Texture2D(1, 1); dot.SetPixel(0, 0, Color.white); dot.Apply(); }

        var sb = new System.Text.StringBuilder();
        int live = 0;
        foreach (AudioSystem.Voice v in sys.OneShotVoices) if (v.active) live++;
        int audible = 0;
        foreach (AudioSystem.Voice v in sys.LoopVoices) if (v.source != null) audible++;
        sb.Append($"<b>Audio</b>  one-shots {live}/{sys.OneShotVoices.Length}   loops {audible} audible, " +
                  $"{sys.LoopVoices.Count - audible} virtual   steals/s {sys.Steals}   drops/s {sys.Drops}" +
                  $"   radius {AudioSpace.Radius(cfg, 1f):0.0}u   paused {AudioListener.pause}\n");
        float avgDb = meanSquare > 1e-10f ? 10f * Mathf.Log10(meanSquare) : -80f;
        string peakColour = peakDb > -1f ? "#ff6060" : peakDb > -6f ? "#ffd060" : "#a0ffa0";
        sb.Append($"<b>Out</b>  peak <color={peakColour}>{peakDb,6:0.0}</color> dBFS   rms {rmsDb,6:0.0} dB   " +
                  $"≈ loudness ({AverageSeconds:0} s) {avgDb,6:0.0} dB   duck {sys.DuckDb:0.0} dB\n");

        // per bus: how many voices, and the loudest
        var buses = new SortedDictionary<string, (int count, float loudest)>();
        void Count(AudioSystem.Voice v)
        {
            buses.TryGetValue(Bus(v), out var b);
            buses[Bus(v)] = (b.count + 1, b.count == 0 ? v.gain : Mathf.Max(b.loudest, v.gain));
        }
        foreach (AudioSystem.Voice v in sys.OneShotVoices) if (v.active) Count(v);
        foreach (AudioSystem.Voice v in sys.LoopVoices) if (v.source != null) Count(v);
        foreach (var kv in buses) sb.Append($"{kv.Key} {kv.Value.count} ({ToDb(kv.Value.loudest):0} dB)   ");
        if (buses.Count > 0) sb.Append('\n');

        SoundEvent[] tests = cfg.debugTestEvents;
        if (tests != null && tests.Length > 0)
        {
            SoundEvent t = tests[Mathf.Clamp(selected, 0, tests.Length - 1)];
            sb.Append($"test (F8/F10/F11): <b>{(t != null ? t.name : "-")}</b>\n");
        }
        foreach (AudioSystem.Voice v in sys.OneShotVoices)
            if (v.active) sb.Append(Line(v, "  "));
        foreach (AudioSystem.Voice v in sys.LoopVoices)
            sb.Append(Line(v, v.source != null ? "~ " : "· "));

        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(8, 8, 760, 18 + 15 * (live + sys.LoopVoices.Count + 4)), dot);
        GUI.color = Color.white;
        GUI.Label(new Rect(14, 10, 750, 2000), sb.ToString(), style);

        DrawRadius(cfg);
    }

    static string Line(AudioSystem.Voice v, string mark)
    {
        float db = v.gain > 0f ? 20f * Mathf.Log10(v.gain) : -80f;
        string where = v.screen ? "2D" : v.hasFollow && v.follow != null ? v.follow.name : $"{v.point.x:0.0},{v.point.y:0.0}";
        return $"{mark}{v.ev.name,-34} {db,6:0.0} dB  {Bus(v),-9} {v.ev.tier,-8} {where}\n";
    }

    // the hearing radius at rangeScale 1, as a dotted ring on screen
    void DrawRadius(AudioConfig cfg)
    {
        Camera cam = Camera.main;
        if (cam == null || AudioSpace.HalfWidth <= 0f) return;
        float r = AudioSpace.Radius(cfg, 1f);
        GUI.color = new Color(0.4f, 0.9f, 1f, 0.8f);
        for (int i = 0; i < 96; i++)
        {
            float a = i * Mathf.PI * 2f / 96f;
            Vector3 w = (Vector3)(AudioSpace.Centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            Vector3 s = cam.WorldToScreenPoint(w);
            GUI.DrawTexture(new Rect(s.x - 1, Screen.height - s.y - 1, 3, 3), dot);
        }
        GUI.color = Color.white;
    }
}

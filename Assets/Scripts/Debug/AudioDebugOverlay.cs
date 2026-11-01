using UnityEngine;
using UnityEngine.InputSystem;

// Throwaway audio overlay, added by AudioSystem in the editor and development builds.
// F9 toggles it: every live voice (event, gain, pan, what it follows), loops audible
// or virtual, steals and drops per second, and a ring at the hearing radius around the
// view's centre. While it's up, F8 picks one of AudioConfig.debugTestEvents, F10
// plays it at the cursor (a loop toggles on and off there) and F11 fires it 100 times
// at once -- the voice cap, cooldown and frame budget should hold that to a few.
// Reads the keyboard directly rather than through UserInput, so deleting this file
// removes the feature entirely.
public class AudioDebugOverlay : MonoBehaviour
{
    bool show;
    int selected;
    SoundHandle testLoop;
    GUIStyle style;
    Texture2D dot;

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f9Key.wasPressedThisFrame) show = !show;
        if (!show) return;

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
        GUI.DrawTexture(new Rect(8, 8, 620, 18 + 15 * (live + sys.LoopVoices.Count + 2)), dot);
        GUI.color = Color.white;
        GUI.Label(new Rect(14, 10, 610, 2000), sb.ToString(), style);

        DrawRadius(cfg);
    }

    static string Line(AudioSystem.Voice v, string mark)
    {
        float db = v.gain > 0f ? 20f * Mathf.Log10(v.gain) : -80f;
        string where = v.screen ? "2D" : v.hasFollow && v.follow != null ? v.follow.name : $"{v.point.x:0.0},{v.point.y:0.0}";
        return $"{mark}{v.ev.name,-34} {db,6:0.0} dB  {where}\n";
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

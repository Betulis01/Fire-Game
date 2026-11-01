using UnityEditor;
using UnityEngine;

// SoundEvent inspector: the sound.md brief on top (what to record), then the fields, then
// ▶ Play / ■ Stop with the event's own jitter -- in edit mode through a hidden preview
// AudioSource, in play mode through Sfx (so the bus, limits and attenuation apply) -- and
// Analyse, which measures each clip's peak and RMS and flags variants more than 2 dB
// apart or with leading silence (the swing feels late).
[CustomEditor(typeof(SoundEvent)), CanEditMultipleObjects]
public class SoundEventEditor : Editor
{
    static AudioSource preview;
    static SoundHandle playModeLoop;
    string analysis;

    public override void OnInspectorGUI()
    {
        var e = (SoundEvent)target;
        if (!string.IsNullOrEmpty(e.brief))
        {
            string head = $"{e.catalogPriority}  ·  {e.catalogSection}" + (e.catalogVariants > 0 ? $"  ·  {e.catalogVariants} variants" : "");
            EditorGUILayout.HelpBox(head + "\n" + e.brief, MessageType.None);
        }

        DrawDefaultInspector();

        int count = 0;
        float length = 0f;
        if (e.clips != null) foreach (AudioClip c in e.clips) if (c != null) { count++; length += c.length; }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{count} clip(s), {length:0.00} s total" +
                                   (e.catalogVariants > 0 && count < e.catalogVariants ? $"  (sound.md asks for {e.catalogVariants})" : ""),
                                   EditorStyles.miniLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = e.HasClips;
            if (GUILayout.Button("▶ Play")) Play(e);
            GUI.enabled = true;
            if (GUILayout.Button("■ Stop")) Stop();
            GUI.enabled = e.HasClips;
            if (GUILayout.Button("Analyse")) analysis = Analyse(e);
            GUI.enabled = true;
        }
        if (!string.IsNullOrEmpty(analysis)) EditorGUILayout.HelpBox(analysis, MessageType.Info);
    }

    static void Play(SoundEvent e)
    {
        if (Application.isPlaying)
        {
            if (!e.loop) Sfx.Play2D(e);
            else { playModeLoop.Stop(); playModeLoop = Sfx.PlayLoop(e, null); }
            return;
        }

        AudioClip clip = null;
        for (int tries = 0; tries < 8 && clip == null; tries++) clip = e.clips[Random.Range(0, e.clips.Length)];
        if (clip == null) return;

        PlayClip(clip, e.loop, e.output);
        preview.volume = AudioSystem.DbToLinear(e.volumeDb + Random.Range(-e.volumeJitterDb, e.volumeJitterDb));
        preview.pitch = Mathf.Pow(2f, Random.Range(-e.pitchJitterSemitones, e.pitchJitterSemitones) / 12f);
    }

    // a bare clip through the hidden preview source (also the Sound Import window's ▶)
    internal static void PlayClip(AudioClip clip, bool loop, UnityEngine.Audio.AudioMixerGroup output = null)
    {
        if (preview == null)
        {
            var go = EditorUtility.CreateGameObjectWithHideFlags("SoundEvent Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
            preview = go.GetComponent<AudioSource>();
            preview.playOnAwake = false;
            preview.spatialBlend = 0f;
        }
        preview.Stop();
        preview.clip = clip;
        preview.loop = loop;
        preview.outputAudioMixerGroup = output;
        preview.volume = 1f;
        preview.pitch = 1f;
        preview.Play();
    }

    internal static void Stop()
    {
        if (preview != null) preview.Stop();
        playModeLoop.Stop(0.2f);
    }

    void OnDisable() => Stop();

    // Peak and RMS (dBFS) per clip, from its samples. Streamed clips can't be read.
    static string Analyse(SoundEvent e)
    {
        var sb = new System.Text.StringBuilder();
        float minRms = float.MaxValue, maxRms = float.MinValue;
        foreach (AudioClip c in e.clips)
        {
            if (c == null) continue;
            if (!ClipStats(c, out float peak, out float rms, out float lead))
            {
                sb.AppendLine($"{c.name}: can't read samples ({c.loadType})");
                continue;
            }
            minRms = Mathf.Min(minRms, rms);
            maxRms = Mathf.Max(maxRms, rms);
            sb.AppendLine($"{c.name}: peak {peak:0.0} dB, RMS {rms:0.0} dB" +
                          (lead > 0.01f ? $", {lead * 1000f:0} ms leading silence" : "") +
                          (peak > -0.5f ? "  (clipping risk)" : ""));
        }
        if (maxRms - minRms > 2f) sb.AppendLine($"Variants differ by {maxRms - minRms:0.0} dB RMS (keep them within ±1 dB).");
        return sb.ToString().TrimEnd();
    }

    internal static bool ClipStats(AudioClip c, out float peakDb, out float rmsDb, out float leadSeconds)
    {
        peakDb = rmsDb = -80f;
        leadSeconds = 0f;
        if (c.loadType == AudioClipLoadType.Streaming) return false;
        if (c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData();
        var data = new float[c.samples * c.channels];
        if (data.Length == 0 || !c.GetData(data, 0)) return false;

        double sum = 0;
        float peak = 0f;
        int firstLoud = -1;
        for (int i = 0; i < data.Length; i++)
        {
            float a = Mathf.Abs(data[i]);
            if (a > peak) peak = a;
            if (firstLoud < 0 && a > 0.01f) firstLoud = i;   // -40 dBFS
            sum += a * a;
        }
        peakDb = peak > 0f ? 20f * Mathf.Log10(peak) : -80f;
        float rms = Mathf.Sqrt((float)(sum / data.Length));
        rmsDb = rms > 0f ? 20f * Mathf.Log10(rms) : -80f;
        leadSeconds = firstLoud < 0 ? 0f : firstLoud / (float)c.channels / c.frequency;
        return true;
    }
}

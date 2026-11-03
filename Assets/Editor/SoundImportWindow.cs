using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Tools/Audio/Sound Import: real recordings onto events (SoundSystemPlan Phase 7,
// SoundSourcing.md for where to find them).
//   1. Put downloaded files (wav, ogg, mp3, aiff, flac) anywhere under Assets/Audio/_Incoming/.
//      That folder isn't in git and imports uncompressed (AudioImportRules).
//   2. Fill in where the batch came from -- source, author, licence, URL. One batch per pack.
//   3. Drag files from the left list (or the Project window) onto events on the right. ▶
//      auditions. The event list starts with what's still silent, P1 first.
//   4. Import. Small sounds (steps, foley, UI: their mix tier) are high-passed at 150 Hz
//      first, so their rumble stays out of the low end (the High-pass option: Auto / Off /
//      Custom). Each file is levelled to a -1 dBFS peak. One-shots lose their leading and
//      trailing silence (with fades of a few ms); loops get their seam crossfaded. Mono
//      unless it's a wind bed or music. Written as <id>_NN.wav under Assets/Audio/Clips/
//      (the folder by kind), with one LICENSES.csv row each, and added to the event's
//      clips. A placeholder linked by hand is unlinked; the files in Clips/_Placeholder stay.
//      Imported sources are moved to _Incoming/_Done, never deleted.
//      A clip that's already imported (anything under Clips/ but the placeholders: the left
//      list's "Imported clips" tab, the Project window, or a clip under another event) is
//      only linked -- no new file, no second LICENSES.csv row -- so one sound can serve
//      several events (each with its own Pitch, e.g. an axe +2 and a pickaxe -3).
//   5. To take a clip off an event, expand the event and press ✕ beside it. The clip is
//      unlinked; an imported clip also loses its LICENSES.csv row and its file moves to
//      _Incoming/_Removed (never deleted). A placeholder, or a clip another event also
//      uses, is only unlinked.
// Levels between events are the mix session's job (each event's Volume dB), not the clips'.
public class SoundImportWindow : EditorWindow
{
    const string Incoming = "Assets/Audio/_Incoming";
    const string Done = Incoming + "/_Done";
    const string Removed = Incoming + "/_Removed";
    const string Placeholders = AudioImportRules.ClipsRoot + "_Placeholder";
    const string LicensesPath = "Assets/Audio/LICENSES.csv";
    const float PeakDb = -1f;
    const float LeadGate = 0.01f, TailGate = 0.001f;   // -40 and -60 dBFS, after levelling

    static readonly string[] Licences = { "CC0", "CC-BY 4.0", "CC-BY 3.0", "Royalty-free (no attribution)", "Own recording" };
    static readonly string[] Shows = { "Silent", "Silent + placeholder", "All" };
    static readonly string[] Lefts = { "_Incoming", "Imported clips" };
    static readonly string[] HighPasses = { "Auto (by mix tier)", "Off", "Custom Hz" };
    const float AutoHighPassHz = 150f;
    static readonly Regex ClipName = new(@"^(.+)_(\d{2,3})$");

    [System.Serializable]
    class Entry
    {
        public SoundEvent ev;
        public List<AudioClip> clips = new();
    }

    [SerializeField] List<Entry> entries = new();
    [SerializeField] string source = "", author = "", url = "", search = "", librarySearch = "";
    [SerializeField] int licence, show = 1, left, highPass;
    [SerializeField] float highPassHz = AutoHighPassHz;
    [SerializeField] bool crossfadeLoops = true, moveSources = true;

    List<SoundEvent> events;
    List<AudioClip> incoming, library;
    Vector2 leftScroll, rightScroll;
    string openId;
    System.Action deferred;   // list changes wait for the end of OnGUI, so the layout stays whole

    enum Status { Silent, Placeholder, Real }

    [MenuItem("Tools/Audio/Sound Import")]
    static void Open() => GetWindow<SoundImportWindow>("Sound Import");

    void OnEnable() => Reload();
    void OnProjectChange() { Reload(); Repaint(); }
    void OnDisable() => SoundEventEditor.Stop();

    void Reload()
    {
        events = AssetDatabase.FindAssets("t:SoundEvent", new[] { "Assets/Audio/Events" })
            .Select(g => AssetDatabase.LoadAssetAtPath<SoundEvent>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(e => e != null)
            .OrderBy(e => string.IsNullOrEmpty(e.catalogPriority) ? "P9" : e.catalogPriority).ThenBy(e => e.name)
            .ToList();
        incoming = AssetDatabase.IsValidFolder(Incoming)
            ? AssetDatabase.FindAssets("t:AudioClip", new[] { Incoming })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith(Done + "/") && !p.StartsWith(Removed + "/"))
                .Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p))
                .Where(c => c != null)
                .OrderBy(c => c.name)
                .ToList()
            : new List<AudioClip>();
        library = AssetDatabase.IsValidFolder(AudioImportRules.ClipsRoot.TrimEnd('/'))
            ? AssetDatabase.FindAssets("t:AudioClip", new[] { AudioImportRules.ClipsRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith(Placeholders + "/"))
                .Select(p => AssetDatabase.LoadAssetAtPath<AudioClip>(p))
                .Where(c => c != null)
                .OrderBy(c => c.name)
                .ToList()
            : new List<AudioClip>();
        entries.RemoveAll(x => x.ev == null);
        foreach (Entry x in entries) x.clips.RemoveAll(c => c == null);
    }

    // ---------------------------------------------------------------- GUI

    void OnGUI()
    {
        if (events == null) Reload();

        EditorGUILayout.LabelField("Batch (one pack or source at a time)", EditorStyles.boldLabel);
        source = EditorGUILayout.TextField(new GUIContent("Source", "Pack or page name, e.g. '42 Snow and Gravel Footsteps (OpenGameArt)'"), source);
        author = EditorGUILayout.TextField("Author", author);
        licence = EditorGUILayout.Popup("Licence", licence, Licences);
        url = EditorGUILayout.TextField("URL", url);
        using (new EditorGUILayout.HorizontalScope())
        {
            crossfadeLoops = GUILayout.Toggle(crossfadeLoops, "Crossfade loop seams");
            moveSources = GUILayout.Toggle(moveSources, "Move imported sources to _Incoming/_Done");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            highPass = EditorGUILayout.Popup(new GUIContent("High-pass",
                $"Cuts the bass out of small sounds so it stays free for the big ones. Auto: {AutoHighPassHz:0} Hz on " +
                "steps, foley and UI (their mix tier), off for the rest. Custom: this frequency on every file in the batch."),
                highPass, HighPasses);
            if (highPass == 2) highPassHz = Mathf.Clamp(EditorGUILayout.FloatField(highPassHz, GUILayout.Width(60)), 20f, 2000f);
        }
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.36f))) DrawIncoming();
            using (new EditorGUILayout.VerticalScope()) DrawEvents();
        }
        DrawFooter();

        System.Action d = deferred;
        deferred = null;
        if (d != null) { d(); Repaint(); }
    }

    void DrawIncoming()
    {
        left = GUILayout.Toolbar(left, new[] { $"{Lefts[0]} ({incoming.Count})", $"{Lefts[1]} ({library.Count})" });
        if (left == 1) { DrawLibrary(); return; }
        if (!AssetDatabase.IsValidFolder(Incoming))
        {
            EditorGUILayout.HelpBox("Put downloaded sound files in Assets/Audio/_Incoming/.", MessageType.Info);
            if (GUILayout.Button("Create the folder")) deferred = () => { SoundEventSync.EnsureFolder(Incoming); Reload(); };
            return;
        }

        var assigned = new HashSet<AudioClip>(entries.SelectMany(x => x.clips));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        foreach (AudioClip c in incoming)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶", GUILayout.Width(24))) SoundEventEditor.PlayClip(c, false);
                var label = new GUIContent((assigned.Contains(c) ? "✓ " : "") + c.name,
                                           $"{AssetDatabase.GetAssetPath(c)}\n{c.length:0.00} s, {c.channels} ch, {c.frequency} Hz\nDrag onto an event.");
                Rect r = GUILayoutUtility.GetRect(label, EditorStyles.label, GUILayout.ExpandWidth(true));
                GUI.Label(r, label);
                DragSource(r, c);
            }
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("■ Stop")) SoundEventEditor.Stop();
    }

    // clips already imported: dragged onto an event, they're linked, not imported again
    void DrawLibrary()
    {
        librarySearch = EditorGUILayout.TextField("Filter", librarySearch);
        string needle = librarySearch.Trim().ToLowerInvariant();
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        foreach (AudioClip c in library)
        {
            if (needle.Length > 0 && !c.name.ToLowerInvariant().Contains(needle)) continue;
            string[] users = events.Where(e => e.clips != null && e.clips.Contains(c)).Select(e => e.name).ToArray();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶", GUILayout.Width(24))) SoundEventEditor.PlayClip(c, false);
                var label = new GUIContent(c.name + (users.Length == 0 ? "  (unused)" : users.Length > 1 ? $"  ({users.Length} events)" : ""),
                                           $"{AssetDatabase.GetAssetPath(c)}\nUsed by: {(users.Length > 0 ? string.Join(", ", users) : "nothing")}\n" +
                                           "Drag onto an event to link it there too (no new file).");
                Rect r = GUILayoutUtility.GetRect(label, EditorStyles.label, GUILayout.ExpandWidth(true));
                GUI.Label(r, label);
                DragSource(r, c);
            }
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("■ Stop")) SoundEventEditor.Stop();
    }

    void DrawEvents()
    {
        show = GUILayout.Toolbar(show, Shows);
        search = EditorGUILayout.TextField("Search", search);
        string needle = search.Trim().ToLowerInvariant();

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
        foreach (SoundEvent e in events)
        {
            Status status = StatusOf(e);
            // a search looks through every event, whatever the filter
            if (needle.Length == 0 && ((show == 0 && status != Status.Silent) || (show == 1 && status == Status.Real))) continue;
            if (needle.Length > 0 && !e.name.Contains(needle) && !(e.brief ?? "").ToLowerInvariant().Contains(needle)) continue;

            Entry entry = entries.Find(x => x.ev == e);
            int queued = entry?.clips.Count ?? 0;
            string label = $"{e.catalogPriority}  {e.name}"
                           + (status == Status.Placeholder ? "  (placeholder)" : status == Status.Real ? $"  ({e.clips.Length} real)" : "")
                           + (queued > 0 ? $"   ← {queued}" : "");

            Rect row = EditorGUILayout.GetControlRect();
            if (Drop(row, out AudioClip[] dropped)) deferred = () => { Add(e, dropped); openId = e.name; };
            bool open = openId == e.name;
            bool now = EditorGUI.Foldout(row, open, new GUIContent(label, e.brief), true);
            if (now != open) openId = now ? e.name : null;
            if (!now) continue;

            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(e.brief))
                EditorGUILayout.HelpBox(e.brief + (e.catalogVariants > 0 ? $"\nsound.md asks for {e.catalogVariants} variant(s)." : "")
                                        + (e.loop ? "\nA loop." : ""), MessageType.None);

            DrawTuning(e);

            // what the event plays now
            AudioClip[] current = e.clips ?? new AudioClip[0];
            foreach (AudioClip c in current)
            {
                if (c == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("▶", "At the event's volume and pitch"), GUILayout.Width(24))) SoundEventEditor.PlayClipAs(e, c);
                    var clipLabel = new GUIContent(c.name + (IsPlaceholder(c) ? "  (placeholder)" : ""),
                                                   AssetDatabase.GetAssetPath(c) + "\nDrag onto another event to link it there too. Double-click: show in Project.");
                    Rect r = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(clipLabel, EditorStyles.label, GUILayout.ExpandWidth(true)));
                    GUI.Label(r, clipLabel);
                    DragSource(r, c);
                    if (GUILayout.Button(new GUIContent("✕", "Take this clip off the event"), GUILayout.Width(24)))
                        EditorApplication.delayCall += () => Remove(e, c);
                }
            }
            int empty = current.Count(c => c == null);
            if (empty > 0 && GUILayout.Button($"Drop {empty} empty clip slot(s)"))
                EditorApplication.delayCall += () => DropEmpty(e);

            if (entry != null)
                for (int i = 0; i < entry.clips.Count; i++)
                {
                    int at = i;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("▶", GUILayout.Width(24)) && entry.clips[at] != null) SoundEventEditor.PlayClip(entry.clips[at], e.loop);
                        bool link = IsLibrary(entry.clips[at]);
                        float hp = link ? 0f : HighPassFor(e);
                        GUILayout.Label(new GUIContent(link ? "link" : hp > 0f ? $"new, HP {hp:0}" : "new",
                                                       link ? "Already imported: only linked, no new file."
                                                       : "Imported as a new clip." + (hp > 0f ? $" High-passed at {hp:0} Hz (the bass cut out)." : "")),
                                        GUILayout.Width(hp > 0f ? 80 : 34));
                        entry.clips[at] = (AudioClip)EditorGUILayout.ObjectField(entry.clips[at], typeof(AudioClip), false);
                        if (GUILayout.Button("✕", GUILayout.Width(24))) deferred = () => entry.clips.RemoveAt(at);
                    }
                }
            var add = (AudioClip)EditorGUILayout.ObjectField("Add", null, typeof(AudioClip), false);
            if (add != null) deferred = () => Add(e, new[] { add });
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndScrollView();
    }

    // the event's level and pitch, edited on the asset itself (undoable), and heard at once
    static void DrawTuning(SoundEvent e)
    {
        var so = new SerializedObject(e);
        EditorGUILayout.PropertyField(so.FindProperty("volumeDb"), new GUIContent("Volume dB"));
        EditorGUILayout.PropertyField(so.FindProperty("pitchSemitones"), new GUIContent("Pitch (semitones)",
            "Varispeed: up is also shorter and lighter, down longer and deeper."));
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(so.FindProperty("volumeJitterDb"), new GUIContent("± dB"));
            EditorGUILayout.PropertyField(so.FindProperty("pitchJitterSemitones"), new GUIContent("± st"));
        }
        so.ApplyModifiedProperties();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            GUI.enabled = e.HasClips;
            if (GUILayout.Button(new GUIContent("▶ Play event", "A random variant with the jitter, as the game plays it"))) SoundEventEditor.Play(e);
            GUI.enabled = true;
            if (GUILayout.Button("■ Stop", GUILayout.Width(60))) SoundEventEditor.Stop();
        }
    }

    void DrawFooter()
    {
        int files = entries.Sum(x => x.clips.Count(c => c != null && !IsLibrary(c)));
        int links = entries.Sum(x => x.clips.Count(IsLibrary));
        int onto = entries.Count(x => x.clips.Any(c => c != null));
        EditorGUILayout.Space();
        if (files > 0 && string.IsNullOrWhiteSpace(source))
            EditorGUILayout.HelpBox("Fill in Source (and Author and URL) so LICENSES.csv knows where these came from.", MessageType.Warning);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = files > 0 ? !string.IsNullOrWhiteSpace(source) : links > 0;
            string what = files > 0 && links > 0 ? $"Import {files} file(s) and link {links} clip(s)"
                        : links > 0 ? $"Link {links} clip(s)" : $"Import {files} file(s)";
            if (GUILayout.Button($"{what} onto {onto} event(s)", GUILayout.Height(26))) EditorApplication.delayCall += Import;
            GUI.enabled = files + links > 0;
            if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(26))) deferred = entries.Clear;
            GUI.enabled = true;
        }
    }

    void Add(SoundEvent e, IEnumerable<AudioClip> clips)
    {
        Entry entry = entries.Find(x => x.ev == e);
        if (entry == null) entries.Add(entry = new Entry { ev = e });
        foreach (AudioClip c in clips)
            if (c != null && !entry.clips.Contains(c) && !(e.clips != null && e.clips.Contains(c))) entry.clips.Add(c);
    }

    static void DragSource(Rect r, AudioClip c)
    {
        Event ev = Event.current;
        if (!r.Contains(ev.mousePosition)) return;
        if (ev.type == EventType.MouseDown && ev.clickCount == 2) { EditorGUIUtility.PingObject(c); ev.Use(); }
        else if (ev.type == EventType.MouseDrag)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[] { c };
            DragAndDrop.StartDrag(c.name);
            ev.Use();
        }
    }

    static bool Drop(Rect r, out AudioClip[] clips)
    {
        clips = null;
        Event ev = Event.current;
        if (!r.Contains(ev.mousePosition) || (ev.type != EventType.DragUpdated && ev.type != EventType.DragPerform)) return false;
        AudioClip[] found = DragAndDrop.objectReferences.OfType<AudioClip>().ToArray();
        if (found.Length == 0) return false;
        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        bool perform = ev.type == EventType.DragPerform;
        if (perform) { DragAndDrop.AcceptDrag(); clips = found; }
        ev.Use();
        return perform;
    }

    static Status StatusOf(SoundEvent e)
    {
        bool any = false, real = false;
        if (e.clips != null)
            foreach (AudioClip c in e.clips)
                if (c != null) { any = true; real |= !IsPlaceholder(c); }
        return real ? Status.Real : any ? Status.Placeholder : Status.Silent;
    }

    static bool IsPlaceholder(AudioClip c) => AssetDatabase.GetAssetPath(c).StartsWith(Placeholders + "/");

    // the high-pass a new file for this event gets (0 = none)
    float HighPassFor(SoundEvent e)
    {
        if (highPass == 1) return 0f;
        if (highPass == 2) return highPassHz;
        SoundEvent.MixTier t = e.tier != SoundEvent.MixTier.Unset ? e.tier : MixTiers.Classify(e);
        return MixTiers.HighPassed(t) ? AutoHighPassHz : 0f;
    }

    // already imported (it has its file and licence row): linked, never imported again
    static bool IsLibrary(AudioClip c) =>
        c != null && AssetDatabase.GetAssetPath(c).StartsWith(AudioImportRules.ClipsRoot) && !IsPlaceholder(c);

    // ---------------------------------------------------------------- import

    void Import()
    {
        var rows = new List<string>();
        var written = new List<(SoundEvent e, string path)>();
        var linked = new List<(SoundEvent e, AudioClip clip)>();
        var sources = new HashSet<AudioClip>();
        var failed = new List<string>();

        foreach (Entry x in entries)
        {
            SoundEvent e = x.ev;
            if (e == null) continue;
            linked.AddRange(x.clips.Where(IsLibrary).Select(c => (e, c)));
            List<AudioClip> clips = x.clips.Where(c => c != null && !IsLibrary(c)).ToList();
            if (clips.Count == 0) continue;

            string folder = SoundEventSync.EnsureFolder(FolderFor(e));
            int next = NextIndex(e);
            foreach (AudioClip src in clips)
            {
                float hp = HighPassFor(e);
                if (!Process(src, e.loop, KeepsStereo(e), crossfadeLoops, hp, out float[] data, out int channels, out string why))
                {
                    failed.Add($"{src.name} → {e.name}: {why}");
                    continue;
                }
                string path;
                while (File.Exists(path = $"{folder}/{e.name}_{next:00}.wav")) next++;
                next++;
                WriteWav(path, data, channels, src.frequency);
                rows.Add(Csv(Path.GetFileName(path), source, author, Licences[licence], url,
                             "from " + Path.GetFileName(AssetDatabase.GetAssetPath(src)) + (hp > 0f ? $"; high-passed {hp:0} Hz" : "")));
                written.Add((e, path));
                sources.Add(src);
            }
        }

        AssetDatabase.Refresh();
        foreach (SoundEvent e in written.Select(w => w.e).Concat(linked.Select(l => l.e)).Distinct())
        {
            IEnumerable<AudioClip> kept = (e.clips ?? new AudioClip[0]).Where(c => c != null && !IsPlaceholder(c));
            IEnumerable<AudioClip> added = written.Where(w => w.e == e)
                .Select(w => AssetDatabase.LoadAssetAtPath<AudioClip>(w.path)).Where(c => c != null);
            IEnumerable<AudioClip> links = linked.Where(l => l.e == e).Select(l => l.clip);
            e.clips = kept.Concat(added).Concat(links).Distinct().ToArray();
            EditorUtility.SetDirty(e);
        }

        List<string> lines = File.Exists(LicensesPath)
            ? File.ReadAllLines(LicensesPath).ToList()
            : new List<string> { "file,source,author,licence,url,notes" };
        lines = lines.Take(1)
            .Concat(lines.Skip(1).Where(l => l.Trim().Length > 0))
            .Concat(rows).ToList();
        File.WriteAllText(LicensesPath, string.Join("\n", lines) + "\n");

        if (moveSources)
        {
            SoundEventSync.EnsureFolder(Done);
            foreach (AudioClip c in sources)
            {
                string p = AssetDatabase.GetAssetPath(c);
                if (p.StartsWith(Incoming + "/") && !p.StartsWith(Done + "/"))
                    AssetDatabase.MoveAsset(p, AssetDatabase.GenerateUniqueAssetPath($"{Done}/{Path.GetFileName(p)}"));
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        entries.Clear();
        Reload();
        Repaint();

        int touched = written.Select(w => w.e).Concat(linked.Select(l => l.e)).Distinct().Count();
        string summary = $"{written.Count} clip(s) imported, {linked.Count} linked, onto {touched} event(s).";
        var report = new StringBuilder("Sound Import: " + summary);
        foreach (string f in failed) report.Append("\n  • not imported: " + f);
        foreach (var w in written) report.Append($"\n  {w.e.name} ← {w.path}");
        foreach (var l in linked) report.Append($"\n  {l.e.name} ← {AssetDatabase.GetAssetPath(l.clip)} (linked)");
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Sound Import",
            summary +
            (failed.Count > 0 ? $"\n{failed.Count} file(s) not imported (see the Console)." : ""), "OK");
    }

    // ---------------------------------------------------------------- remove

    void Remove(SoundEvent e, AudioClip clip)
    {
        if (e == null || clip == null) return;
        string path = AssetDatabase.GetAssetPath(clip);
        bool shared = events.Any(o => o != e && o.clips != null && o.clips.Contains(clip));
        bool ours = path.StartsWith(AudioImportRules.ClipsRoot) && !IsPlaceholder(clip) && !shared;
        string what = ours
            ? $"Its LICENSES.csv row is removed and the file moves to {Removed}."
            : shared ? "Another event uses it too, so the file stays." : "The file stays where it is.";
        if (!EditorUtility.DisplayDialog("Sound Import", $"Take {clip.name} off {e.name}?\n\n{what}", "Remove", "Cancel")) return;

        e.clips = (e.clips ?? new AudioClip[0]).Where(c => c != clip).ToArray();
        EditorUtility.SetDirty(e);
        AssetDatabase.SaveAssets();

        if (ours)
        {
            string file = Path.GetFileName(path);
            if (File.Exists(LicensesPath))
            {
                List<string> lines = File.ReadAllLines(LicensesPath).ToList();
                lines = lines.Take(1)
                    .Concat(lines.Skip(1).Where(l => l.Trim().Length > 0 && l.Split(',')[0].Trim().Trim('"') != file))
                    .ToList();
                File.WriteAllText(LicensesPath, string.Join("\n", lines) + "\n");
            }
            SoundEventSync.EnsureFolder(Removed);
            string error = AssetDatabase.MoveAsset(path, AssetDatabase.GenerateUniqueAssetPath($"{Removed}/{file}"));
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"Sound Import: couldn't move {path}: {error}");
        }

        AssetDatabase.Refresh();
        Reload();
        Repaint();
        Debug.Log($"Sound Import: {clip.name} taken off {e.name}" + (ours ? $" (file in {Removed})." : "."));
    }

    void DropEmpty(SoundEvent e)
    {
        if (e == null) return;
        e.clips = (e.clips ?? new AudioClip[0]).Where(c => c != null).ToArray();
        EditorUtility.SetDirty(e);
        AssetDatabase.SaveAssets();
        Repaint();
    }

    // where the clip lives decides its import settings (AudioImportRules)
    static string FolderFor(SoundEvent e)
    {
        string root = AudioImportRules.ClipsRoot;
        if (e.name.StartsWith("mus_")) return root + "Music";
        if (e.name.StartsWith("amb_")) return root + (e.loop ? "Ambience/Beds" : "Ambience");
        string section = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(e)));   // "03 Player"
        string name = Regex.Replace(section ?? "Other", @"^\d+\s+", "");
        if (name == "HUD and UI") return root + "UI";
        return root + (e.loop ? "SFX/Loops/" : "SFX/") + name;
    }

    static bool KeepsStereo(SoundEvent e) => e.name.StartsWith("mus_") || (e.name.StartsWith("amb_") && e.loop);

    // after the highest real <id>_NN (placeholders don't count: they're never linked automatically)
    static int NextIndex(SoundEvent e)
    {
        int max = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioImportRules.ClipsRoot.TrimEnd('/') }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.StartsWith(Placeholders + "/")) continue;
            Match m = ClipName.Match(Path.GetFileNameWithoutExtension(p));
            if (m.Success && m.Groups[1].Value == e.name) max = Mathf.Max(max, int.Parse(m.Groups[2].Value));
        }
        return max + 1;
    }

    static bool Process(AudioClip src, bool loop, bool stereo, bool crossfade, float highPassHz,
                        out float[] data, out int channels, out string why)
    {
        data = null;
        channels = 0;
        why = null;
        if (src.loadType == AudioClipLoadType.Streaming) { why = "streamed, so its samples can't be read (put it in _Incoming)"; return false; }
        if (src.loadState != AudioDataLoadState.Loaded) src.LoadAudioData();
        int ch = src.channels, frames = src.samples;
        var raw = new float[frames * ch];
        if (raw.Length == 0 || !src.GetData(raw, 0)) { why = $"can't read its samples ({src.loadType}); put it in _Incoming"; return false; }

        channels = stereo && ch >= 2 ? 2 : 1;
        var x = new float[frames * channels];
        for (int f = 0; f < frames; f++)
        {
            if (channels == 2) { x[2 * f] = raw[f * ch]; x[2 * f + 1] = raw[f * ch + 1]; }
            else
            {
                float s = 0f;
                for (int c = 0; c < ch; c++) s += raw[f * ch + c];
                x[f] = s / ch;
            }
        }
        if (highPassHz > 0f) HighPass(x, channels, src.frequency, highPassHz);

        float peak = 0f;
        foreach (float s in x) peak = Mathf.Max(peak, Mathf.Abs(s));
        if (peak < 1e-5f) { why = "silent"; return false; }
        float gain = AudioSystem.DbToLinear(PeakDb) / peak;
        for (int i = 0; i < x.Length; i++) x[i] *= gain;

        int rate = src.frequency;
        if (!loop) x = Trim(x, channels, rate);
        else if (crossfade) x = Crossfade(x, channels, Mathf.Min(rate / 2, frames / 10));
        data = x;
        return true;
    }

    // a 2nd-order Butterworth high-pass (12 dB/octave), per channel, in place: takes the
    // rumble out of small sounds so the low end stays free for the big ones
    static void HighPass(float[] x, int ch, int rate, float hz)
    {
        double w0 = 2.0 * System.Math.PI * Mathf.Min(hz, rate * 0.45f) / rate;
        double cos = System.Math.Cos(w0), alpha = System.Math.Sin(w0) / (2.0 * 0.70710678);
        double a0 = 1.0 + alpha;
        double b0 = (1.0 + cos) / 2.0 / a0, b1 = -(1.0 + cos) / a0, b2 = b0;
        double a1 = -2.0 * cos / a0, a2 = (1.0 - alpha) / a0;
        for (int c = 0; c < ch; c++)
        {
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = c; i < x.Length; i += ch)
            {
                double x0 = x[i];
                double y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x0; y2 = y1; y1 = y0;
                x[i] = (float)y0;
            }
        }
    }

    // from 1 ms before the first sound to 20 ms after the last, with a 0.5 ms fade in and a
    // 10 ms fade out, so neither cut clicks
    static float[] Trim(float[] x, int ch, int rate)
    {
        int frames = x.Length / ch;
        int first = 0;
        while (first < frames && Loud(x, ch, first) <= LeadGate) first++;
        int last = frames - 1;
        while (last > first && Loud(x, ch, last) <= TailGate) last--;
        first = Mathf.Max(0, first - rate / 1000);
        last = Mathf.Min(frames - 1, last + rate / 50);

        int n = last - first + 1;
        var y = new float[n * ch];
        System.Array.Copy(x, first * ch, y, 0, n * ch);
        int fadeIn = Mathf.Min(n, rate / 2000 + 1), fadeOut = Mathf.Min(n, rate / 100);
        for (int f = 0; f < n; f++)
        {
            float w = Mathf.Min(1f, f / (float)fadeIn, (n - 1 - f) / (float)fadeOut);
            for (int c = 0; c < ch; c++) y[f * ch + c] *= w;
        }
        return y;
    }

    static float Loud(float[] x, int ch, int frame)
    {
        float m = 0f;
        for (int c = 0; c < ch; c++) m = Mathf.Max(m, Mathf.Abs(x[frame * ch + c]));
        return m;
    }

    // the tail blended over the head (equal power, for noisy beds like fire and wind) and
    // then dropped, so the loop runs on from its last frame into its first
    static float[] Crossfade(float[] x, int ch, int fade)
    {
        int frames = x.Length / ch, n = frames - fade;
        if (fade < 1 || n <= fade) return x;
        var y = new float[n * ch];
        System.Array.Copy(x, y, n * ch);
        for (int f = 0; f < fade; f++)
        {
            float t = f / (float)fade * Mathf.PI * 0.5f;
            float a = Mathf.Sin(t), b = Mathf.Cos(t);
            for (int c = 0; c < ch; c++) y[f * ch + c] = x[f * ch + c] * a + x[(n + f) * ch + c] * b;
        }
        return y;
    }

    static void WriteWav(string path, float[] x, int ch, int rate)
    {
        using var w = new BinaryWriter(File.Create(path));
        int bytes = x.Length * 2;
        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + bytes);
        w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write((short)ch);
        w.Write(rate);
        w.Write(rate * ch * 2);
        w.Write((short)(ch * 2));
        w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(bytes);
        foreach (float s in x) w.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1f, 1f) * 32767f));
    }

    static string Csv(params string[] cells) =>
        string.Join(",", cells.Select(c =>
        {
            c = (c ?? "").Trim();
            return c.IndexOfAny(new[] { ',', '"' }) >= 0 ? "\"" + c.Replace("\"", "\"\"") + "\"" : c;
        }));
}

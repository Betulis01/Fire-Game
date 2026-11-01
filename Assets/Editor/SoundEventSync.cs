using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// sound.md is the catalogue of every sound; this keeps one SoundEvent asset per ID in it.
//
// Tools/Audio/Sync Events From sound.md reads every table row whose first cell is an
// ID (`sfx_...`, `amb_...`, `mus_...`). It creates the missing assets under
// Assets/Audio/Events/<NN Section>/ with defaults for their section (bus, space, range,
// limits, loop), and refreshes every asset's Catalogue fields (brief, priority,
// section, variants) from the row -- tuning someone did on an existing asset is never
// touched. Rows that only reuse another sound (Var or Type "—", or a Sound cell that
// points at another ID or says "Silent"/"Covered by") and mix rules (Type "mix") are
// skipped; IDs with a <placeholder> are templates and skipped too. Assets whose ID is no
// longer in sound.md are reported, never deleted. It also makes AudioConfig (in
// Resources) if it's missing, links the mixer once it exists, assigns a missing bus
// when the mixer has a group of that name, and fills the debug overlay's test list.
//
// Tools/Audio/Auto-Fill Clips assigns clips by name: every AudioClip under
// Assets/Audio/Clips named <id>_NN goes on the event <id>, in order -- except the generated
// stand-ins in Clips/_Placeholder, which are kept but never linked automatically. An event
// whose clips were picked by hand (any clip not named after it) is left alone. Sync runs it too.
public static class SoundEventSync
{
    const string CataloguePath = "sound.md";
    const string EventsRoot = "Assets/Audio/Events";
    const string ConfigPath = "Assets/Audio/Resources/AudioConfig.asset";
    const string MixerPath = "Assets/Audio/Mixer/FireGame.mixer";
    const string PlaceholderRoot = "Assets/Audio/Clips/_Placeholder";

    static readonly Regex IdCell = new(@"^`((?:sfx|amb|mus)_[a-z0-9_]+)`$");
    static readonly Regex Section = new(@"^##\s+(\d+)\.\s+(.+)$");
    static readonly Regex SubSection = new(@"^###\s+(\d+\.\d+)\s+(.+)$");
    static readonly Regex ClipName = new(@"^(.+)_(\d{2,3})$");

    static readonly string[] DebugTests =
        { "sfx_ui_click", "sfx_ui_denied", "sfx_ground_step_snow", "sfx_axe_swing", "sfx_campfire_loop_medium" };

    class Row
    {
        public string id, section, folder, brief, priority, type;
        public int sectionNumber, variants;
        public bool loop;
    }

    [MenuItem("Tools/Audio/Sync Events From sound.md")]
    static void Sync()
    {
        if (!File.Exists(CataloguePath)) { Debug.LogError($"{CataloguePath} not found at the project root."); return; }

        List<Row> rows = Parse(File.ReadAllLines(CataloguePath), out int skipped, out int templates);
        AudioConfig config = EnsureConfig();
        Dictionary<string, SoundEvent> existing = FindEvents();

        int created = 0, updated = 0;
        var ids = new HashSet<string>();
        foreach (Row row in rows)
        {
            if (!ids.Add(row.id)) { Debug.LogWarning($"sound.md lists {row.id} twice; using the first row."); continue; }

            if (!existing.TryGetValue(row.id, out SoundEvent e))
            {
                string folder = EnsureFolder($"{EventsRoot}/{row.folder}");
                e = ScriptableObject.CreateInstance<SoundEvent>();
                ApplyDefaults(e, row);
                AssetDatabase.CreateAsset(e, $"{folder}/{row.id}.asset");
                existing[row.id] = e;
                created++;
            }
            else updated++;

            e.brief = row.brief;
            e.catalogPriority = row.priority;
            e.catalogSection = row.section;
            e.catalogVariants = row.variants;
            if (e.output == null && config.mixer != null) e.output = Group(config.mixer, BusFor(row));
            EditorUtility.SetDirty(e);
        }

        if (config.debugTestEvents == null || config.debugTestEvents.Length == 0)
        {
            config.debugTestEvents = DebugTests.Where(existing.ContainsKey).Select(id => existing[id]).ToArray();
            EditorUtility.SetDirty(config);
        }

        var orphans = existing.Keys.Where(id => !ids.Contains(id) && !string.IsNullOrEmpty(existing[id].catalogSection)).OrderBy(id => id).ToList();
        int filled = AutoFill(existing.Values);
        AssetDatabase.SaveAssets();

        string report = $"sound.md: {rows.Count} sounds. Created {created}, refreshed {updated}. " +
                        $"Skipped {skipped} reuse/mix rows and {templates} templated IDs. Clips auto-filled on {filled} events." +
                        (config.mixer == null ? "\nNo mixer yet (Assets/Audio/Mixer/FireGame.mixer): buses stay on Master until it exists; run Sync again then." : "") +
                        (orphans.Count > 0 ? $"\n{orphans.Count} assets are no longer in sound.md: {string.Join(", ", orphans)}" : "");
        Debug.Log(report);
        EditorUtility.DisplayDialog("Sync Events From sound.md", report, "OK");
    }

    [MenuItem("Tools/Audio/Auto-Fill Clips")]
    static void AutoFillMenu()
    {
        int filled = AutoFill(FindEvents().Values);
        AssetDatabase.SaveAssets();
        Debug.Log($"Auto-Fill Clips: {filled} events updated.");
    }

    // ---------------------------------------------------------------- parsing

    static List<Row> Parse(string[] lines, out int skipped, out int templates)
    {
        var rows = new List<Row>();
        skipped = templates = 0;
        int sectionNumber = 0;
        string sectionTitle = "", folder = "", sub = "";
        string[] header = null;

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            Match m;
            if ((m = Section.Match(line)).Success)
            {
                sectionNumber = int.Parse(m.Groups[1].Value);
                sectionTitle = m.Groups[2].Value.Trim();
                folder = $"{sectionNumber:00} {FolderName(sectionTitle)}";
                sub = "";
                header = null;
                continue;
            }
            if ((m = SubSection.Match(line)).Success) { sub = $"{m.Groups[1].Value} {m.Groups[2].Value.Trim()}"; header = null; continue; }
            if (!line.StartsWith("|")) { header = null; continue; }

            string[] cells = Cells(line);
            if (cells.All(c => Regex.IsMatch(c, @"^:?-{3,}:?$"))) continue;   // the |---| line
            if (header == null) { header = cells; continue; }

            string first = cells.Length > 0 ? cells[0] : "";
            if (first.Contains("<") && first.StartsWith("`")) { templates++; continue; }
            Match id = IdCell.Match(first);
            if (!id.Success) continue;

            string var = Cell(header, cells, "Var");
            string type = Cell(header, cells, "Type");
            string sound = Cell(header, cells, "Sound") ?? Cell(header, cells, "Feel") ?? "";
            if (IsReuse(var, type, sound)) { skipped++; continue; }

            string when = Cell(header, cells, "When") ?? Cell(header, cells, "Used for") ?? Cell(header, cells, "Moment") ?? "";
            string who = Cell(header, cells, "Weapon (prefab)") ?? Cell(header, cells, "Skill");
            string t = (type ?? "").ToLowerInvariant();
            rows.Add(new Row
            {
                id = id.Groups[1].Value,
                sectionNumber = sectionNumber,
                section = string.IsNullOrEmpty(sub) ? $"{sectionNumber}. {sectionTitle}" : sub,
                folder = folder,
                brief = Plain((who != null ? who + " — " : "") + (when.Length > 0 ? "When: " + when + "\n" : "") + sound),
                priority = Cell(header, cells, "P") ?? "",
                type = type,
                variants = Variants(var, type),
                loop = t.StartsWith("loop") || t.StartsWith("layer"),
            });
        }
        return rows;
    }

    static bool IsReuse(string var, string type, string sound)
    {
        if (var == "—" || type == "—") return true;
        if (type != null && type.Trim().ToLowerInvariant() == "mix") return true;
        string s = sound.Trim();
        return s.StartsWith("`") || s.StartsWith("Covered by") || s.StartsWith("**Silent") || s.StartsWith("Silent")
            || s.StartsWith("—") || s == "";
    }

    static string[] Cells(string line)
    {
        string inner = line.Trim();
        if (inner.StartsWith("|")) inner = inner.Substring(1);
        if (inner.EndsWith("|")) inner = inner.Substring(0, inner.Length - 1);
        return inner.Split('|').Select(c => c.Trim()).ToArray();
    }

    static string Cell(string[] header, string[] cells, string column)
    {
        int i = System.Array.IndexOf(header, column);
        return i >= 0 && i < cells.Length ? cells[i] : null;
    }

    static int Variants(string var, string type)
    {
        Match m = Regex.Match(var ?? "", @"\d+");
        if (m.Success) return int.Parse(m.Value);
        m = Regex.Match(type ?? "", @"\((\d+)\s*var\)");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    // "World: ambience, time and music" -> "World"; "Weapons and tools (the swing)" -> "Weapons and tools"
    static string FolderName(string title)
    {
        string t = title.Split(':', '(')[0].Trim();
        foreach (char c in Path.GetInvalidFileNameChars()) t = t.Replace(c.ToString(), "");
        return t;
    }

    static string Plain(string md) => Regex.Replace(md, @"\*\*|`|<br>", "").Replace("  ", " ").Trim();

    // ---------------------------------------------------------------- defaults

    static string BusFor(Row row)
    {
        string id = row.id;
        if (id.StartsWith("mus_")) return "Music";
        if (id.StartsWith("amb_")) return "Ambience";
        if (id.StartsWith("sfx_time_") || id.Contains("levelup")) return "Stingers";
        return row.sectionNumber switch
        {
            3 => "Player",
            4 or 5 => "Combat",
            6 => "Creatures",
            10 => "UI",
            _ => "World",
        };
    }

    // Only on creation: an existing asset's tuning is never overwritten.
    static void ApplyDefaults(SoundEvent e, Row row)
    {
        string id = row.id;
        string bus = BusFor(row);
        e.loop = row.loop;
        e.space = bus is "UI" or "Music" or "Ambience" or "Stingers" ? SoundEvent.SoundSpace.Screen : SoundEvent.SoundSpace.World;
        e.ignorePause = bus is "UI" or "Music" or "Ambience";

        if (bus == "UI") { e.cooldown = 0.03f; e.maxVoices = 3; e.pitchJitterSemitones = 0.4f; e.volumeJitterDb = 0.8f; }
        if (bus == "Music") { e.cooldown = 0f; e.maxVoices = 2; e.pitchJitterSemitones = 0f; e.volumeJitterDb = 0f; e.priority = SoundEvent.SoundPriority.Critical; e.loop = !id.Contains("game_over"); }
        if (bus == "Ambience" && e.loop) { e.pitchJitterSemitones = 0f; e.volumeJitterDb = 0f; }
        if (id == "amb_forest_creak") { e.space = SoundEvent.SoundSpace.World; e.ignorePause = false; e.rangeScale = 1.2f; }
        if (bus == "Stingers") { e.pitchJitterSemitones = 0f; e.volumeJitterDb = 0f; e.priority = SoundEvent.SoundPriority.High; }

        // things the player hears as status, not as a place: heartbeat, freezing, level-up
        if (id.Contains("lowhealth") || id.Contains("freezing_loop") || id.Contains("hold_equip")) e.space = SoundEvent.SoundSpace.Screen;

        if (id.StartsWith("sfx_ground_step_") || id.StartsWith("sfx_foot_") || id == "sfx_player_carry_layer")
        { e.rangeScale = 0.6f; e.maxVoices = 6; e.cooldown = 0.02f; e.priority = SoundEvent.SoundPriority.Low; e.volumeJitterDb = 2f; }
        else if (id.EndsWith("_idle")) { e.priority = SoundEvent.SoundPriority.Low; e.maxVoices = 2; e.cooldown = 0.5f; }
        else if (id.EndsWith("_spawn")) { e.rangeScale = 1.8f; e.priority = SoundEvent.SoundPriority.High; }
        else if (id.EndsWith("_aggro") || id.EndsWith("_howl_distant")) { e.rangeScale = 1.3f; e.priority = SoundEvent.SoundPriority.High; }
        else if (id.EndsWith("_hurt") || id.EndsWith("_death") || id.EndsWith("_charge_ready") || id.EndsWith("_crit"))
            e.priority = SoundEvent.SoundPriority.High;
        else if (id == "sfx_tree_fell" || id.StartsWith("sfx_ore_break")) { e.rangeScale = 1.6f; e.priority = SoundEvent.SoundPriority.High; }

        if (e.loop) { e.cooldown = 0f; e.maxVoices = 8; }
        if (id.StartsWith("sfx_campfire_loop_")) e.priority = SoundEvent.SoundPriority.High;
        if (id is "sfx_player_death" or "sfx_player_levelup") e.priority = SoundEvent.SoundPriority.Critical;
    }

    // ---------------------------------------------------------------- assets

    static Dictionary<string, SoundEvent> FindEvents()
    {
        var map = new Dictionary<string, SoundEvent>();
        if (!AssetDatabase.IsValidFolder(EventsRoot)) return map;
        foreach (string guid in AssetDatabase.FindAssets("t:SoundEvent", new[] { EventsRoot }))
        {
            var e = AssetDatabase.LoadAssetAtPath<SoundEvent>(AssetDatabase.GUIDToAssetPath(guid));
            if (e != null && !map.ContainsKey(e.name)) map[e.name] = e;
        }
        return map;
    }

    static AudioConfig EnsureConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
        if (config == null)
        {
            EnsureFolder(Path.GetDirectoryName(ConfigPath).Replace('\\', '/'));
            config = ScriptableObject.CreateInstance<AudioConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }
        if (config.mixer == null)
        {
            config.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (config.mixer != null) EditorUtility.SetDirty(config);
        }
        return config;
    }

    static AudioMixerGroup Group(AudioMixer mixer, string name)
    {
        foreach (AudioMixerGroup g in mixer.FindMatchingGroups(name))
            if (g.name == name) return g;
        return null;
    }

    internal static string EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return path;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        return path;
    }

    static int AutoFill(IEnumerable<SoundEvent> events)
    {
        var byId = new Dictionary<string, List<AudioClip>>();
        if (AssetDatabase.IsValidFolder("Assets/Audio/Clips"))
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Clips" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(PlaceholderRoot + "/")) continue;   // stand-ins are only ever linked by hand
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Match m = clip != null ? ClipName.Match(clip.name) : Match.Empty;
                if (!m.Success) continue;
                if (!byId.TryGetValue(m.Groups[1].Value, out var list)) byId[m.Groups[1].Value] = list = new List<AudioClip>();
                list.Add(clip);
            }

        int filled = 0;
        foreach (SoundEvent e in events)
        {
            if (!byId.TryGetValue(e.name, out var found)) continue;
            AudioClip[] current = e.clips ?? new AudioClip[0];
            bool handPicked = current.Any(c => c != null && !(ClipName.Match(c.name) is { Success: true } m && m.Groups[1].Value == e.name));
            if (handPicked) continue;

            AudioClip[] next = found.OrderBy(c => c.name).ToArray();
            if (current.Length == next.Length && current.SequenceEqual(next)) continue;
            e.clips = next;
            EditorUtility.SetDirty(e);
            filled++;
        }
        return filled;
    }
}

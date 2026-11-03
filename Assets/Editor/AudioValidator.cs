using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Tools/Audio/Validate: what's missing or wrong in the audio layer, as one console report.
//   - AudioConfig and the mixer: linked, its exposed volumes, snapshots and buses present,
//     and its shared sets (ground, crit, world, UI kit) set
//   - coverage per sound.md section, and the events still without clips, P1 first
//   - events with empty clip slots, no bus (once the mixer exists), a loop flag on a
//     one-shot clip or vice versa
//   - the mix: events with no tier, louder than their tier allows, or a Danger sound
//     below High priority (MixTiers)
//   - clips: leading silence over 10 ms (one-shots), a loop whose ends don't meet (click),
//     clips no event uses (stand-ins in Clips/_Placeholder may sit unused), clips with no row in Assets/Audio/LICENSES.csv or with an
//     unknown licence there
//   - prefabs: a Hurtbox without SurfaceAudio (silent when hit), an Animator whose clips
//     fire OnFootstep without a FootstepAudio beside it (Unity's "no receiver" error), a
//     fire (Fuel + FireRender) without FireAudio
//   - items: an ItemDefinition with no ItemSounds (it falls back to WorldSounds.fallback)
// Later phases add their checks here.
public static class AudioValidator
{
    const string EventsRoot = "Assets/Audio/Events";
    const string ClipsRoot = "Assets/Audio/Clips";
    const string LicensesPath = "Assets/Audio/LICENSES.csv";
    static readonly string[] Buses = { "Music", "Ambience", "SFX", "Player", "Combat", "Creatures", "World", "Stingers", "UI" };

    [MenuItem("Tools/Audio/Validate")]
    static void Validate()
    {
        var sb = new StringBuilder();
        int problems = 0;
        void Problem(string s) { sb.AppendLine("  • " + s); problems++; }

        // --- config and mixer
        sb.AppendLine("<b>Config and mixer</b>");
        var config = AssetDatabase.LoadAssetAtPath<AudioConfig>("Assets/Audio/Resources/AudioConfig.asset");
        AudioMixer mixer = config != null ? config.mixer : null;
        if (config == null) Problem("No Assets/Audio/Resources/AudioConfig.asset (run Tools/Audio/Sync Events From sound.md).");
        else if (mixer == null) Problem("AudioConfig has no mixer (build Assets/Audio/FireGame.mixer, then Sync again).");
        if (config != null)
        {
            const string fix = " (run Tools/Audio/Set Up Audio On Prefabs).";
            if (config.groundSounds == null) Problem("AudioConfig has no Ground Sounds: no footsteps or landings" + fix);
            if (config.critLayer == null) Problem("AudioConfig has no Crit Layer" + fix);
            if (config.world == null) Problem("AudioConfig has no World Sounds: no item-handling or building layers" + fix);
            if (config.ui == null) Problem("AudioConfig has no UI Sound Kit: every UI sound is silent" + fix);
            if (config.ambience == null) Problem("AudioConfig has no Ambience Sounds: no wind, scattered ambience or stingers" + fix);
            if (config.music == null) Problem("AudioConfig has no Music Sounds: no music" + fix);
            if (mixer != null)
                foreach (string p in new[] { config.heatCutoffParam, config.freezeCutoffParam, config.lowHealthCutoffParam })
                    if (!mixer.GetFloat(p, out _)) Problem($"Mixer has no exposed parameter '{p}' (the Phase 6 mixer steps in SoundSystemPlan.md).");
        }
        if (config != null && mixer != null)
        {
            for (int i = 0; i < 5; i++)
                if (AudioSettingsStore.OnMixer((AudioChannel)i) && !mixer.GetFloat(AudioSettingsStore.Parameter((AudioChannel)i), out _))
                    Problem($"Mixer has no exposed parameter '{AudioSettingsStore.Parameter((AudioChannel)i)}'.");
            foreach (string s in new[] { config.gameplaySnapshot, config.pausedSnapshot, config.menuSnapshot, config.endSnapshot })
                if (mixer.FindSnapshot(s) == null) Problem($"Mixer has no snapshot '{s}'.");
            foreach (string b in Buses)
                if (!mixer.FindMatchingGroups(b).Any(g => g.name == b)) Problem($"Mixer has no group '{b}'.");
        }

        // --- events
        List<SoundEvent> events = AssetDatabase.FindAssets("t:SoundEvent", new[] { EventsRoot })
            .Select(g => AssetDatabase.LoadAssetAtPath<SoundEvent>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(e => e != null).OrderBy(e => e.name).ToList();

        sb.AppendLine($"\n<b>Coverage</b> ({events.Count} events)");
        foreach (var section in events.GroupBy(e => SectionKey(e)).OrderBy(g => g.Key))
        {
            int done = section.Count(e => e.HasClips);
            int p1 = section.Count(e => e.catalogPriority == "P1"), p1Done = section.Count(e => e.catalogPriority == "P1" && e.HasClips);
            sb.AppendLine($"  {section.Key,-28} {done,3}/{section.Count(),-3}  (P1 {p1Done}/{p1})");
        }

        sb.AppendLine("\n<b>Events without clips</b>");
        foreach (string p in new[] { "P1", "P2", "P3", "" })
        {
            var empty = events.Where(e => !e.HasClips && (e.catalogPriority ?? "") == p).Select(e => e.name).ToList();
            if (empty.Count > 0) sb.AppendLine($"  {(p == "" ? "untagged" : p)} ({empty.Count}): {string.Join(", ", empty)}");
        }

        sb.AppendLine("\n<b>Event problems</b>");
        var used = new HashSet<AudioClip>();
        foreach (SoundEvent e in events)
        {
            if (e.clips != null)
            {
                if (e.clips.Any(c => c == null)) Problem($"{e.name}: an empty clip slot.");
                foreach (AudioClip c in e.clips) if (c != null) used.Add(c);
            }
            if (mixer != null && e.output == null) Problem($"{e.name}: no mixer bus.");
            if (e.layers != null && e.layers.Contains(e)) Problem($"{e.name}: layers itself.");
        }

        // --- the mix: each event against its tier (MixTiers)
        sb.AppendLine("\n<b>Mix problems</b>");
        int unset = events.Count(e => e.tier == SoundEvent.MixTier.Unset);
        if (unset > 0) Problem($"{unset} event(s) have no mix tier (run Tools/Audio/Apply Mix Baseline).");
        foreach (SoundEvent e in events)
        {
            if (e.tier == SoundEvent.MixTier.Unset) continue;
            MixTiers.Level l = MixTiers.Of(e.tier);
            if (e.volumeDb > l.maxDb + 0.01f) Problem($"{e.name}: {e.volumeDb:0.#} dB is loud for a {e.tier} sound (at most {l.maxDb:0} dB).");
            if (e.tier == SoundEvent.MixTier.Danger && e.priority < SoundEvent.SoundPriority.High)
                Problem($"{e.name}: a Danger sound with {e.priority} priority (a crowd could steal it).");
        }

        // --- clips
        sb.AppendLine("\n<b>Clip problems</b>");
        var licensed = LicensedFiles(out HashSet<string> unlicensed);
        var allClips = AssetDatabase.IsValidFolder(ClipsRoot)
            ? AssetDatabase.FindAssets("t:AudioClip", new[] { ClipsRoot }).Select(AssetDatabase.GUIDToAssetPath).ToList()
            : new List<string>();
        foreach (string path in allClips)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) continue;
            if (!used.Contains(clip) && !path.StartsWith(ClipsRoot + "/_Placeholder/")) Problem($"{path}: not used by any event.");
            if (!licensed.Contains(Path.GetFileName(path))) Problem($"{path}: no row in LICENSES.csv.");
            else if (unlicensed.Contains(Path.GetFileName(path))) Problem($"{path}: licence unknown (replace it, or fill in its LICENSES.csv row).");
        }
        foreach (SoundEvent e in events)
        {
            if (e.clips == null) continue;
            foreach (AudioClip c in e.clips)
            {
                if (c == null || !SoundEventEditor.ClipStats(c, out _, out _, out float lead)) continue;
                if (!e.loop && lead > 0.01f) Problem($"{e.name} / {c.name}: {lead * 1000f:0} ms leading silence.");
                if (e.loop && !LoopMeets(c)) Problem($"{e.name} / {c.name}: the loop's ends don't meet (a click at the loop point).");
            }
        }

        // --- prefabs
        sb.AppendLine("\n<b>Prefab problems</b>");
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            if (prefab.GetComponentInChildren<Hurtbox>(true) != null && prefab.GetComponent<SurfaceAudio>() == null)
                Problem($"{path}: has a Hurtbox but no SurfaceAudio (silent when hit).");
            foreach (Animator animator in prefab.GetComponentsInChildren<Animator>(true))
            {
                RuntimeAnimatorController rac = animator.runtimeAnimatorController;
                if (rac == null || animator.GetComponent<FootstepAudio>() != null) continue;
                if (rac.animationClips.Any(c => c != null && AnimationUtility.GetAnimationEvents(c).Any(ev => ev.functionName == FootstepEvents.Function)))
                    Problem($"{path}: '{animator.name}' plays footstep events but has no FootstepAudio.");
            }
            // a fire the player tends (not a burning-effect VFX, which FlammableAudio voices)
            if (prefab.GetComponent<Fuel>() != null && prefab.GetComponent<FuelReceiver>() != null && prefab.GetComponent<FireAudio>() == null)
                Problem($"{path}: a fire without FireAudio.");
        }

        sb.AppendLine("\n<b>Item problems</b>");
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (def == null || def.sounds != null) continue;
            bool structure = def.prefab != null && def.prefab.GetComponent<Station>() != null;
            if (!structure) Problem($"{path}: no ItemSounds (falls back to WorldSounds.fallback).");
        }

        sb.Insert(0, $"Audio validation: {problems} problem(s).\n");
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Audio validation", $"{problems} problem(s), {events.Count} events, {used.Count} clips in use.\nFull report in the Console.", "OK");
    }

    // the event's folder under Events/ -- one per sound.md section ("03 Player")
    static string SectionKey(SoundEvent e) =>
        Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(e)));

    // every file with a row, and those whose licence cell is empty, "unknown" or "FILL IN"
    static HashSet<string> LicensedFiles(out HashSet<string> unknown)
    {
        var set = new HashSet<string>();
        unknown = new HashSet<string>();
        if (!File.Exists(LicensesPath)) return set;
        foreach (string line in File.ReadAllLines(LicensesPath).Skip(1))
        {
            List<string> cells = CsvCells(line);
            string file = Path.GetFileName(cells[0].Trim());
            if (file.Length == 0) continue;
            set.Add(file);
            string licence = cells.Count > 3 ? cells[3].Trim().ToLowerInvariant() : "";
            if (licence is "" or "unknown" or "fill in") unknown.Add(file);
        }
        return set;
    }

    static List<string> CsvCells(string line)
    {
        var cells = new List<string>();
        var cell = new System.Text.StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { cell.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else cell.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { cells.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(c);
        }
        cells.Add(cell.ToString());
        return cells;
    }

    // The last and first sample frames should be close, or the loop clicks.
    static bool LoopMeets(AudioClip c)
    {
        if (c.loadType == AudioClipLoadType.Streaming || c.samples < 2) return true;
        var head = new float[c.channels];
        var tail = new float[c.channels];
        if (!c.GetData(head, 0) || !c.GetData(tail, c.samples - 1)) return true;
        for (int i = 0; i < c.channels; i++) if (Mathf.Abs(head[i] - tail[i]) > 0.05f) return false;
        return true;
    }
}

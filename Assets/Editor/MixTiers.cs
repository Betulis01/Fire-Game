using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Tier = SoundEvent.MixTier;
using Priority = SoundEvent.SoundPriority;

// The mix hierarchy as data (AudioSystem.md, "The mix"): every SoundEvent sorted into a
// tier by what the player most needs to hear, and each tier's starting level, priority
// and duck. Danger at 0 dB is the anchor; everything else is cut relative to it.
//   Tools/Audio/Apply Mix Baseline   sorts every event still Unset into its tier, and gives
//                                    it the tier's level and priority unless its level was
//                                    already tuned (anything but 0 dB)
//   SoundEventSync                   does the same for each event it creates
//   AudioValidator                   flags events louder than their tier allows
//   Sound Import                     high-passes the tiers in HighPassed (no rumble in small sounds)
public static class MixTiers
{
    public readonly struct Level
    {
        public readonly float db, maxDb;          // the baseline, and the loudest the Validator lets pass
        public readonly Priority? priority;       // null = keep Sync's (Music, Stingers)
        public readonly bool duck;
        public Level(float db, float maxDb, Priority? priority, bool duck = false)
        { this.db = db; this.maxDb = maxDb; this.priority = priority; this.duck = duck; }
    }

    public static Level Of(Tier t) => t switch
    {
        Tier.Danger => new Level(0f, 0f, Priority.High, duck: true),
        Tier.Action => new Level(-2f, 0f, Priority.Normal),
        Tier.Fire => new Level(-3f, 0f, Priority.High),
        Tier.Big => new Level(0f, 0f, Priority.Normal),
        Tier.Creature => new Level(-6f, -2f, Priority.Normal),
        Tier.Foley => new Level(-8f, -4f, Priority.Low),
        Tier.Step => new Level(-14f, -8f, Priority.Low),
        Tier.UI => new Level(-8f, -3f, Priority.Normal),
        Tier.UISoft => new Level(-14f, -8f, Priority.Normal),
        _ => new Level(0f, 0f, null),   // Ambience, Music, Stinger: their bus trims set them
    };

    // small sounds whose low end only muddies the mix
    public static bool HighPassed(Tier t) => t is Tier.Step or Tier.Foley or Tier.UI or Tier.UISoft;

    // The tier an event belongs to, by its ID and sound.md section; the first rule that matches wins.
    public static Tier Classify(SoundEvent e)
    {
        string id = e.name;
        int section = Section(e);
        bool Has(params string[] parts) => parts.Any(id.Contains);

        if (id.StartsWith("mus_")) return Tier.Music;
        if (id.StartsWith("sfx_time_") || Has("levelup")) return Tier.Stinger;
        if (id.StartsWith("amb_")) return Tier.Ambience;
        if (Has("sfx_ui_hover", "slider_tick", "prompt_focus")) return Tier.UISoft;
        if (section == 10) return Tier.UI;
        if (Has("sfx_player_hurt", "player_death", "freeze_start", "freezing_loop", "lowhealth_loop", "_aggro",
                "skeleton_spawn", "attack_windup", "bite_windup", "campfire_low_warning", "campfire_out", "torch_burnout"))
            return Tier.Danger;
        if (Has("ground_step_", "sfx_foot_", "ground_land_", "lunge_step", "carry_layer")) return Tier.Step;
        if (Has("_fell", "_break", "log_split", "_death", "combat_kill", "build_complete")) return Tier.Big;
        if (Has("campfire_", "furnace_", "torch_flame_loop", "torch_low", "torch_relight", "torch_refuel", "heat_enter")) return Tier.Fire;
        if (Has("loadout_", "_draw", "_stow", "shield_raise", "shield_lower", "arrow_drop")) return Tier.Foley;
        if (section is 4 or 5 or 13
            || Has("_hit", "_chop", "_glance", "_hurt", "_attack", "wolf_bite", "knockback", "blood_splatter", "cold_tick"))
            return Tier.Action;
        if (section == 6) return Tier.Creature;
        return Tier.Foley;
    }

    // Sets the event's tier and duck; with `levels`, its tier's volume and priority too.
    public static void Apply(SoundEvent e, Tier t, bool levels)
    {
        Level l = Of(t);
        e.tier = t;
        e.duckOthers = l.duck;
        if (levels)
        {
            e.volumeDb = l.db;
            if (l.priority.HasValue) e.priority = l.priority.Value;
        }
        EditorUtility.SetDirty(e);
    }

    static int Section(SoundEvent e)
    {
        string s = e.catalogSection ?? "";
        int dot = s.IndexOf('.');
        string head = dot > 0 ? s.Substring(0, dot) : s.Split(' ')[0];
        if (int.TryParse(head, out int n)) return n;
        // no catalogue section: the event's folder ("10 HUD and UI")
        string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(e)));
        return int.TryParse(folder?.Split(' ')[0], out n) ? n : 0;
    }

    [MenuItem("Tools/Audio/Apply Mix Baseline")]
    static void ApplyBaseline()
    {
        var events = AssetDatabase.FindAssets("t:SoundEvent", new[] { "Assets/Audio/Events" })
            .Select(g => AssetDatabase.LoadAssetAtPath<SoundEvent>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(e => e != null).ToList();

        int sorted = 0, levelled = 0, kept = 0;
        var perTier = new System.Collections.Generic.SortedDictionary<Tier, int>();
        foreach (SoundEvent e in events.Where(e => e.tier == Tier.Unset))
        {
            Tier t = Classify(e);
            bool untuned = Mathf.Approximately(e.volumeDb, 0f);
            Apply(e, t, untuned);
            sorted++;
            if (untuned) levelled++; else kept++;
            perTier[t] = perTier.TryGetValue(t, out int n) ? n + 1 : 1;
        }
        AssetDatabase.SaveAssets();

        var sb = new StringBuilder($"Mix baseline: {sorted} event(s) sorted into tiers, {levelled} levelled, " +
                                   $"{kept} kept their hand-tuned level; {events.Count - sorted} already had a tier.");
        foreach (var kv in perTier) sb.Append($"\n  {kv.Key,-9} {kv.Value,3}   ({Of(kv.Key).db:0} dB)");
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Mix baseline", $"{sorted} event(s) sorted, {levelled} levelled, {kept} kept their level.\n" +
                                    "Per tier in the Console.", "OK");
    }
}

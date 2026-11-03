using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools/Audio/Set Up Audio On Prefabs: wires the audio reactors onto every prefab that
// makes a sound (SoundSystemPlan Phase 3), so nothing is hand-linked:
//   1. the sets in Assets/Audio/Sets -- ground, footstep profiles, surfaces, tool sounds,
//      creature voices -- each field linked to its sound.md event by ID
//   2. AudioConfig's ground sounds, crit layer, world, UI, ambience and music sets
//   3. the reactors on the prefabs (player, creatures, props, tools, arrow, skills,
//      stations, plants), each pointed at its set and events
//   4. every ItemDefinition's ItemSounds, by material; every recipe's craft sound; the
//      UI's sound kit
//   5. the footstep events on the run / walk clips (FootstepEvents)
// Idempotent and non-destructive: it adds what's missing and fills empty fields, never
// replacing a set or event someone chose by hand. Run Sync Events From sound.md first
// (the events must exist). EnemyArtSetup calls SetUpCreature on every rebuild.
public static class AudioPrefabSetup
{
    const string EventsRoot = "Assets/Audio/Events";
    const string SetsRoot = "Assets/Audio/Sets";
    const string ConfigPath = "Assets/Audio/Resources/AudioConfig.asset";
    const string Prefabs = "Assets/Prefabs";

    static Dictionary<string, SoundEvent> events;
    static int missingEvents;

    [MenuItem("Tools/Audio/Set Up Audio On Prefabs")]
    static void SetUpAll()
    {
        LoadEvents();
        if (events.Count == 0)
        {
            EditorUtility.DisplayDialog("Set Up Audio On Prefabs", "No SoundEvents found. Run Tools/Audio/Sync Events From sound.md first.", "OK");
            return;
        }
        missingEvents = 0;

        SetUpConfig();
        int prefabs = 0;

        prefabs += Edit("Entities/Player/Player", root =>
        {
            Fill(Add<WeaponAudio>(root), ("chargeReady", "sfx_swing_charge_ready"), ("glance", "sfx_swing_glance"),
                 ("draw", "sfx_loadout_draw"), ("stow", "sfx_loadout_stow"), ("loadoutSwap", "sfx_loadout_swap"),
                 ("castDenied", "sfx_skill_cast_denied"), ("skillReady", "sfx_skill_ready"));
            Fill(Add<PlayerAudio>(root), ("hurt", "sfx_player_hurt"), ("hurtHeavy", "sfx_player_hurt_heavy"),
                 ("hurtBurn", "sfx_player_hurt_burn"), ("death", "sfx_player_death"), ("heal", "sfx_player_heal"),
                 ("knockback", "sfx_player_knockback"), ("freezeStart", "sfx_player_freeze_start"),
                 ("freezingLoop", "sfx_player_freezing_loop"), ("coldTick", "sfx_player_cold_tick"),
                 ("warmRelief", "sfx_player_warm_relief"), ("heartbeatLoop", "sfx_player_lowhealth_loop"),
                 ("xpGain", "sfx_player_xp_gain"), ("levelUp", "sfx_player_levelup"), ("weaponLevelUp", "sfx_weapon_levelup"),
                 ("killConfirm", "sfx_combat_kill"), ("buffStart", "sfx_player_buff_start"), ("buffEnd", "sfx_player_buff_end"));
            SetRef(Add<FootstepAudio>(root), "profile", Footsteps("Player"), false);
            SetRef(Add<SurfaceAudio>(root), "surface", Surface("Player"), false);
            Fill(Add<InteractionAudio>(root), ("holdFill", "sfx_player_hold_equip_fill"), ("fumble", "sfx_player_fumble"),
                 ("throwAim", "sfx_player_throw_aim"));
            Add<CraftAudio>(root);
        });

        foreach (string kind in new[] { "Enemy", "Wolf", "Boar", "Deer", "Polar Bear" })
            prefabs += Edit($"Entities/Enemy/{kind}", root => SetUpCreature(root, kind, overwrite: false));

        prefabs += Edit("Entities/Enemy/TargetDummy", root =>
        {
            SetRef(Add<SurfaceAudio>(root), "surface", Surface("Straw"), false);
            SetUpFlammable(root);
            Fill(Add<DestructibleAudio>(root), ("shake", "sfx_dummy_hit"), ("destroyed", "sfx_dummy_break"));
        });
        prefabs += Edit("Entities/Enemy/Polar Bear", root => Add<LootAudio>(root));

        foreach ((string prefab, string surface) in new[]
                 {
                     ("Scots Pine", "Wood"), ("Grey Alder", "Wood"),
                     ("Scots Pine Wood", "WoodLog"), ("Grey Alder Wood", "WoodLog"),
                     ("Stone Ore", "Stone"), ("Stone", "Rock"), ("Iron Ore", "IronOre"),
                 })
            prefabs += Edit($"Entities/Ground/{prefab}", root =>
            {
                SurfaceAudio a = Add<SurfaceAudio>(root);
                // the big stone shared the ore's set until it got its own
                bool replace = surface == "Rock" && a.surface != null && a.surface.name == "Surface_Stone";
                SetRef(a, "surface", Surface(surface), replace);
            });

        foreach ((string prefab, string shake, string creak, string destroyed, string bump) in new (string, string, string, string, string)[]
                 {
                     ("Scots Pine", "sfx_tree_shake", "sfx_tree_creak", "sfx_tree_fell", null),
                     ("Grey Alder", "sfx_tree_shake", "sfx_tree_creak", "sfx_tree_fell", null),
                     ("Stone Ore", null, null, "sfx_ore_break_stone", null),
                     ("Iron Ore", null, null, "sfx_ore_break_iron", null),
                     ("Scots Pine Wood", null, null, "sfx_log_split", "sfx_log_bump"),
                     ("Grey Alder Wood", null, null, "sfx_log_split", "sfx_log_bump"),
                     ("Stone", null, null, "sfx_stone_break", "sfx_stone_bump"),
                 })
            prefabs += Edit($"Entities/Ground/{prefab}", root =>
            {
                var d = Add<DestructibleAudio>(root);
                if (shake != null) Fill(d, ("shake", shake));
                if (creak != null) Fill(d, ("creak", creak));
                Fill(d, ("destroyed", destroyed));
                if (root.GetComponent<DropOnHit>() != null || root.GetComponent<DropOnDeath>() != null) Add<LootAudio>(root);
                if (bump != null) SetUpCollide(root, bump);
            });

        prefabs += Edit("Entities/Plants/Red Berries/Blue Berries", root => SetUpCollide(root, "sfx_bush_rustle"));
        prefabs += Edit("Entities/Plants/Hemp Plant", root =>
        {
            SetUpCollide(root, "sfx_hemp_rustle");
            Fill(Add<HarvestAudio>(root), ("harvested", "sfx_hemp_harvest"));
        });

        // stations
        prefabs += Edit("Stations/Campfire", root =>
        {
            SetRef(Add<FireAudio>(root), "sounds", Fire("Campfire"), false);
            Fill(Add<ProcessorAudio>(root), ("workingLoop", "sfx_campfire_cook_loop"), ("done", "sfx_campfire_cook_done"));
            Fill(Add<StructureAudio>(root), ("buildComplete", "sfx_campfire_build_complete"));
        });
        prefabs += Edit("Stations/Furnace", root =>
        {
            SetRef(Add<FireAudio>(root), "sounds", Fire("Furnace"), false);
            Fill(Add<ProcessorAudio>(root), ("workingLoop", "sfx_furnace_smelt_loop"), ("done", "sfx_furnace_bar_done"),
                 ("stalled", "sfx_furnace_output_full"), ("input", "sfx_furnace_input"));
            Fill(Add<StructureAudio>(root), ("buildComplete", "sfx_furnace_build_complete"));
        });
        prefabs += Edit("Stations/Workbench", root =>
            Fill(Add<StructureAudio>(root), ("buildComplete", "sfx_workbench_build_complete"), ("open", "sfx_workbench_open"),
                 ("close", "sfx_workbench_close")));

        foreach ((string prefab, string tool) in new[]
                 {
                     ("Fists", "Fists"), ("Stone Axe", "Axe"), ("Stone Pickaxe", "Pickaxe"), ("Iron Sword", "Sword"),
                     ("Torch", "Torch"), ("Sap Torch", "Torch"), ("Bow", "Bow"), ("Shield", "Shield"),
                 })
            prefabs += Edit($"Tools/{prefab}", root =>
            {
                SetRef(Add<ToolAudio>(root), "sounds", Tool(tool), false);
                if (tool == "Torch" && root.GetComponent<Fuel>() != null) SetRef(Add<FireAudio>(root), "sounds", Fire("Torch"), false);
            });

        prefabs += Edit("Tools/Arrow", root =>
            Fill(Add<ProjectileAudio>(root), ("flightLoop", "sfx_arrow_flight"), ("drop", "sfx_arrow_drop")));

        prefabs += Edit("Skills/Whirlwind", root =>
            Fill(Add<WhirlwindAudio>(root), ("start", "sfx_whirlwind_start"), ("spin", "sfx_whirlwind_spin"), ("end", "sfx_whirlwind_end")));

        prefabs += Edit("Skills/Axe Throw", root =>
            Fill(Add<AxeThrowAudio>(root), ("windup", "sfx_axethrow_windup"), ("release", "sfx_axethrow_release"),
                 ("spinLoop", "sfx_axethrow_spin_loop"), ("stick", "sfx_axethrow_stick"), ("rip", "sfx_axethrow_rip"),
                 ("missTurn", "sfx_axethrow_miss_turn"), ("catchSlap", "sfx_axethrow_catch")));

        int items = SetUpItems();
        SetUpRecipes();
        FootstepEvents.BakeAll();
        AssetDatabase.SaveAssets();

        string report = $"Audio set up on {prefabs} prefabs and {items} items." +
                        (missingEvents > 0 ? $" {missingEvents} event IDs weren't found (see the Console); run Sync Events From sound.md." : "");
        Debug.Log("[AudioPrefabSetup] " + report);
        EditorUtility.DisplayDialog("Set Up Audio On Prefabs", report, "OK");
    }

    // ---------------------------------------------------------------- creatures

    static readonly Dictionary<string, (string voice, string footsteps, string surface)> Creatures = new()
    {
        ["Enemy"] = ("Skeleton", "Skeleton", "Bone"),
        ["Wolf"] = ("Wolf", "Wolf", "Flesh"),
        ["Boar"] = ("Boar", "Boar", "Flesh"),
        ["Deer"] = ("Deer", "Deer", "Flesh"),
        ["Polar Bear"] = ("PolarBear", "PolarBear", "Flesh"),
    };

    // A creature's voice, feet, flesh and burning. `overwrite`: point them at this kind's
    // sets even if set (a new prefab copied from the skeleton carries the skeleton's).
    public static void SetUpCreature(GameObject root, string kind, bool overwrite)
    {
        if (!Creatures.TryGetValue(kind, out var sets)) return;
        if (events == null) LoadEvents();
        SetRef(Add<CreatureAudio>(root), "voice", Voice(sets.voice), overwrite);
        SetRef(Add<FootstepAudio>(root), "profile", Footsteps(sets.footsteps), overwrite);
        SetRef(Add<SurfaceAudio>(root), "surface", Surface(sets.surface), overwrite);
        SetUpFlammable(root);
    }

    static void SetUpFlammable(GameObject root)
    {
        if (root.GetComponent<Flammable>() == null) return;
        Fill(Add<FlammableAudio>(root), ("ignite", "sfx_torch_ignite_target"), ("burningLoop", "sfx_burning_loop"),
             ("tick", "sfx_burning_tick"), ("extinguish", "sfx_burning_extinguish"));
    }

    // ---------------------------------------------------------------- sets

    static CreatureVoice Voice(string name) => name switch
    {
        "Skeleton" => VoiceSet(name, new Vector2(4f, 10f), ("spawn", "sfx_skeleton_spawn"), ("idle", "sfx_skeleton_idle"),
                               ("aggro", "sfx_skeleton_aggro"), ("attackWindup", "sfx_skeleton_attack_windup"),
                               ("attackSwing", "sfx_skeleton_attack_swing"), ("hurt", "sfx_skeleton_hurt"),
                               ("death", "sfx_skeleton_death"), ("despawn", "sfx_skeleton_despawn"), ("knockback", "sfx_creature_knockback")),
        "Wolf" => VoiceSet(name, new Vector2(5f, 12f), ("idle", "sfx_wolf_idle"), ("aggro", "sfx_wolf_aggro"),
                           ("chaseLoop", "sfx_wolf_chase_loop"), ("attackWindup", "sfx_wolf_bite_windup"), ("attackHit", "sfx_wolf_bite"),
                           ("hurt", "sfx_wolf_hurt"), ("death", "sfx_wolf_death"), ("knockback", "sfx_creature_knockback")),
        "Boar" => VoiceSet(name, new Vector2(4f, 10f), ("idle", "sfx_boar_idle"), ("hurt", "sfx_boar_hurt"), ("flee", "sfx_boar_flee"),
                           ("fleeLoop", "sfx_boar_flee_loop"), ("death", "sfx_boar_death"), ("knockback", "sfx_creature_knockback")),
        "Deer" => VoiceSet(name, new Vector2(5f, 14f), ("idle", "sfx_deer_idle"), ("hurt", "sfx_deer_hurt"), ("flee", "sfx_deer_flee"),
                           ("death", "sfx_deer_death"), ("knockback", "sfx_creature_knockback")),
        _ => VoiceSet(name, new Vector2(6f, 14f), ("idle", "sfx_bear_idle"), ("aggro", "sfx_bear_aggro"), ("attackSwing", "sfx_bear_attack"),
                      ("hurt", "sfx_bear_hurt"), ("death", "sfx_bear_death"), ("knockback", "sfx_creature_knockback")),
    };

    static CreatureVoice VoiceSet(string name, Vector2 idleInterval, params (string field, string id)[] fields)
    {
        bool created = Asset(out CreatureVoice v, $"Voice_{name}");
        if (created) v.idleInterval = idleInterval;
        Fill(v, fields);
        return v;
    }

    static FootstepProfile Footsteps(string name)
    {
        (string foot, float ground, float stride) = name switch
        {
            "Player" => ((string)null, 1f, 0.55f),
            "Skeleton" => ("sfx_foot_bone", 0.7f, 0.45f),
            "Wolf" => ("sfx_foot_paw", 0.45f, 0.5f),
            "Boar" => ("sfx_foot_hoof", 0.7f, 0.4f),
            "Deer" => ("sfx_foot_hoof_light", 0.55f, 0.6f),
            _ => ("sfx_foot_heavypaw", 1f, 0.8f),
        };
        if (Asset(out FootstepProfile p, $"Footsteps_{name}"))
        {
            p.groundVolume = ground;
            p.strideLength = stride;
        }
        if (foot != null) Fill(p, ("foot", foot));
        if (name == "Player") Fill(p, ("carryLayer", "sfx_player_carry_layer"), ("iceSlide", "sfx_player_ice_slide"));
        return p;
    }

    static SurfaceSounds Surface(string name)
    {
        Asset(out SurfaceSounds s, $"Surface_{name}");
        switch (name)
        {
            case "Flesh":   // every furred creature (wolf, boar, deer, polar bear) shares this set
                Fill(s, ("impact", "sfx_surface_flesh"));
                ByTool(s, (ToolKind.Axe, "sfx_surface_flesh_axe"), (ToolKind.Pickaxe, "sfx_surface_flesh_pickaxe"),
                       (ToolKind.Fist, "sfx_surface_flesh_fist"), (ToolKind.Sword, "sfx_surface_flesh_sword"),
                       (ToolKind.Torch, "sfx_surface_flesh_torch"));
                break;
            case "Bone": Fill(s, ("impact", "sfx_surface_bone")); break;
            case "Player": Fill(s, ("impact", "sfx_surface_player")); break;
            case "Wood":
                Fill(s, ("impact", "sfx_tree_chop"), ("glance", "sfx_tree_glance"));
                ByTool(s, (ToolKind.Pickaxe, "sfx_tree_hit_pickaxe"), (ToolKind.Fist, "sfx_tree_hit_fist"),
                       (ToolKind.Sword, "sfx_tree_hit_sword"), (ToolKind.Torch, "sfx_tree_hit_torch"));
                break;
            case "WoodLog":
                Fill(s, ("impact", "sfx_surface_wood_log"), ("glance", "sfx_tree_glance"));
                ByTool(s, (ToolKind.Pickaxe, "sfx_log_hit_pickaxe"), (ToolKind.Fist, "sfx_log_hit_fist"),
                       (ToolKind.Sword, "sfx_log_hit_sword"), (ToolKind.Torch, "sfx_log_hit_torch"));
                break;
            case "Rock":   // the big stone (an item you mine for rocks), not the ore
                Fill(s, ("impact", "sfx_ore_stone_hit"), ("glance", "sfx_ore_glance"));
                ByTool(s, (ToolKind.Axe, "sfx_stone_hit_axe"), (ToolKind.Fist, "sfx_stone_hit_fist"),
                       (ToolKind.Sword, "sfx_stone_hit_sword"), (ToolKind.Torch, "sfx_stone_hit_torch"));
                break;
            case "Stone":
            case "IronOre":
                Fill(s, ("impact", name == "Stone" ? "sfx_ore_stone_hit" : "sfx_ore_iron_hit"), ("glance", "sfx_ore_glance"));
                ByTool(s, (ToolKind.Axe, "sfx_ore_hit_axe"), (ToolKind.Fist, "sfx_ore_hit_fist"),
                       (ToolKind.Sword, "sfx_ore_hit_sword"), (ToolKind.Torch, "sfx_ore_hit_torch"));
                break;
            case "Straw": Fill(s, ("impact", "sfx_surface_straw")); break;
        }
        return s;
    }

    static ToolSounds Tool(string name)
    {
        Asset(out ToolSounds t, $"Tool_{name}");
        switch (name)
        {
            case "Fists": Fill(t, ("swing", "sfx_fists_swing"), ("swingHeavy", "sfx_fists_swing_heavy"), ("chargeLoop", "sfx_swing_charge_loop")); break;
            case "Axe": Fill(t, ("swing", "sfx_axe_swing"), ("swingHeavy", "sfx_axe_swing_heavy"), ("chargeLoop", "sfx_swing_charge_loop")); break;
            case "Pickaxe": Fill(t, ("swing", "sfx_pickaxe_swing"), ("swingHeavy", "sfx_pickaxe_swing_heavy"), ("chargeLoop", "sfx_swing_charge_loop")); break;
            case "Sword": Fill(t, ("swing", "sfx_sword_swing"), ("swingHeavy", "sfx_sword_swing_heavy"), ("chargeLoop", "sfx_swing_charge_loop")); break;
            case "Torch": Fill(t, ("swing", "sfx_torch_swing"), ("chargeLoop", "sfx_swing_charge_loop"),
                               ("draw", "sfx_torch_draw"), ("stow", "sfx_torch_stow")); break;
            case "Bow": Fill(t, ("chargeLoop", "sfx_bow_draw"), ("chargeReady", "sfx_bow_ready"), ("fire", "sfx_bow_release")); break;
            case "Shield": Fill(t, ("raise", "sfx_shield_raise"), ("lower", "sfx_shield_lower"), ("block", "sfx_shield_block")); break;
        }
        return t;
    }

    static void SetUpConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
        if (config == null) { Debug.LogWarning("[AudioPrefabSetup] No AudioConfig; run Sync Events From sound.md."); return; }

        Asset(out GroundSounds g, "GroundSounds");
        var wanted = new[]
        {
            (GroundType.Snow, "sfx_ground_step_snow", "sfx_ground_land_snow"),
            (GroundType.Grass, "sfx_ground_step_grass", "sfx_ground_land_grass"),
            (GroundType.Ice, "sfx_ground_step_ice", "sfx_ground_land_ice"),
            (GroundType.Stone, "sfx_ground_step_stone", null),
            (GroundType.Dirt, "sfx_ground_step_dirt", null),
        };
        var entries = g.grounds.ToList();
        foreach ((GroundType type, string step, string land) in wanted)
        {
            int i = entries.FindIndex(e => e.ground == type);
            GroundSounds.Entry e = i >= 0 ? entries[i] : new GroundSounds.Entry { ground = type };
            if (e.step == null) e.step = Event(step);
            if (e.land == null && land != null) e.land = Event(land);
            if (i >= 0) entries[i] = e; else entries.Add(e);
        }
        g.grounds = entries.ToArray();
        EditorUtility.SetDirty(g);

        if (config.groundSounds == null) config.groundSounds = g;
        if (config.critLayer == null) config.critLayer = Event("sfx_combat_crit");

        Asset(out WorldSounds w, "WorldSounds");
        Fill(w, ("pocket", "sfx_player_pickup_small"), ("autoPickup", "sfx_player_autopickup"), ("liftHeavy", "sfx_player_lift_heavy"),
             ("thrown", "sfx_player_throw"), ("spill", "sfx_player_bag_spill"), ("bagSwap", "sfx_bag_swap"),
             ("lootPop", "sfx_item_loot_pop"), ("ghostBegin", "sfx_build_ghost_begin"), ("ghostPlace", "sfx_build_ghost_place"),
             ("ghostCancel", "sfx_build_ghost_cancel"), ("ingredientDone", "sfx_build_ingredient_done"),
             ("buildComplete", "sfx_build_complete"));
        if (config.world == null) config.world = w;

        Asset(out UiSoundKit kit, "UiSoundKit");
        var roles = new (UiSound role, string id)[]
        {
            (UiSound.Click, "sfx_ui_click"), (UiSound.Hover, "sfx_ui_hover"), (UiSound.Denied, "sfx_ui_denied"),
            (UiSound.Pickup, "sfx_ui_pickup"), (UiSound.Place, "sfx_ui_place"), (UiSound.Swap, "sfx_ui_swap"),
            (UiSound.Return, "sfx_ui_return"), (UiSound.Toss, "sfx_ui_toss"), (UiSound.Open, "sfx_ui_open"),
            (UiSound.Close, "sfx_ui_close"), (UiSound.Tab, "sfx_menu_tab"), (UiSound.Subtab, "sfx_menu_subtab"),
            (UiSound.SliderTick, "sfx_ui_slider_tick"), (UiSound.SpellAssign, "sfx_actionbar_spell_assign"),
            (UiSound.SkillBuy, "sfx_skill_node_buy"), (UiSound.SkillMaxed, "sfx_skill_node_maxed"),
            (UiSound.SpellLearn, "sfx_skill_spell_learn"), (UiSound.MainPlay, "sfx_menu_main_play"),
        };
        var kitEntries = kit.entries.ToList();
        foreach ((UiSound role, string id) in roles)
        {
            int i = kitEntries.FindIndex(e => e.role == role);
            if (i < 0) kitEntries.Add(new UiSoundKit.Entry { role = role, sound = Event(id) });
            else if (kitEntries[i].sound == null) kitEntries[i] = new UiSoundKit.Entry { role = role, sound = Event(id) };
        }
        kit.entries = kitEntries.ToArray();
        EditorUtility.SetDirty(kit);
        if (config.ui == null) config.ui = kit;

        if (Asset(out AmbienceSounds amb, "AmbienceSounds"))
        {
            amb.gust.interval = new Vector2(10f, 30f);
            amb.forestCreak.interval = new Vector2(8f, 20f);
            amb.dawnBirds.interval = new Vector2(6f, 15f);
            amb.owl.interval = new Vector2(20f, 45f);
            amb.howl.interval = new Vector2(25f, 60f);
        }
        var ambSo = new SerializedObject(amb);
        foreach ((string path, string id) in new[]
                 {
                     ("windDay", "amb_wind_day"), ("windNight", "amb_wind_night"), ("gust.sound", "amb_gust"),
                     ("forestCreak.sound", "amb_forest_creak"), ("dawnBirds.sound", "amb_dawn_birds"),
                     ("owl.sound", "amb_night_owl"), ("howl.sound", "sfx_wolf_howl_distant"),
                     ("duskSting", "sfx_time_dusk"), ("nightSting", "sfx_time_night"), ("dawnSting", "sfx_time_dawn"),
                 })
        {
            SerializedProperty p = ambSo.FindProperty(path);
            if (p != null && p.objectReferenceValue == null) p.objectReferenceValue = Event(id);
        }
        ambSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(amb);
        if (config.ambience == null) config.ambience = amb;

        // heard from a tree, so positional (the sync made amb_* non-positional)
        SoundEvent creak = Event("amb_forest_creak");
        if (creak != null && creak.space == SoundEvent.SoundSpace.Screen)
        {
            creak.space = SoundEvent.SoundSpace.World;
            creak.ignorePause = false;
            EditorUtility.SetDirty(creak);
        }

        Asset(out MusicSounds music, "MusicSounds");
        Fill(music, ("mainMenu", "mus_main_menu"), ("day", "mus_day_explore"), ("night", "mus_night_tension"),
             ("combat", "mus_combat_layer"), ("freezing", "mus_freezing_layer"), ("lowFire", "mus_low_fire_layer"),
             ("gameOver", "mus_game_over"), ("gameWon", "mus_game_won"));
        if (config.music == null) config.music = music;
        EditorUtility.SetDirty(config);
    }

    // A CollideAudio beside every CollideAnimation (the bump trigger, a child).
    static void SetUpCollide(GameObject root, string id)
    {
        foreach (CollideAnimation c in root.GetComponentsInChildren<CollideAnimation>(true))
            Fill(Add<CollideAudio>(c.gameObject), ("touched", id));
    }

    static FireSoundSet Fire(string name)
    {
        bool created = Asset(out FireSoundSet f, $"Fire_{name}");
        if (created || f.layers.Length == 0)
        {
            f.layers = name switch
            {
                "Campfire" => Layers(("sfx_campfire_loop_ember", 0.125f), ("sfx_campfire_loop_small", 0.375f),
                                     ("sfx_campfire_loop_medium", 0.625f), ("sfx_campfire_loop_roaring", 0.875f)),
                "Furnace" => Layers(("sfx_furnace_loop_ember", 0.125f), ("sfx_furnace_loop_small", 0.375f),
                                    ("sfx_furnace_loop_medium", 0.625f), ("sfx_furnace_loop_roaring", 0.875f)),
                _ => Layers(("sfx_torch_flame_loop", 0.5f)),
            };
            if (name == "Campfire")
                f.feeds = new[]
                {
                    new FireSoundSet.Feed { fuel = FuelType.Stick, sound = Event("sfx_campfire_feed_stick") },
                    new FireSoundSet.Feed { fuel = FuelType.Wood, sound = Event("sfx_campfire_feed_log") },
                    new FireSoundSet.Feed { fuel = FuelType.Sap, sound = Event("sfx_campfire_feed_sap") },
                };
            if (name == "Torch") { f.stageCount = 3; f.motionBoost = 0.3f; f.popInterval = Vector2.zero; }
            EditorUtility.SetDirty(f);
        }
        switch (name)
        {
            case "Campfire":
                Fill(f, ("feedDefault", "sfx_campfire_feed_stick"), ("stageUp", "sfx_campfire_stage_up"), ("low", "sfx_campfire_low_warning"),
                     ("outSound", "sfx_campfire_out"), ("relight", "sfx_campfire_relight"), ("pop", "sfx_campfire_pop"));
                break;
            case "Furnace":
                Fill(f, ("feedDefault", "sfx_furnace_feed"), ("outSound", "sfx_furnace_out"));
                break;
            default:
                Fill(f, ("feedDefault", "sfx_torch_refuel"), ("low", "sfx_torch_low"), ("outSound", "sfx_torch_burnout"),
                     ("relight", "sfx_torch_relight"));
                break;
        }
        return f;
    }

    static FireSoundSet.Layer[] Layers(params (string id, float centre)[] layers) =>
        layers.Select(l => new FireSoundSet.Layer { loop = Event(l.id), centre = l.centre }).ToArray();

    // Every ItemDefinition (by asset name) -> its material's ItemSounds.
    static readonly Dictionary<string, string> ItemMaterials = new()
    {
        ["Scots Pine Wood"] = "WoodHeavy", ["Grey Alder Wood"] = "WoodHeavy",
        ["Stone"] = "StoneHeavy", ["Stick"] = "Stick", ["Rock"] = "StoneSmall", ["Iron Stone"] = "StoneSmall",
        ["Sap"] = "Sap", ["Red Berries"] = "Berry", ["Hemp"] = "Fibre", ["Burlap"] = "Fibre", ["Leather"] = "Leather",
        ["Pouch"] = "Bag", ["Small Bag"] = "Bag", ["Iron Bar"] = "MetalBar",
        ["Polar Bear Meat"] = "MeatRaw", ["Polar Bear Meat Cooked"] = "MeatCooked",
        ["Stone Axe"] = "ToolWood", ["Stone Pickaxe"] = "ToolWood", ["Torch"] = "ToolWood", ["Sap Torch"] = "ToolWood",
        ["Bow"] = "ToolWood", ["Shield"] = "ToolWood", ["Iron Sword"] = "ToolMetal",
        ["Iron Helmet"] = "ArmourMetal", ["Burlap Pants"] = "ArmourCloth",
    };

    static int SetUpItems()
    {
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;
            var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (def == null || !ItemMaterials.TryGetValue(def.name, out string material)) continue;
            SetRef(def, "sounds", Item(material), false);
            EditorUtility.SetDirty(def);
            n++;
        }
        return n;
    }

    // Each Instant recipe's craft sound, by what it's made of (sound.md §10.5).
    static readonly Dictionary<string, string> RecipeSounds = new()
    {
        ["Torch"] = "sfx_craft_wood", ["Bow"] = "sfx_craft_wood", ["Sap Torch"] = "sfx_craft_sap",
        ["Burlap"] = "sfx_craft_cloth", ["Small Bag"] = "sfx_craft_cloth", ["Burlap Pants"] = "sfx_craft_cloth",
        ["Stone Axe"] = "sfx_craft_stone_tool", ["Stone Pickaxe"] = "sfx_craft_stone_tool", ["Iron Sword"] = "sfx_craft_metal",
    };

    static void SetUpRecipes()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:RecipeBook"))
        {
            var book = AssetDatabase.LoadAssetAtPath<RecipeBook>(AssetDatabase.GUIDToAssetPath(guid));
            if (book == null) continue;
            var so = new SerializedObject(book);
            SerializedProperty recipes = so.FindProperty("recipes");
            for (int i = 0; i < recipes.arraySize; i++)
            {
                SerializedProperty r = recipes.GetArrayElementAtIndex(i);
                string name = r.FindPropertyRelative("displayName").stringValue;
                SerializedProperty sound = r.FindPropertyRelative("craftSound");
                if (sound.objectReferenceValue == null && RecipeSounds.TryGetValue(name, out string id))
                    sound.objectReferenceValue = Event(id);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(book);
        }
    }

    static ItemSounds Item(string material)
    {
        Asset(out ItemSounds s, $"Item_{material}");
        (string pickup, string land) = material switch
        {
            "WoodHeavy" => ("sfx_item_wood_heavy_pickup", "sfx_item_wood_heavy_land"),
            "StoneHeavy" => ("sfx_item_stone_heavy_pickup", "sfx_item_stone_heavy_land"),
            "Stick" => ("sfx_item_stick_pickup", "sfx_item_stick_land"),
            "StoneSmall" => ("sfx_item_stone_small_pickup", "sfx_item_stone_small_land"),
            "Sap" => ("sfx_item_sap_pickup", "sfx_item_sap_land"),
            "Berry" => ("sfx_bush_pick", "sfx_item_berry_land"),
            "Fibre" => ("sfx_item_fibre_pickup", "sfx_item_fibre_land"),
            "Leather" or "Bag" => ("sfx_item_leather_pickup", "sfx_item_leather_land"),
            "MetalBar" => ("sfx_item_metal_bar_pickup", "sfx_item_metal_bar_land"),
            "MeatRaw" or "MeatCooked" => ("sfx_item_meat_pickup", "sfx_item_meat_land"),
            "ToolWood" => ("sfx_item_tool_wood_pickup", "sfx_item_tool_wood_land"),
            "ToolMetal" => ("sfx_item_tool_metal_pickup", "sfx_item_tool_metal_land"),
            "ArmourMetal" => ("sfx_item_armour_metal_pickup", "sfx_item_armour_metal_land"),
            _ => ("sfx_item_armour_cloth_pickup", "sfx_item_armour_cloth_land"),
        };
        Fill(s, ("pickup", pickup), ("land", land));
        switch (material)
        {
            case "Berry": Fill(s, ("use", "sfx_player_eat_berry")); break;
            case "MeatRaw": Fill(s, ("use", "sfx_player_eat_meat_raw")); break;
            case "MeatCooked": Fill(s, ("use", "sfx_player_eat_meat_cooked")); break;
            case "Bag": Fill(s, ("equip", "sfx_equip_leather"), ("unequip", "sfx_unequip_leather")); break;
            case "ArmourMetal": Fill(s, ("equip", "sfx_equip_metal"), ("unequip", "sfx_unequip_metal")); break;
            case "ArmourCloth": Fill(s, ("equip", "sfx_equip_cloth"), ("unequip", "sfx_unequip_cloth")); break;
        }
        return s;
    }

    // ---------------------------------------------------------------- plumbing

    static void LoadEvents()
    {
        events = new Dictionary<string, SoundEvent>();
        if (!AssetDatabase.IsValidFolder(EventsRoot)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:SoundEvent", new[] { EventsRoot }))
        {
            var e = AssetDatabase.LoadAssetAtPath<SoundEvent>(AssetDatabase.GUIDToAssetPath(guid));
            if (e != null && !events.ContainsKey(e.name)) events[e.name] = e;
        }
    }

    static SoundEvent Event(string id)
    {
        if (id == null) return null;
        if (events.TryGetValue(id, out SoundEvent e)) return e;
        Debug.LogWarning($"[AudioPrefabSetup] No SoundEvent '{id}'.");
        missingEvents++;
        return null;
    }

    // The set asset at Sets/<name>.asset, made if missing. True when it was just made.
    static bool Asset<T>(out T asset, string name) where T : ScriptableObject
    {
        string path = $"{SetsRoot}/{name}.asset";
        asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return false;
        SoundEventSync.EnsureFolder(SetsRoot);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return true;
    }

    // Fill each empty SoundEvent field of `target` with the event of that ID.
    static void Fill(Object target, params (string field, string id)[] fields)
    {
        if (target == null) return;
        var so = new SerializedObject(target);
        foreach ((string field, string id) in fields)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[AudioPrefabSetup] {target.GetType().Name} has no field '{field}'."); continue; }
            if (p.objectReferenceValue == null) p.objectReferenceValue = Event(id);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    // A material's per-tool sounds: adds an entry for each kind no entry covers yet, so an
    // entry tuned by hand (or covering several tools) is left alone.
    static void ByTool(SurfaceSounds s, params (ToolKind kind, string id)[] entries)
    {
        if (s == null) return;
        var list = new List<SurfaceSounds.ToolHit>(s.byTool ?? new SurfaceSounds.ToolHit[0]);
        foreach ((ToolKind kind, string id) in entries)
        {
            if (list.Any(t => (t.tools & kind) != 0)) continue;
            SoundEvent e = Event(id);
            if (e != null) list.Add(new SurfaceSounds.ToolHit { tools = kind, sound = e });
        }
        if (list.Count == (s.byTool?.Length ?? 0)) return;
        s.byTool = list.ToArray();
        EditorUtility.SetDirty(s);
    }

    static void SetRef(Object target, string field, Object value, bool overwrite)
    {
        if (target == null || value == null) return;
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p.objectReferenceValue == null || overwrite) p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static T Add<T>(GameObject root) where T : Component =>
        root.TryGetComponent(out T c) ? c : root.AddComponent<T>();

    // Open a prefab under Assets/Prefabs, change it, save it. 1 if it exists.
    static int Edit(string prefab, System.Action<GameObject> change)
    {
        string path = $"{Prefabs}/{prefab}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning($"[AudioPrefabSetup] No prefab at {path}.");
            return 0;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            change(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return 1;
    }
}

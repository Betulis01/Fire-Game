# Fire Game — Audio System

How the game's sound works and how to work with it. It uses native Unity audio and an AudioMixer, with no middleware.

| Doc | What it's for |
|---|---|
| **This doc** | How to use the system, and how it works. |
| [sound.md](sound.md) | **The catalogue:** every sound the game needs (265 events). It's the source of truth for which events exist. Tools read it, so its tables keep their format. |
| [SoundSourcing.md](SoundSourcing.md) | Where to find sounds, licence rules, and the silent P1 sounds in batches. |
| [SoundSystemPlan.md](SoundSystemPlan.md) | The original design and the phase-by-phase build log. History and rationale; where it disagrees with this doc, this doc wins. |

**Contents**
1. [How to use it](#1-how-to-use-it)
2. [How it works](#2-how-it-works)
3. [The catalogue](#3-the-catalogue-soundmd)
4. [Where it stands](#4-where-it-stands)

---

## 1. How to use it

### 1.1 The one idea to keep in mind

**Gameplay never plays sounds. It raises facts, and audio decides what they sound like.**
- `Health` raises `Damaged`, and `CreatureAudio` hears it and plays the hurt voice.
- Every sound is a **SoundEvent** asset named after its sound.md ID, such as `sfx_wolf_hurt`.
- Prefabs carry small `*Audio` components that point at **sets** (groups of events, such as `Voice_Wolf`).

So most audio work is data: give an event clips, or point a set field at an event. Code only comes in for a brand-new kind of moment.

### 1.2 Give an event its sound (the common case)

Most events exist already and are just silent. To fill one:

1. **Find the sound.** Use [SoundSourcing.md](SoundSourcing.md) for sources and search terms. Use CC0, CC-BY or royalty-free sounds; never NonCommercial or NoDerivatives licences.
2. **Put the files in `Assets/Audio/_Incoming/`**: wav, ogg, mp3, aiff or flac. The folder is git-ignored, and files in it import uncompressed.
3. **Open Tools → Audio → Sound Import.**
   - Fill in the **batch**: Source (the pack or page name), Author, Licence and URL. Use one batch per pack, so every licence row is right.
   - Audition files with **▶** in the left list.
   - Drag files onto events in the right list. It starts with silent events, P1 first; expand an event to read its brief and how many variants sound.md asks for.
4. **Press Import.** For each file it:
   - levels it to a −1 dBFS peak;
   - trims one-shots of leading and trailing silence, with fades of a few milliseconds;
   - crossfades loop seams (equal power);
   - writes it mono, unless it's a wind bed or music;
   - names it `<id>_NN.wav` and puts it in the right `Assets/Audio/Clips/` folder, which sets its import settings;
   - adds a row to `Assets/Audio/LICENSES.csv`;
   - adds it to the event's clips;
   - moves the source file to `_Incoming/_Done`. Sources are never deleted.
5. **Listen:** select the event asset and press **▶ Play** in its inspector. That plays with the event's own pitch and volume jitter, so you hear the variety.
6. **Run Tools → Audio → Validate** when a batch is done.

> Without the window, a file named `<id>_NN.wav` anywhere under `Assets/Audio/Clips/` is picked up by **Tools → Audio → Auto-Fill Clips**. You'd then have to trim, level and write the licence row yourself.

**Variants:** several takes of one event, such as 4 snow steps, keep repeats from sounding mechanical. The event shuffles them so the same take never plays twice in a row. Two or three takes cut from one recording are fine.

### 1.3 Tune an event

Select the event asset (`Assets/Audio/Events/<section>/<id>.asset`). Its inspector shows the sound.md brief on top, then:

| To change | Field | Notes |
|---|---|---|
| How loud | `volumeDb` | Balance **events** against each other here, never by editing the clips. Group levels live in the mixer. |
| Variety | `volumeJitterDb`, `pitchJitterSemitones` | ±1.5 dB and ±0.8 semitones by default. Set them to 0 for music and beds. |
| How far it carries | `rangeScale` | Multiplies the hearing radius: footsteps 0.6, tree fell 1.6, skeleton spawn 1.8. |
| Positional or not | `space` | World is panned and fades with distance. Screen is for UI, player status and stingers. |
| Spam control | `cooldown`, `maxVoices`, `steal`, `priority` | A cleave hitting 6 targets plays about 2 impacts. |
| Plays together | `layers` | Child events started with this one, e.g. a heavy step plus a load creak. |
| Plays during pause | `ignorePause` | For UI, music and menu sounds. |

The **Analyse** button shows each clip's peak and RMS level and flags variants more than 2 dB apart.

### 1.4 Add a new sound

**Step one is always sound.md.** Add a row with an ID, when it plays, what it should sound like, the variant count, the type and the priority (§3). Then run **Tools → Audio → Sync Events From sound.md**. That creates the `SoundEvent` with sensible defaults for its section: bus, space, range, limits, and loop for `loop` and `layer` rows. Then pick the case that fits:

**A. A new thing of a kind that already exists** (a new creature, item material, tool, surface, ground or recipe). No code needed:
1. Make the set the kind uses (Create → Audio → …), or copy a similar one in `Assets/Audio/Sets/`:

   | New … | Set | Points at it |
   |---|---|---|
   | Creature | `Voice_<Name>` (CreatureVoice), `Footsteps_<Name>` (FootstepProfile) | `CreatureAudio`, `FootstepAudio` on the prefab |
   | Hittable material | `Surface_<Name>` (SurfaceSounds) | `SurfaceAudio` |
   | Tool or weapon | `Tool_<Name>` (ToolSounds) | `ToolAudio` on the tool prefab |
   | Item material | `Item_<Name>` (ItemSounds) | `ItemDefinition.sounds` |
   | Fire | `Fire_<Name>` (FireSoundSet) | `FireAudio` |
   | Recipe | — | `Recipe.craftSound` (one of the `sfx_craft_*` events) |
   | Ground type | a row in `GroundSounds` | AudioConfig |

2. Link its fields to the new events, and the prefab's component to the set.
3. Alternatively, teach **Tools → Audio → Set Up Audio On Prefabs** the new set, in `AudioPrefabSetup.cs`. Then it's wired by ID like everything else and comes back after a rebuild.
   - That tool only adds what's missing and fills empty fields. It never replaces something set by hand.
   - `EnemyArtSetup` calls it on every creature rebuild.
4. For a creature, give its run and walk clips footstep frames in the table in `FootstepEvents.cs`, then run **Tools → Audio → Bake Footstep Events**.

**B. A new moment on something that already has audio.** For example, a new CreatureVoice slot for "sniff", or a new ToolSounds slot:
1. Add a `SoundEvent` field to the set class.
2. Play it from the component that hears the moment:
   ```csharp
   Sfx.Play(voice.sniff, transform);
   ```
3. Link the field on each set (by hand, or with an extra line in `AudioPrefabSetup`).

**C. A new kind of moment that needs code.**
1. In the gameplay script, raise a C# event at the moment it happens:
   ```csharp
   public event System.Action Sniffed;
   // ...where it happens:
   Sniffed?.Invoke();
   ```
   For things created at runtime, which no prefab can reach (build sites, items), use a static event like `ItemEvents`.
2. In the matching `*Audio` reactor (or a new one), subscribe in `OnEnable`, unsubscribe in `OnDisable`, and call `Sfx`:
   ```csharp
   void OnEnable()  => brain.Sniffed += OnSniffed;
   void OnDisable() => brain.Sniffed -= OnSniffed;
   void OnSniffed() => Sfx.Play(voice.sniff, transform);
   ```
3. Never call `Sfx` from gameplay logic, and never hold a `SoundEvent` in a gameplay script.

**D. A UI sound.** The UI is the one exception: UI scripts call it directly.
1. Add a role to `enum UiSound` in `UiSoundKit.cs`.
2. Add an entry for it (role → event) on `Assets/Audio/Sets/UiSoundKit.asset`.
3. Call it from the handler:
   ```csharp
   UiSfx.Play(UiSound.Place);
   ```
   `volume` and `pitch` arguments are optional, e.g. a merge plays Place a little higher.

Most UI moments map to the shared kit (Click, Hover, Denied, Pickup, Place, Swap, Return, Toss, Open, Close, Tab, Subtab, SliderTick, …). Only add a role when the kit doesn't have one. A UI action with a world outcome plays the world's sound instead: dragging sticks onto the campfire sounds like feeding it. Pause, menu and end-screen buttons get click and hover from `ButtonSounds` (added by **Tools → Audio → Set Up Menu Audio**).

### 1.5 Loops

A loop is held, not fired.

```csharp
SoundHandle h = Sfx.PlayLoop(e, transform);
h.SetVolume(0.5f, fade: 0.3f);
h.Stop(fade: 0.5f);
```

- The component that started a loop must `Stop` it in `OnDisable`.
- Layers that must stay in step (the campfire's four) start with the same playhead.
- Loops far outside hearing go virtual by themselves (§2.4), so 20 torches cost nothing.

### 1.6 Check your work

| Tool | Use it for |
|---|---|
| **Tools → Audio → Validate** | One report covering:<ul><li>config and mixer wiring, and the shared sets;</li><li>coverage per sound.md section, and every event without clips, P1 first;</li><li>clips with more than 10 ms of leading silence;</li><li>loops whose ends click;</li><li>unused clips;</li><li>clips with no licence row, or an unknown licence;</li><li>prefabs with a Hurtbox but no `SurfaceAudio`, or footstep events but no `FootstepAudio`;</li><li>items with no `ItemSounds`.</li></ul> |
| **F9 in Play mode** (editor and development builds) | The debug overlay: every live voice (event, gain, pan, what it follows), audible and virtual loops, steals and drops per second.<ul><li>F8 picks one of `AudioConfig.debugTestEvents`.</li><li>F10 plays it at the cursor.</li><li>F11 fires it 100 times; the limits should hold that to a few.</li></ul> |
| **Audio Mixer window**, in Play mode | The group meters. |
| **SoundEvent inspector** | ▶ Play, ■ Stop and Analyse. |

### 1.7 Tools at a glance

| Menu | What it does |
|---|---|
| **Tools → Audio → Sound Import** | Brings real recordings onto events (§1.2). |
| **Tools → Audio → Sync Events From sound.md** | Creates the missing `SoundEvent`s from sound.md and refreshes every event's brief, priority and variant count. It never touches your tuning, and reports events whose ID left sound.md without deleting them. It runs Auto-Fill too. |
| **Tools → Audio → Auto-Fill Clips** | Links clips named `<id>_NN` to their event, in order. It skips `Clips/_Placeholder` and events whose clips were picked by hand. |
| **Tools → Audio → Set Up Audio On Prefabs** | Wires every set, reactor, item, recipe and footstep event by ID. Idempotent, and never replaces a hand choice. |
| **Tools → Audio → Set Up Menu Audio** | Button sounds on the menu panels, and the Audio settings section on the pause panel. |
| **Tools → Audio → Bake Footstep Events** | Writes `OnFootstep` Animation Events at the contact frames in `FootstepEvents.cs`. |
| **Tools → Audio → Validate** | The health report (§1.6). |

---

## 2. How it works

### 2.1 Overview

```
 Gameplay (raises facts)                 Audio layer
 ───────────────────────                 ────────────────────────────────────────────
 Health.Damaged ──────────┐              *Audio reactors (on prefabs)
 EnemyBrain.StateChanged ─┤                CreatureAudio, FootstepAudio, SurfaceAudio,
 WeaponUse.Swung ─────────┼────────────▶   WeaponAudio, FireAudio, DestructibleAudio ...
 Fuel.FuelAdded ──────────┤                pick WHICH SoundEvent, and WHERE
 ItemEvents.PickedUp ─────┘                          │
 UI handlers ── UiSfx.Play(UiSound.Place) ───────────┤
                                                     ▼
 Directors (on AudioSystem):                 Sfx (static API, the only door)
   ItemAudioDirector ← ItemEvents                    │
   BuildAudio        ← PlacementGhost, builds        ▼
   MenuAudio         ← GameState             AudioSystem (persistent)
   AmbienceDirector  ← clock, trees            ├─ 28 one-shot voices
   MusicDirector     ← state, combat,          ├─ 16 loop sources, virtual loops
                       cold, low fire          ├─ AudioSpace: gain and pan from the camera
   MixDirector       ← warmth, cold, health    └─ per-event cooldowns, shuffle bags
                                                     │
                                                     ▼
                                             AudioMixer: groups · snapshots · ducking
```

### 2.2 SoundEvent: data only

A ScriptableObject (Create → Audio → Sound Event), one per sound.md ID, under `Assets/Audio/Events/<NN Section>/`.

| Field | Default | What it does |
|---|---|---|
| `clips` | — | The variants. **Empty is legal and plays silence**, so everything can be wired before audio exists. |
| `selection` | ShuffleNoRepeat | Every variant plays before any repeats, and never the same twice in a row. Also: Random, Sequential. |
| `volumeDb` / `volumeJitterDb` | 0 / ±1.5 | Level in dB, and random variation per play. |
| `pitchJitterSemitones` | ±0.8 | Random pitch, about ±5 %. |
| `output` | by section | The mixer group. |
| `space` | World | World: from its position. Screen: non-positional. |
| `rangeScale` | 1 | Multiplies the hearing radius. |
| `loop` | false | Played through `PlayLoop`. |
| `cooldown` | 0.04 s | The minimum gap between starts, in unscaled time. |
| `maxVoices` / `steal` | 4 / Oldest | The cap on concurrent voices, and what happens at the cap (Oldest, Quietest, None). |
| `priority` | Normal | Who loses when the pool is full: Low, Normal, High, Critical. |
| `ignorePause` | false | Keeps playing while paused. |
| `layers` | — | Child events started together, each with its own limits and bus. |
| `startDelay` | 0 | A small offset, e.g. an item landing just after the loot pop. |
| `brief`, `catalogPriority`, `catalogSection`, `catalogVariants` | from sound.md | Written by Sync, shown in the inspector. |

The event holds no runtime state. Cooldowns and shuffle bags live in `AudioSystem` and reset every Play session, so nothing goes stale in the editor.

### 2.3 Sfx: the only API

```csharp
Sfx.Play(e, Vector2 at, volume = 1, pitch = 1);
Sfx.Play(e, Transform follow, volume = 1, pitch = 1);   // follows it (an arrow, a voice)
Sfx.Play2D(e, volume = 1, pitch = 1);                   // non-positional
Sfx.PlayLoop(e, Transform follow, volume = 1, playhead = -1);   // -1: a random start
Sfx.PlayLoopAt(e, Vector2 at, volume = 1, playhead = -1);
```

`SoundHandle`:
- `IsValid` and `IsAudible`;
- `SetVolume(v, fade)`, `SetPitch(p)`, `MoveTo(point)`, `Stop(fade)`.

A handle goes stale once its voice ends, is stolen or is stopped. Calling it then does nothing.

- `volume` and `pitch` scale on top of the event's jitter. Charge amount, combo steps and a build filling up all use them.
- **Safe everywhere:** with no AudioSystem (edit mode, tests, a scene tearing down), every call quietly does nothing. A null event does nothing too.
- **Engine seam:** only `AudioSystem` touches `AudioSource`. A move to FMOD would replace `SoundEvent` and the system's internals; reactors and call sites would stay as they are.

### 2.4 AudioSystem: voices, loops, state

- **Bootstrap:** created in code before the first scene loads (`RuntimeInitializeOnLoadMethod`) as a DontDestroyOnLoad object.
  - It holds the AudioListener, `AudioConfig` (from `Assets/Audio/Resources/AudioConfig.asset`, which links the mixer and the shared sets) and the directors.
  - It disables any other AudioListener it finds.
- **Starting a one-shot:**
  1. cooldown check;
  2. voice-cap check;
  3. cull check: a World sound starting below −40 dB isn't started;
  4. take a clip from the shuffle bag;
  5. apply jitter and route to the bus;
  6. play.
- **Pool:** 28 pre-made one-shot voices, so playing allocates nothing.
  - **Stealing:** when the pool is full, the lowest priority loses, then the quietest, then the oldest. If everything playing outranks the new sound, the new sound is dropped.
  - **Frame budget:** at most 10 new voices per frame. The rest are dropped, lowest priority first.
- **Loops:** 16 loop sources.
  - A loop beyond 1.2 × the hearing radius goes **virtual**: it gives its source back, but its playhead keeps running.
  - When the loop comes back into hearing it resumes mid-stream, so it never sounds restarted.
- **Pause:** the Paused state sets `AudioListener.pause`, so the world freezes mid-sound. Sources with `ignorePause` (UI, music, menu ambience) keep going.
- **Snapshots** follow the game state: Gameplay, Paused, Menu, End.
- **Time:** everything runs on unscaled time, so it works while the game is paused, in menus and during hit-stop (timeScale 0).
- **Scene unload:** World voices and loops from the old scene stop. Music and UI carry on, so "Quit to menu" doesn't trigger a burst of extinguish sounds.
- **Focus:** in builds, it mutes when the window loses focus, unless the player turned that off in the Audio settings.

### 2.5 Hearing: AudioSpace

Every AudioSource is 2D (spatialBlend 0). The camera sits at z −10, so Unity's 3D rolloff would be wrong. Hearing is computed from the camera's view instead:

```
halfW  = camera.orthographicSize × aspect              // half the visible width
R      = max(6, 1.6 × halfW) × event.rangeScale         // hearing radius
gain   = falloff(distance / R)                          // full near, eased to 0 at R
pan    = clamp((x − camera.x) / halfW, −1, 1) × 0.55    // a gentle hint, not hard panning
```

- **Zoom-aware:** zooming out lets you hear more of the world.
- **Just off-screen is faint but audible,** so a skeleton rising out of view is a warning.
- The radius factor, minimum, falloff curve, pan width, cull level and virtual-loop distance are all in `AudioConfig`, and can be tuned in Play mode.

### 2.6 The mixer

`Assets/Audio/FireGame.mixer` is built by hand, because Unity has no API for building mixers.

```
Master            [MasterVolume]
├── Music         [MusicVolume]     · Duck Volume ◀ Combat, Stingers
├── Ambience      [AmbienceVolume]  · Duck Volume ◀ Combat, Stingers · Lowpass (snapshots)
├── SFX           [SfxVolume]       · Lowpass
│   ├── Player      voice, body, footsteps, eating
│   ├── Combat      swings, impacts, skills          · sends to the ducks
│   ├── Creatures   voices, creature steps
│   └── World       fire, stations, props, items, building · Highpass
├── Stingers      time of day, level-up, game over/won · send to the Music duck
└── UI            [UiVolume]
```

**Two owners, never the same control.** Once code sets an exposed parameter, Unity stops letting snapshots drive it. So:
- **Settings** own the five exposed volumes in brackets. `AudioSettingsStore` saves them in PlayerPrefs as 0..1 and converts with `dB = 20·log10(v)`, −80 at zero. They're applied in `Start`, because Unity resets mixer values between Awake and Start.
- **Snapshots** own everything else:

  | Snapshot | When | What |
  |---|---|---|
  | Gameplay | Playing | Neutral. |
  | Paused | Paused | Music −6 dB and muffled; Ambience −8 dB. The world is frozen by the listener pause. |
  | Menu | Main menu | Gameplay groups silent; Ambience −10 dB and muffled. |
  | End | Game over / won | Gameplay groups fade out over 1.5 s; Music and Stingers stay. |

- **Ducking:** Combat and Stingers dip Music and Ambience by 3–4 dB, so big hits and stingers come through without the game getting louder.
- **Continuous mix** (`MixDirector`) would need three more exposed parameters: `AmbienceHeatCutoff` (wind muffled near a fire), `WorldFreezeCutoff` (the world thins while freezing) and `SfxLowHealthCutoff` (SFX muffle below 25 % health).
  - **These aren't set up yet** (§4). Until they are, `MixDirector` warns once and does nothing.
  - Setting them up: in the Audio Mixer window, add a *second* Lowpass to Ambience. Expose its cutoff, the SFX Lowpass cutoff and the World Highpass cutoff, and rename the three parameters to those names. Keep them out of every snapshot.

**The pause panel's Audio section** has five sliders and a "Mute when unfocused" toggle (`AudioSettingsPanel`). Dragging a slider ticks at the level you're setting.

### 2.7 Sets

Sets group events so prefabs share one tuning. They live in `Assets/Audio/Sets/`.

| Set (class) | Assets | Holds | Used by |
|---|---|---|---|
| `CreatureVoice` | `Voice_Skeleton`, `_Wolf`, `_Boar`, `_Deer`, `_PolarBear` | spawn, idle (and its interval), aggro, attack windup, swing and bite, hurt, hurt while burning, death, flee, flee loop, chase loop, despawn | `CreatureAudio` |
| `FootstepProfile` | `Footsteps_Player`, `_Skeleton`, `_Wolf`, `_Boar`, `_Deer`, `_PolarBear` | the foot layer (bone, paw, hoof, heavy paw), the ground layer's level and range, the carry layer | `FootstepAudio` |
| `GroundSounds` | `GroundSounds` (on AudioConfig) | ground type → step, landing layer, slide loop | every footstep and landing |
| `SurfaceSounds` | `Surface_Flesh`, `_Bone`, `_Player`, `_Wood`, `_WoodLog`, `_Stone`, `_IronOre`, `_Straw` | impact, heavy impact, glance knock, crit override | `SurfaceAudio` |
| `ToolSounds` | `Tool_Fists`, `_Axe`, `_Pickaxe`, `_Sword`, `_Torch`, `_Bow`, `_Shield` | swing, heavy swing, charge loop, charge ready, glance, draw, stow, held loop | `WeaponAudio`, via `ToolAudio` |
| `ItemSounds` | `Item_*` (Stick, StoneHeavy, Berry, MeatRaw, MetalBar, ArmourCloth, Bag, …) | pickup, land, equip, unequip, use | `ItemAudioDirector`, via `ItemDefinition.sounds` |
| `FireSoundSet` | `Fire_Campfire`, `_Furnace`, `_Torch` | layered loops by fuel, feed per fuel type, stage up, low, out, relight, pops, ignite | `FireAudio` |
| `WorldSounds` | `WorldSounds` (on AudioConfig) | item-handling layers (pocket, auto-pickup, lift, throw, spill, bag swap, loot pop, a fallback item set) and the building sounds | `ItemAudioDirector`, `BuildAudio`, `LootAudio` |
| `UiSoundKit` | `UiSoundKit` (on AudioConfig) | `UiSound` role → event | `UiSfx` |
| `AmbienceSounds` | `AmbienceSounds` (on AudioConfig) | day and night wind, scattered sounds with intervals (gust, tree creak, dawn birds, owl, far howl), dusk, night and dawn stingers | `AmbienceDirector` |
| `MusicSounds` | `MusicSounds` (on AudioConfig) | menu, day and night bases; combat, freezing and low-fire layers; game over and won; fades | `MusicDirector` |

**Footsteps are layered: ground × foot.** A wolf on ice plays the *ice* step and a *paw* layer, so every creature sounds right on every ground without a file per pair.

### 2.8 Reactors and directors

Reactors sit on prefabs, and directors sit on the AudioSystem. Each listens to C# events and plays from its sets.

| Component | On | Listens to | Plays |
|---|---|---|---|
| `PlayerAudio` | Player | Health damaged, died, healed; freezing and cold; level-ups, XP, buffs; health ratio (heartbeat) | the player's body and voice, kill confirm |
| `FootstepAudio` | Player, creatures | `OnFootstep` Animation Events (a stride fallback after 0.5 s without one); the ground under it; carrying | steps, landings, ice slide |
| `WeaponAudio` | Player | `WeaponUse` (armed, charge ready, released, swung, landed), shield, loadout swap, stow and draw, skill cast fail and ready | swings, charge, glance, miss, block |
| `ToolAudio` | each tool | — (holds the tool's `ToolSounds`) | — |
| `InteractionAudio` | Player | hold-E progress, throw, fumble, the drop ghost | §3.3–3.5 of sound.md |
| `SurfaceAudio` | everything with a Hurtbox | being hit: impact, or the glance knock when the tool can't damage it; crit layer | sound.md §13 |
| `CreatureAudio` | creatures | brain state, spawn, attack windup, swing and hit, damage, death, despawn, knockback; idle timer | the creature's voice |
| `FlammableAudio` | everything flammable | ignited, burning tick, extinguished | the burning loop and its edges |
| `DestructibleAudio` | trees, ore, logs, big stone, dummy | hit (shake), low health (creak once), died (fell, break, split) | sound.md §7.3, §9.1–9.2 |
| `LootAudio` | anything that drops loot | `Dropped` | loot pop, then the item landing |
| `CollideAudio` | bushes, hemp, logs, big stone | its own trigger, 0.5 s per-object cooldown, moving bodies only | rustles, bumps |
| `HarvestAudio` | Hemp Plant | harvested, regrown | harvest, regrow |
| `FireAudio` | campfire, furnace, torches | fuel level each frame (layer crossfade, low/out/relight edges, pops), fuel added; for torches, held or stowed | sound.md §8.1–8.2, §4.3 |
| `ProcessorAudio` | campfire, furnace | working on and off, job finished, stalled, input deposited | cooking, smelting |
| `StructureAudio` | stations | built, workbench opened and closed | build complete, workbench |
| `CraftAudio` | Player | `CraftingController.Crafted` | the recipe's `craftSound` |
| `ProjectileAudio` | Arrow | launched, hit, expired | arrow flight and hit |
| `WhirlwindAudio`, `AxeThrowAudio` | skill prefabs, Thrown Axe | the skills' and axe's events | sound.md §5 |
| `ItemAudioDirector` | AudioSystem | static `ItemEvents` (picked up, landed, thrown, spilled, worn, removed, wielded, eaten) | the item's `ItemSounds` plus world layers |
| `BuildAudio` | AudioSystem | `PlacementGhost` began, placed, cancelled; build deposits | sound.md §8.4 |
| `MenuAudio` | AudioSystem | game state | Play from the main menu, the pause panel opening and closing |
| `AmbienceDirector` | AudioSystem | the clock; trees within hearing | wind beds crossfaded by hour (night rises 19:00–22:00 and fades 05:00–08:00), scattered sounds on game-time clocks, stingers at dusk, night (22:00) and dawn |
| `MusicDirector` | AudioSystem | game state, the player's combat and freezing, the nearest campfire's fuel | the base per state, fading layers, the game-over sting; carries across the scene reload |
| `MixDirector` | AudioSystem | warmth, freezing, health | the continuous mix (§2.6) |

**Bark budget:** creature idle sounds are capped at one per 1.2 s across the world, and one per creature kind per 3 s, all from within hearing. Six wolves together sound like a pack, not a choir.

### 2.9 Gameplay hooks

Each hook is a small C# event, raised where the fact already happens. None changed any behaviour. The main ones:

| Script | Events |
|---|---|
| `GameStateManager` | `static StateChanged` |
| `Health` | `Damaged`, `Died`, `Healed` |
| `WeaponUse` | `SwingArmed`, `ChargeReady`, `Released`, `Swung`, `Landed(StrikeResult)`, `Fired` |
| `ShieldBlock` / `Loadout` / `Hands` | `BlockingChanged`, `Blocked` / `Swapped` / `CarryingChanged` |
| `ItemEvents` (static) | `PickedUp`, `Landed`, `Thrown`, `Spilled`, `Worn`, `Removed`, `Wielded`, `Eaten` |
| `PlayerInteractor`, `Quickslot` | `HoldProgress`, `Fumbled`, `DropGhostShown`; `EatFailed` |
| `PlayerTemperature` | `IsFreezing`, `FreezingChanged`, `ColdOverflow` |
| `PlayerExperience`, `WeaponExperience`, `PlayerBuffs` | `LeveledUp`, `Awarded`; `LeveledUp`; `Added`, `Expired` |
| `EnemyBrain`, `EnemyAttacker`, `EnemyAnimator`, `Knockback` | `StateChanged`; `SwingStarted`, `Swung`, `Struck`; `SpawnStarted`; `Applied` |
| `Flammable`, `Fuel` | `Ignited(fresh)`, `Ticked`, `Extinguished`; `FuelAdded(amount, item)` |
| `ItemProcessor`, `ItemReceiver`, `BuildRequirement` | `WorkingChanged`, `JobFinished`, `Stalled`; `Deposited`, `static AnyDeposited`; `static IngredientSatisfied` |
| `PlacementGhost` (static), `CraftingController` | `Began`, `Placed`, `Cancelled`, `Completed`; `Crafted`, `CraftFailed` |
| `Harvestable`, `DropOnHit` / `DropOnDeath`, `Projectile` | `Harvested`, `Regrown`; `Dropped`; `Launched`, `HitSomething`, `Expired` |
| `WhirlwindSkill`, `AxeThrowSkill`, `ThrownAxe`, `ActiveSkillSlots` | `Started`, `Spun`, `Ended`; `WindupStarted`, `Released`, `CaughtBack`; `Stuck`, `Returning`; `CastFailed`, `CooldownReady` |
| `StationInteractable`, `WorkbenchInteractable`, `HudMenu` | `PanelOpened`/`Closed`; `Opened`/`Closed`; `Opened`/`Closed` |
| `DayNightCycle`, `CombatState`, `EnemyDespawner` | `PhaseChanged`, `NightChanged`; `Changed`; `Despawned` |

SoundSystemPlan.md §6 lists where each one is raised.

### 2.10 Files, folders and import settings

```
Assets/Audio/
  FireGame.mixer
  Resources/AudioConfig.asset           voices, hearing, shared sets, mix, snapshots
  Events/<NN Section>/<id>.asset        one SoundEvent per sound.md ID
  Sets/                                 the sets in §2.7
  Clips/
    SFX/<Section>/<id>_NN.wav           one-shots
    SFX/Loops/<Section>/                loops (fire, torch, burning, slide)
    UI/                                 HUD and UI
    Ambience/                           gusts, birds, howls
    Ambience/Beds/                      wind beds
    Music/
    _Placeholder/                       generated stand-ins: kept, linked to nothing
  _Incoming/                            downloads for Sound Import (not in git)
  LICENSES.csv                          one row per clip: file, source, author, licence, url, notes
```

Import settings come from the folder (`AudioImportRules`), so no clip is tuned by hand:

| Folder | Channels | Load type | Quality |
|---|---|---|---|
| `SFX/`, `UI/` and the rest of `Clips/` | mono | Decompress On Load (instant start) | Vorbis 70 % |
| `SFX/Loops/` | mono | Compressed In Memory | Vorbis 60 % |
| `Ambience/` | mono | Compressed In Memory | Vorbis 60 % |
| `Ambience/Beds/` | stereo | Streaming | Vorbis 55 % |
| `Music/` | stereo | Streaming, loaded in the background | Vorbis 60 % |
| `_Incoming/` | as is | Decompress On Load, PCM | — (so Sound Import can read it) |

**Naming:** a clip is its sound.md ID plus `_NN` (`sfx_wolf_hurt_01.wav`); the event asset is the bare ID. Git LFS stores `*.wav` and `*.ogg`.

**Delivery spec for new recordings:**
- 48 kHz (44.1 is fine), 16- or 24-bit, mono for SFX;
- no more than 10 ms of silence before the sound starts;
- loops whose ends meet.

Sound Import takes care of the trimming, levelling and loop seams.

**Project settings:**
- 48 real voices;
- DSP buffer 512, about 11 ms, so hits land with the frame;
- Doppler 0;
- stereo.

### 2.11 Performance budget

| Item | Budget |
|---|---|
| One-shot voices | 28 pooled; under 20 typical |
| Loop sources | 16 pooled. Typical: the campfire's 4 layers, a torch, a furnace's 4, a burning enemy. |
| New voices per frame | 10 |
| GC per play | 0 bytes |
| Main-thread CPU | under 0.1 ms: one pass over live voices for gain and pan; reactors only act on events |
| Memory at full content | about 50 MB of decompressed one-shots, about 10 MB of loops; music and beds stream |

---

## 3. The catalogue (sound.md)

[sound.md](sound.md) lists every sound the game needs, grouped by what makes it. Each table row is one event. **Sync reads its tables**, so edit sound.md first and then run Sync.

### 3.1 Row format

| Column | Meaning |
|---|---|
| **ID** | `sfx_<group>_<subject>_<action>` (`amb_` for ambience, `mus_` for music), in backticks. It's also the event name and the file prefix. |
| **When** | The moment it plays, and the code that knows about it. |
| **Sound** | What it should sound like. This becomes the event's brief. |
| **Var** | How many variants to record. |
| **Type** | `1×` one-shot; `loop` while a condition holds; `layer` a loop whose volume follows a value, such as fuel; `mix` a mix rule (no event). |
| **P** | **P1** the core loop, missed at once; **P2** polish that sells the world; **P3** later, or not in the scene yet. |

Sync skips:
- rows that only reuse another sound (Var or Type "—", or a Sound cell pointing at another ID, "Silent" or "Covered by");
- mix rules;
- IDs with a `<placeholder>` in them, which are templates.

### 3.2 Sound direction

- **Frozen, quiet and hostile.** Mostly wind and the crunch of snow. Silence is a tool: the fire is the loudest, warmest thing on screen.
- **The fire is the emotional centre.** It sounds alive (crackle, breathing, pops), and its sound shrinks as it dies. You should *hear* the fire getting low before you look at it.
- **The victim chooses the material, not the weapon.** An axe and a fist on a tree both sound like wood. The weapon adds its own whoosh, and a "no effect" thud on a glance.
- **Pixel-art scale.** Short, punchy, slightly stylised. UI is soft, woody and papery, not digital blips.
- **Positional:** world sounds at their source with gentle 2D falloff; UI and player status non-positional.

### 3.3 What's in it

265 events: 128 P1, 84 P2, 53 P3.

| sound.md section | Events | Covers |
|---|---|---|
| 3 Player | 38 | movement and footsteps, body (hurt, cold, health), hands and carrying, Q (drop, throw), E (interact, equip), eating, buffs, progression |
| 4 Weapons and tools | 33 | the shared swing set, per-weapon sounds, the torch, loadout swaps |
| 5 Skills | 12 | Whirlwind, Axe Throw, skill feedback |
| 6 Enemies and wildlife | 43 | skeleton, wolf, boar, deer, polar bear, target dummy, shared reactions, foot layers |
| 7 Items | 39 | item material sets, item moments, hittable carried items, bag and gear |
| 8 Stations and fire | 35 | campfire, furnace, workbench, building, heat |
| 9 Environment props | 18 | trees, ore, bushes and plants, ground landings |
| 10 HUD and UI | 25 | the shared UI kit, F menu, player tab, action bar, crafting, skills, station panel, prompts, game menus, crafting sounds |
| 11 World | 17 | ambience beds and scattered sounds, time-of-day stingers, music |
| 13 Surface materials | 5 | the surfaces hits land on |

---

## 4. Where it stands

*As of 2026-11-01. Validate always has the live picture.*

- **The system is built.**
  - All runtime, reactors, directors, tools, hooks, UI calls, settings and mixer snapshots are in place.
  - Every event exists and every prefab is wired.
  - The old `HitEffect.sfx` / `PlayClipAtPoint` path is gone.
- **Placeholders are unlinked.** The generated stand-ins were taken off every event, so the game plays only real sounds. The files stay in `Clips/_Placeholder`, and Auto-Fill never relinks them.
- **Nearly everything is silent** until real sounds come in through Sound Import. The only linked clip is the campfire's medium loop. [SoundSourcing.md](SoundSourcing.md) has the 87 silent P1 events in nine batches, fire first.
- **The campfire medium loop** (`sfx_campfire_loop_medium_01.wav`, the old `campfire.wav`) has an **unknown licence**. Validate flags it until it's replaced.
- **The continuous mix is skipped:** its three exposed mixer parameters aren't set up (§2.6), so Validate lists them.
- **Not done:**
  - music stems aren't sample-locked: loops start together, which is fine for slow layers;
  - the night music doesn't build with the number of skeletons alive.
- **Still to do after the sounds are in:**
  - a mix session: group levels first, then event `volumeDb`, never the clips;
  - a performance check against §2.11;
  - a clean licence report before any release.

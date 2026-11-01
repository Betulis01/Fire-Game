# Fire Game — Sound System Plan

This plan covers both halves of the job: the **audio system** (code, mixer, tools) and the **sound content** listed in [sound.md](sound.md), arranged into phases.

- [sound.md](sound.md) stays the catalogue of *what* the game must sound like, one event ID per row.
- This document says *how* those sounds get played, mixed, authored and produced.

The old sfx code is thrown out and replaced (§9). Nothing in the new system depends on it.

## Status

| Phase | Scope | Status |
|---|---|---|
| **1. Foundation** | Cleanup, folders, import rules, project settings, `SoundEvent` + sync from sound.md, the core runtime (`AudioSystem`, `Sfx`, voices, virtual loops, pause, settings store), editor tools, placeholders | **Code done (2026-11-01).** Waiting on: the hand-built mixer (checklist in §11) and the in-editor acceptance run. |
| **2. Gameplay hooks** | The events in §6 | **Done (2026-11-01).** Compiles; no behaviour change. Needs a quick play-through to confirm nothing changed. |
| **3. Combat, creatures, footsteps** | `SurfaceAudio`, `WeaponAudio`, `PlayerAudio`, `CreatureAudio`, `FootstepAudio`, burning, arrow, skills | **Code done (2026-11-01).** Waiting on: Sync + Set Up Audio On Prefabs in the editor, and the acceptance fight. |
| **4. World, items, stations** | `FireAudio`, processing, building, items, props, plants | **Code done (2026-11-01).** Waiting on: Sync + Set Up Audio On Prefabs, and the core-loop run. |
| **5. UI and settings** | `UiSfx` + kit, every HUD handler, menus, pause-menu Audio sliders | **Code done (2026-11-01).** Waiting on: Sync, Set Up Audio On Prefabs, Set Up Menu Audio (save the scene), and the UI run. |
| **6. Ambience, music, mix** | `AmbienceDirector`, `MusicDirector`, `MixDirector`, stingers | **Done (2026-11-01)**, except the continuous mix: its three mixer parameters were skipped for now, so `MixDirector` warns once and does nothing. Waiting on a full in-game day. |
| **7. Content and mix pass** | Sound Import window, `SoundSourcing.md`, licence audit; then batches, mix session, performance | **Tooling done (2026-11-01).** Waiting on the real sounds, batch by batch (`SoundSourcing.md`). |

---

## 1. What the project has today

These findings come from reading the code, the scene and the project settings. Several of them drive design decisions below.

| # | Finding | Consequence |
|---|---|---|
| 1 | **The Camp scene has no `AudioListener` at all.** No AudioSource exists on any prefab or in the scene. | Nothing can be heard today. The system starts from zero. |
| 2 | The only sfx code: `HitEffect.sfx` (AudioClip) played by `SurfaceHitEffect` through `AudioSource.PlayClipAtPoint`. No clip is assigned. | It gets removed (§9). `PlayClipAtPoint` spawns a GameObject per sound, and it's a 3D sound (see 3). |
| 3 | The camera is orthographic, size 3, and sits at **z = −10** (`CameraFollow.offset`). Zoom levels run from 1 to 6. | Unity 3D audio measures distance from the listener. With the listener on the camera, every sound is at least 10 units away, so falloff barely changes across the screen. Zoom isn't accounted for either. **Unity's 3D model doesn't fit this game** (§3.3). |
| 4 | `GameStateManager.SetState` sets `Time.timeScale = 0` in Main Menu, Paused, Game Over and Won. `HitStop` sets it to 0 for a few frames on impacts. | Audio keeps playing at timeScale 0, so pausing needs explicit handling. Any cooldown that reads `Time.time` freezes in menus and during hit-stop. **All audio timing must use unscaled time.** |
| 5 | "Quit to menu" **reloads the scene**. It's the only scene. | Music and UI audio should survive the reload, so the system must be persistent. Positional voices must be dropped when their scene goes away. |
| 6 | The player's clips are hand-made `.anim` files. Their existing Animation Events (`OnAttackSwing`, `OnAttackHit`, `OnAttackChargeReady`) are preserved by the tools. The enemy clips are **regenerated** by `EnemyArtSetup`, which overwrites their events: bite events on bite clips, an empty event list on everything else. | Footstep events on enemy clips must be written by `EnemyArtSetup` itself. Player footsteps can be baked once by a tool. |
| 7 | Many facts the audio needs are already exposed as C# events: `Health.Damaged`, `Died` and `Healed`, `CombatState.Changed`, `DayNightCycle.PhaseChanged` and `NightChanged`, `PlayerTemperature.ColdOverflow`, `PlayerExperience.LeveledUp`, `EnemyDespawner.Despawned`, `Fuel.FuelAdded` (only `FireLight` listens). Many others are missing (§6). | The plan adds about 35 small events. It never puts sound calls inside gameplay logic. |
| 8 | The architecture is capability components plus reactors (`IHitReactor`, `IAttackReactor`, `IKillReactor`; `FlashOnHit` and `CameraShakeOnHit` sit beside the gameplay they react to). | The audio follows the same pattern: `*Audio` reactor components sit next to the gameplay and listen to it. |
| 9 | Project audio settings: 32 real voices, 512 virtual, DSP buffer 1024, Doppler 1. | These get tuned in Phase 1 (§3.9). |
| 10 | There's one stray clip, `Assets/Sound/Fire/campfire.wav`, referenced by nothing. | It moves into the new folder layout as the campfire's medium-layer candidate. |
| 11 | Unity 6.4 offers the Audio Random Container (ARC) and, since 6.3, the Scriptable Audio Pipeline (Burst DSP processors). | Both were evaluated; see §2. |

---

## 2. Key decisions

| Decision | Choice | Why | Rejected alternative |
|---|---|---|---|
| Engine | **Native Unity audio and AudioMixer** | It covers everything the game needs: randomised one-shots, layered loops, snapshots, sidechain ducking and exposed parameters. No middleware, build steps or second editor are involved. | **FMOD Studio** is free for indies, according to [FMOD licensing](https://fmod.com/licensing) (small-budget tier). Its adaptive-music tooling is better, but it's a second toolchain for a solo project. §3.4 keeps a seam so a move later only touches the playback layer. |
| Sound definition | **Our own `SoundEvent` ScriptableObject** | It needs per-event cooldowns, voice limits, priorities, a mixer group, a hearing range, layered child events and no-repeat shuffles. All runtime state is kept outside the asset. | **The Audio Random Container** needs an AudioSource of its own per container. Unity's audio team has said you can't read or set its values at runtime ([forum](https://discussions.unity.com/t/modifying-and-getting-audiorandomcontainer-data-at-runtime/1562205)), so you can't pull a clip out of it to play through a shared pool. It also has no cooldowns or voice caps. It doesn't fit a pooled design. |
| Spatialisation | **Custom 2D attenuation and pan** computed from the camera's view (§3.3); every AudioSource is 2D (`spatialBlend = 0`) | It works with an orthographic camera at z = −10, it's aware of zoom, the pan follows screen position, and the curves are fully ours. | Unity 3D audio with the listener moved to z = 0: zoom is ignored, the Rolloff Scale is global and set in the project settings, and its falloff curves are tuned for 3D games. |
| Where sound calls live | **Audio reactor components** (`CreatureAudio`, `FireAudio`, ...) listen to gameplay events. Gameplay scripts never reference a `SoundEvent`. | It matches the project's reactor architecture, keeps gameplay testable, and deleting a component mutes that entity. It's one place to tune per entity type. | `[SerializeField] SoundEvent` plus `Play` calls inside gameplay scripts scatter audio decisions across about 60 files. |
| UI exception | UI scripts call `UiSfx.Play(UiSound.X)` directly: one static call with an enum key | Accept or deny outcomes are decided inside each UI handler. Routing them through events would double the code for no gain. | — |
| Lifetime | **A persistent `AudioSystem`**, created by `[RuntimeInitializeOnLoadMethod]` in code (only its `AudioConfig` lives in `Resources`) | It always exists, in any scene, with no setup. It survives "Quit to menu", so music and the UI keep playing. | A scene object with the DontDestroyOnLoad and destroy-duplicate pattern works too, but it relies on someone remembering to place it. |
| Time | `Time.unscaledTime` for cooldowns, schedulers and fades | Pause, menus and hit-stop all set timeScale to 0. | `Time.time`. |
| Ambition | No Scriptable Audio Pipeline, no HRTF, no occlusion, no reverb zones in v1 | Not needed for a 2D isometric game. Effort goes into content and mix. | — |

### Where this plan departs from the notes you gave

The notes are a good base: ScriptableObject definitions, pooling, a mixer with exposed volumes, the log10 slider formula, mono world SFX, jitter and 2D UI are all kept. These points change:

1. **Cooldown state on the ScriptableObject (`_lastPlayTime`) is a real bug in the editor.**
   - The asset keeps that field between Play sessions, but `Time.time` restarts at 0.
   - So after the first session, `Time.time - _lastPlayTime` is negative. That's below the cooldown, so the sound is **silenced until the clock catches up** with the old value.
   - The fix: the asset is pure data, and all runtime state (last play time, shuffle bag, live voices) lives in `AudioSystem` and is cleared on start.
2. **`Time.time` stops at timeScale 0.** In the pause menu every UI click after the first would hit the cooldown forever. Use unscaled time.
3. **One shared 2D AudioSource with `PlayOneShot`:**
   - Setting `source.pitch` before `PlayOneShot` changes the pitch of *every* one-shot still ringing on that source.
   - One-shots can't be stopped, counted or voice-limited individually.
   - So 2D sounds use pooled voices too.
4. **`spatialBlend 0.5–1` with the Unity listener:** see finding 3. The game also has no listener at all.
5. **The slash example plays 2D.** World sounds (a slash, a hit) should be positional, or an off-screen skeleton swing sounds like it's in your ear.
6. **Mixer snapshots and exposed parameters conflict.** Once code calls `SetFloat` on an exposed parameter, snapshots stop driving that parameter. Settings volumes and snapshots must therefore drive *different* controls (§3.6).
7. **`AudioMixer.SetFloat` in `Awake` gets reset before `Start`.** Unity marks this behaviour "By Design" ([issue tracker](https://issuetracker.unity3d.com/issues/audiomixer-values-get-reset-between-awake-and-start)). Apply saved volumes in `Start`.

---

## 3. The system

### 3.1 Overview

```
 Gameplay (unchanged logic, raises facts)          Audio layer (new)
 ───────────────────────────────────────           ─────────────────────────────────────────────
 Health.Damaged ─────────────┐                     *Audio reactors (on prefabs)
 EnemyBrain.StateChanged ────┤                       CreatureAudio, FootstepAudio, SurfaceAudio,
 WeaponUse.Swung ────────────┼──────────────────▶    WeaponAudio, FireAudio, DestructibleAudio, ...
 Fuel.FuelAdded ─────────────┤                       decide WHICH SoundEvent and WHERE
 ItemEvents.PickedUp (static)┘                                 │
 UI handlers ──── UiSfx.Play(UiSound.Place) ───────────────────┤
                                                               ▼
                                                     Sfx  (static facade, engine-agnostic)
                                                               │
                                                               ▼
 Directors (persistent, on AudioSystem):            AudioSystem (persistent, bootstrapped)
   MusicDirector  ← GameState, Combat, Night,         ├─ voice pool (one-shots)   ── AudioSources
                    Freezing, Low fire                ├─ loop handles (emitters)   (spatialBlend 0,
   AmbienceDirector ← DayNightCycle, Heat             ├─ AudioSpace: volume/pan     routed to
   MixDirector    ← GameState, Freezing, Health       │  from camera view           mixer groups)
   ItemAudioDirector ← ItemEvents                     └─ per-event runtime state
                                                               │
                                                               ▼
                                                     AudioMixer: groups · snapshots · ducking
```

### 3.2 `SoundEvent` (ScriptableObject, data only)

`Create → Audio → Sound Event`. One asset per sound.md ID.

| Field | Type / default | Purpose |
|---|---|---|
| `clips` | `AudioClip[]` | The variants. |
| `selection` | `ShuffleNoRepeat` (default) / `Random` / `Sequential` | A shuffle bag: every variant plays before any repeats, and never the same one twice in a row. |
| `volumeDb` | `0` dB | Base level. Authored in dB, because levels are compared in dB. |
| `volumeJitterDb` | `1.5` | ± random level per play. |
| `pitchJitterSemitones` | `0.8` (about ±5 %) | ± random pitch. Semitones are perceptually even, so +0.8 and −0.8 sound like the same amount of change. |
| `output` | `AudioMixerGroup` | Which bus it goes through. |
| `space` | `World` / `Screen` | World: attenuated and panned from its position. Screen: non-positional (UI, player status, stingers). |
| `rangeScale` | `1` | Multiplies the hearing radius. Footsteps 0.6, tree fell 1.6, skeleton spawn 1.8. |
| `loop` | `false` | True for loops (used through `PlayLoop`). |
| `cooldown` | `0.04` s (unscaled) | The minimum gap between starts of this event. A cleave hitting 6 targets plays about 2 impacts, not 6. |
| `maxVoices` | `4` | The cap on concurrent voices of this event. |
| `steal` | `Oldest` / `Quietest` / `None` | What happens when that cap is hit (`None`: refuse the new voice). |
| `priority` | `Low` / `Normal` / `High` / `Critical` | Pool stealing order. Also maps to `AudioSource.priority`. |
| `ignorePause` | `false` | Keeps playing while paused (UI, music, menu stingers). |
| `layers` | `SoundEvent[]` | Child events started together, e.g. hurt = voice + flesh thump, heavy step = step + load creak. Each child keeps its own cooldown, jitter and group. |
| `startDelay` | `0` (± range) | Small offsets, e.g. an item's land sound 0.12 s after the loot pop. |

An empty `clips` array is legal and plays silence. The validator lists those (§7), so the whole event map can be wired before any audio exists.

### 3.3 `AudioSpace`: 2D attenuation and pan

The listener point is the camera's XY. `halfW = cam.orthographicSize × cam.aspect` is half the visible width in world units.

```
R      = max(R_min, k · halfW) · event.rangeScale        // hearing radius; k ≈ 1.6, R_min ≈ 6
d      = |source.xy − camera.xy|
gain   = falloffCurve(d / R)                             // 1 inside ~0.25R, eased to 0 at R
pan    = clamp((source.x − camera.x) / halfW, −1, 1) · panWidth   // panWidth ≈ 0.55
```

- **Zoom-aware:** zooming out shows more of the world, and more of it becomes audible.
- **Just off-screen is still faint but audible** (k > 1). A skeleton rising behind the screen edge is a tell.
- **The pan is gentle** (0.55). Pixel-art speakers-wide panning feels cheap, so the pan only suggests left or right.
- **Culling:** a World one-shot whose starting gain is below −40 dB isn't started, so it doesn't use a voice.
- Positional voices are updated in **one** `LateUpdate` loop over the live voices (≤ 28). A voice can follow a `Transform` (an arrow, a thrown axe, a creature's voice) or stay at a point.
- `falloffCurve`, `k`, `R_min` and `panWidth` live in `AudioConfig`, so they can be tuned live.

The Unity `AudioListener` still has to exist (no listener means no output). It goes on the AudioSystem object. Its position is irrelevant, because every source is 2D.

### 3.4 `Sfx`: the only API the game calls

```csharp
public static class Sfx
{
    // one-shots
    public static SoundHandle Play(SoundEvent e, Vector2 at, float volume = 1f, float pitch = 1f);
    public static SoundHandle Play(SoundEvent e, Transform follow, float volume = 1f, float pitch = 1f);
    public static SoundHandle Play2D(SoundEvent e, float volume = 1f, float pitch = 1f);

    // loops: the caller keeps the handle and must Stop it. playhead < 0 = a random
    // start point; layers that must stay in sync pass the same value.
    public static SoundHandle PlayLoop(SoundEvent e, Transform follow, float volume = 1f, float playhead = -1f);
    public static SoundHandle PlayLoopAt(SoundEvent e, Vector2 at, float volume = 1f, float playhead = -1f);
}

public readonly struct SoundHandle   // by generation: stale once the voice ends, is stolen or stopped
{
    public bool IsValid { get; }      // a virtual (out-of-hearing) loop is still valid
    public bool IsAudible { get; }
    public void SetVolume(float volume, float fade = 0.1f);
    public void SetPitch(float pitch);
    public void MoveTo(Vector2 point);
    public void Stop(float fade = 0.1f);
}
```

- `volume` and `pitch` scale on top of the event's own jitter. Uses include charge amount on a bow release, a pitch-up per combo step, and build deposits filling up.
- **No-op safe:** with no AudioSystem (edit mode, a unit test, a scene being torn down) every call quietly does nothing.
- **Engine seam:** only `AudioSystem` touches `AudioSource`. Swapping to FMOD later would replace `SoundEvent` with a wrapper around an FMOD event reference, plus the system internals. The `*Audio` reactors and the call sites stay as they are.

### 3.5 `AudioSystem`: voices, loops, state

- **Bootstrap:** `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` creates an "Audio System" GameObject in code, marked DontDestroyOnLoad. It holds:
  - the `AudioListener`;
  - the `AudioConfig` (loaded from `Assets/Audio/Resources/AudioConfig.asset`), and through it the mixer;
  - the voice pool;
  - later, the directors (Music, Ambience, Mix, ItemAudio), added as components.
- **Voice pool:** 28 pre-made child GameObjects with an AudioSource each (`spatialBlend 0`, `playOnAwake false`). They're created once, so playing allocates nothing.
- **Starting a voice:** cooldown check → voice-cap check → cull check → pick a clip from the shuffle bag → apply jitter → choose the mixer group → `Play()`.
- **Stealing when full:** lowest priority first, then the quietest current gain, then the oldest. If every live voice has higher priority than the new one, the new one is dropped.
- **Per-event runtime state:** `Dictionary<SoundEvent, EventState>`, holding the last start (unscaled), the shuffle bag and the live voice list. It's cleared when the system starts, so nothing leaks between editor Play sessions.
- **Loops:** `PlayLoop` gives the emitter a dedicated pooled loop source (a separate pool of 16). Every loop is positional through AudioSpace.
  - Loops beyond 1.2 R are **virtualised**: the source goes back to the pool, and the loop's own playhead keeps advancing.
  - When back in range, it takes a source again and resumes where the playhead is, so it never sounds restarted.
  - So 6 campfires or 20 torches don't eat voices.
- **Pause:**
  - On `GameStateManager.StateChanged`, `AudioListener.pause = state is Paused`. Every source with `ignorePause` (UI, music, menu ambience) has `ignoreListenerPause = true`, so it keeps going.
  - Game Over and Won don't pause; the End snapshot fades the world instead (§3.6).
  - Main Menu uses the Menu snapshot.
- **Scene unload:** on `SceneManager.sceneUnloaded`, stop and release every World voice and loop whose follow target was in that scene. Music and UI continue. A `Sfx` call made while the scene is tearing down (`OnDisable` cascades) is ignored. That stops a burst of extinguish and death sounds on "Quit to menu".
- **Budget guard:** at most 10 new voices per frame. The rest are dropped, lowest priority first. This catches pathological frames, such as a Whirlwind hitting 8 targets while 3 items land.

### 3.6 The AudioMixer: `Assets/Audio/Mixer/FireGame.mixer`

```
Master                         [exposed: MasterVolume]
├── Music                      [exposed: MusicVolume]   · Duck Volume ◀ (Combat, Stingers)
├── Ambience                   [exposed: AmbienceVolume]· Duck Volume ◀ (Combat, Stingers) · Lowpass (heat)
├── SFX                        [exposed: SfxVolume]
│   ├── Player                 (voice, body, footsteps, eating)
│   ├── Combat                 (swings, impacts, skills)        · Send ▶ Music/Ambience duck
│   ├── Creatures              (voices, creature steps)
│   └── World                  (fire, stations, props, items, building)
├── Stingers                   (time of day, level-up, game over / won)  · Send ▶ Music duck
└── UI                         [exposed: UiVolume]
```

- **Settings own the five exposed group volumes:** Master, Music, Ambience, SFX and UI. Snapshots **never** touch those.
- **Snapshots own everything else:** the child-group volumes, the lowpass cutoffs and the duck thresholds. This avoids the exposed-parameter-vs-snapshot conflict (§2, point 6).

| Snapshot | When | Changes |
|---|---|---|
| `Gameplay` | Playing | Neutral. |
| `Paused` | Paused | Music −6 dB with a lowpass at 1.2 kHz; Ambience −8 dB. The world is frozen by `AudioListener.pause`. |
| `Menu` | Main Menu | Player, Combat, Creatures and World at −80 dB; Ambience −10 dB with a lowpass. |
| `End` | Game Over / Won | Player, Combat, Creatures and World fade to −80 dB over 1.5 s; Ambience −12 dB. Music and Stingers are untouched. |

- **Ducking:** a Duck Volume effect on Music and Ambience, fed by Sends from Combat and Stingers. Threshold about −25 dB, ratio about 3, attack 10 ms, release 400 ms, giving a 3–4 dB dip.
  - Big hits punch through without raising the overall volume.
  - A night sting clears the room for itself.
- **Continuous mix** (`MixDirector`, through a few extra exposed parameters that snapshots never touch):
  - `AmbienceHeatLowpass`: the closer the player is to a `HeatSource`, the more the wind is softened. You *hear* shelter.
  - `WorldFreezeHighpass`: while freezing, the world thins out slightly, as a danger cue.
  - `SfxLowHealthLowpass`: below 25 % health, a gentle lowpass on SFX (not on UI) sits under the heartbeat.
- **No mixer API:** Unity has no public API for creating mixer groups or effects, so the mixer is built by hand. Phase 1 includes a checklist that takes about 15 minutes.

### 3.7 Settings

- A `AudioSettingsStore` holds linear 0..1 values in `PlayerPrefs`.
  - Conversion: `dB = v ≤ 0.0001 ? −80 : 20·log10(v)`.
  - Values are applied in **`Start`**, not `Awake` (§2, point 7).
- The pause panel gets an **Audio** section with five sliders (Master, Music, Ambience, SFX, UI) and a "Mute when unfocused" toggle (`OnApplicationFocus` sets `AudioListener.pause`, unless the toggle is off).
- Dragging a slider plays `ui_slider_tick` (throttled) at the slider's level, so you hear the level you're setting. (It plays on the UI bus: a handle can't move to another bus, and the level already tells it.)

### 3.8 Import rules: `AudioImportRules : AssetPostprocessor`

`OnPreprocessAudio` sets the import settings by folder, so nobody tunes clips by hand.

| Folder | Mono | Load type | Format | Notes |
|---|---|---|---|---|
| `Audio/Clips/SFX/**` (one-shots) | ✔ | Decompress On Load | Vorbis 70 % | Instant start, no decode hitch. |
| `Audio/Clips/UI/**` | ✔ | Decompress On Load | Vorbis 70 % | |
| `Audio/Clips/SFX/Loops/**` | ✔ | Compressed In Memory | Vorbis 60 % | Fire layers, torch, burning, ice slide, heartbeat. |
| `Audio/Clips/Ambience/Beds/**` | ✘ (stereo) | Streaming | Vorbis 55 % | Wind beds. |
| `Audio/Clips/Ambience/OneShots/**` | ✔ | Compressed In Memory | Vorbis 60 % | Gusts, birds, howls. |
| `Audio/Clips/Music/**` | ✘ | Streaming | Vorbis 60 % | Plus Load In Background. |

- **Memory estimate:** about 900 one-shots at an average of 0.6 s, mono, 48 kHz, decompressed (about 58 KB each), comes to roughly 50 MB at full content. That's fine on PC.
  - If it ever matters, switch short SFX to ADPCM + Compressed In Memory (about 3.5× smaller, cheap to decode).
- The postprocessor also warns about:
  - clips with more than 10 ms of leading silence (latency the player feels);
  - stereo files in mono folders;
  - looping clips whose first and last samples don't match (a click at the loop point).

### 3.9 Project settings (Phase 1)

- **Real voices: 48.** The pool is 28 one-shots + 16 loops + music + ambience; Unity virtualises anything beyond that by priority.
- **DSP buffer:** "Good latency" (512). It's about 11 ms at 48 kHz, so swings and impacts land with the frame.
- **Doppler:** 0.
- **Default speaker mode:** stereo.

### 3.10 Content layout and naming

```
Assets/Audio/
  Mixer/FireGame.mixer
  Resources/AudioConfig.asset
  Events/<sound.md section>/sfx_player_step_snow.asset ...      (SoundEvent, one per ID)
  Sets/  GroundSounds, FootstepProfile_*, CreatureVoice_*, SurfaceSounds_*, ItemSounds_*,
         ToolSounds_*, FireSoundSet_*, UiSoundKit, MusicSet, AmbienceSet
  Clips/SFX/{Player,Combat,Creatures/<Name>,World,Items,Skills}/  sfx_<id>_01.wav ...
  Clips/_Placeholder/...   (generated stand-ins, deleted as real clips arrive)
  Clips/SFX/Loops/...   Clips/UI/...   Clips/Ambience/{Beds,OneShots}/...   Clips/Music/...
  LICENSES.csv
```

File names are the sound.md ID plus `_NN`. The SoundEvent asset carries the ID with no suffix.

---

## 4. Data sets (ScriptableObjects that group events)

These keep the reactor components small and let many prefabs share one tuning.

| Set | Holds | Shared by |
|---|---|---|
| `GroundSounds` (one, in AudioConfig) | `GroundType` → step event, a soft land layer (snow puff), and the slide loop for Ice | Every footstep, landing and knockback slide |
| `FootstepProfile` | foot layer (none / bone clack / paw / hoof / heavy paw), ground-layer volume (dB), ground-layer `rangeScale`, carry layer (player only) | Player, Skeleton, Wolf, Boar, Deer, Polar Bear |
| `CreatureVoice` | spawn, idle (+ interval range), aggro, attack windup, attack swing, attack hit (bite), hurt, hurt-while-burning, death, flee, flee loop, chase loop, despawn | One per creature type |
| `SurfaceSounds` | impact, heavy impact (optional), glance knock, crit layer override (optional) | Flesh, Bone, Player, Wood_Living, Wood_Log, Stone, IronOre, Straw, Shield (sound.md §13) |
| `ItemSounds` | pickup, drop/land, equip, unequip, use (eat) | The material sets in sound.md §7.1. **`ItemDefinition` gets a `sounds` field**; items with none fall back to the "generic" set. |
| `ToolSounds` | swing, swing heavy, charge loop, charge ready, glance, draw, stow, plus held loop (torch) | Fists, Stone Axe, Stone Pickaxe, Iron Sword, Torch, Sap Torch, Bow, Shield |
| `FireSoundSet` | layers [event + fuel window + curve], feed per `FuelType`, stage up, low (threshold + hysteresis), out, relight, pop (event + interval by fuel), start (ignite) | Campfire, Furnace, Torch, Sap Torch, BurningEffect |
| `UiSoundKit` | `UiSound` enum → event (Click, Hover, Denied, Pickup, Place, Swap, Return, Toss, Open, Close, Tab, Subtab, SliderTick, ...) | All UI |
| `WorldSounds` (one, on AudioConfig) | The item-handling layers (pocket, auto-pickup, lift, throw, spill, bag swap, loot pop, a fallback `ItemSounds`) and the building sounds | `ItemAudioDirector`, `BuildAudio`, `LootAudio` |
| `MusicSet` / `AmbienceSet` | Stems, beds, schedulers, stingers | The directors |

**Footsteps are layered** (ground × foot). This cuts asset count and keeps every creature consistent on every surface: a wolf on ice sounds like *ice* plus a *paw*. The creature step rows in sound.md become foot layers only (§10.3).

---

## 5. Audio components: what plays what

| Component | Lives on | Listens to | Plays (sound.md) |
|---|---|---|---|
| `PlayerAudio` | Player | `Health.Damaged`, `Died` and `Healed`; `PlayerTemperature.FreezingChanged`*, `ColdOverflow`, `WarmedUp`*; `PlayerExperience.LeveledUp`, `Awarded`*; `WeaponExperience.LeveledUp`*; `PlayerBuffs.Added`* and `Expired`*; `IKillReactor` (kill confirm); the health ratio (heartbeat loop) | §3.2, §3.6, kill confirm (§4.1) |
| `FootstepAudio` | Player, every creature | the `OnFootstep` Animation Event (or **stride mode**: every N units moved, for entities without step frames); `GroundSensor.Ground`; `Hands.IsCarrying` | §3.1, the creature steps in §6; ice slide loop (player) |
| `WeaponAudio` | Player | `WeaponUse.SwingArmed`*, `ChargeReady`*, `Released`*, `Swung`*, `Landed`* (hits / glances / misses); `ShieldBlock.BlockingChanged`* and `Blocked`*; `Loadout.Swapped`*; `Hands.CarryingChanged`* (stow / draw); `ActiveSkillSlots.CastFailed`* and `CooldownReady`*. It reads the active hand item's `ToolAudio`. | §4.1–4.4, §5 shared rows |
| `ToolAudio` | Each tool prefab | — (data holder: `ToolSounds`) | — |
| `InteractionAudio` | Player | `PlayerInteractor.HoldProgress`* (Hold E loop), `Thrown`*, `Fumbled`*, `DropGhostShown`* | §3.3–3.5 (the item-specific parts go through `ItemAudioDirector`) |
| `ItemAudioDirector` | AudioSystem | the static `ItemEvents`*: `PickedUp(item, auto)`, `Landed(item, pos)`, `Spilled`, `Worn` / `Removed`, `Eaten` | §7.1, §7.4, §3.6 eating; "into pocket" and auto-pickup layers |
| `SurfaceAudio` | Everything with a Hurtbox: creatures, the player, trees, ore, logs, the big stone, the dummy | `IHitReactor.OnHit`. It plays the impact, or the glance knock when `ToolDamageFilter` rejects `hit.kind`. Secondaries play −6 dB. A crit adds the crit layer. | §13, `sfx_combat_crit`, the glance halves of §9 |
| `CreatureAudio` | Skeleton, Wolf, Boar, Deer, Polar Bear | `EnemyBrain.StateChanged`* (aggro, flee); `EnemyAnimator.SpawnStarted`*; `EnemyAttacker.SwingStarted`*, `Swung`*, `Struck`*; `Health.Damaged` and `Died`; `EnemyDespawner.Despawned`; `Knockback.Applied`*; idle timer (with the global bark budget) | §6.1–6.5, §6.7 knockback |
| `FlammableAudio` | Every `Flammable` | `Flammable.Ignited`*, `Ticked`*, `Extinguished`* | §6.7 ignite, the burning loop, tick, extinguish |
| `DestructibleAudio` | Trees, ore, logs, the big stone, the dummy | `OnHit` (shake / reaction), `Health.Damaged` (low-health creak, once), `Health.Died` (fell / break / split) | §7.3, §9.1, §9.2, §6.6 |
| `LootAudio` | Anything with `DropOnHit` / `DropOnDeath` | `DropOnHit.Dropped`*, `DropOnDeath.Dropped`* | `sfx_item_loot_pop`, then the item's land sound (delayed) |
| `CollideAudio` | Berry bushes, Hemp Plant, logs, the big stone | Its own `OnTriggerEnter2D` (the same layer mask as `CollideAnimation`), a per-instance cooldown of 0.5 s, and only bodies actually moving | §9.3 rustles, §7.3 bumps |
| `HarvestAudio` | Hemp Plant | `Harvestable.Harvested`*, `Regrown`* | §9.3 harvest / regrow |
| `FireAudio` | Campfire, Furnace, Torch, Sap Torch | It polls `Fuel.fuelLevel` each frame (layer crossfade, low / out / relight edges with hysteresis, pop scheduler); `Fuel.FuelAdded(amount, item)`; for torches, its own `OnEnable` / `OnDisable` (stowed or bagged means silent) and its movement speed | §8.1 loops / feed / low / out / relight, §8.2, §4.3 |
| `ProcessorAudio` | Campfire, Furnace | `ItemProcessor.JobStarted`*, `JobFinished`*, `Stalled`*; `ItemReceiver.Deposited`* (input) | §8.1 cook, §8.2 smelt / bar done / input / output full |
| `StructureAudio` | Campfire, Furnace, Workbench | `PlacementGhost.Completed`* (this structure); `WorkbenchInteractable.Opened`* and `Closed`* | `*_build_complete`, §8.3 |
| `BuildAudio` | AudioSystem | the static `PlacementGhost.Began`*, `Placed`*, `Cancelled`*; `BuildRequirement.Deposited`* and `IngredientSatisfied`* | §8.4 |
| `ProjectileAudio` | Arrow | `Projectile.Launched`*, `HitSomething`*, `Expired`* (flight loop follows the arrow) | §4.2 arrow rows |
| `WhirlwindAudio` | Whirlwind skill prefab | `WhirlwindSkill.Started`*, `Spun(i)`*, `Ended`* | §5 whirlwind |
| `AxeThrowAudio` | Axe Throw skill prefab, Thrown Axe prefab | `AxeThrowSkill.WindupStarted`*, `Released`*, `Caught`*; `ThrownAxe.PhaseChanged`* (spin loop on the axe, stick, rip, turn-back) | §5 axe throw |
| `UiSfx` + `UiSoundKit` | static | direct calls from UI scripts (§8) | §10 |
| `AmbienceDirector` | AudioSystem | `DayNightCycle.Hour` (bed crossfade), schedulers (gust, creak near trees, birds at dawn, owl / howl at night), heat around the player | §11.1 |
| `MusicDirector` | AudioSystem | `GameStateManager.StateChanged`*, `CombatState.Changed`, `DayNightCycle.NightChanged` / `PhaseChanged`, `PlayerTemperature.FreezingChanged`*, the nearest campfire's fuel | §11.2, §11.3 |
| `MixDirector` | AudioSystem | `StateChanged`* (snapshots), heat, freezing, health (exposed params) | §3.6 |

`*` = a new event, listed in §6.

**Bark budget** (shared by every `CreatureAudio`):
- at most one idle bark per 1.2 s across the whole world;
- at most one per creature type per 3 s;
- only from creatures within hearing radius.

Six wolves wandering together sound like a pack, not a choir.

---

## 6. Gameplay hooks (as built in Phase 2)

Each hook is a C# event raised where the fact already happens. There's no behaviour change, and most are 1–3 lines. The rule: **gameplay raises facts, audio decides what they sound like.** Instance events live on the component; static ones are for things created at runtime that a prefab's audio component can't reach (build sites, the ghost, items).

| Script | Events | Raised in |
|---|---|---|
| `GameStateManager` | `static StateChanged(GameState)` | `SetState` (Phase 1) |
| `WeaponUse` | `SwingArmed(HandSide, Tool)`, `ChargeReady(Tool)`, `Released(Tool, bool heavy, float charge)`, `Swung(Tool, bool heavy)`, `Landed(Tool, StrikeResult)`, `Fired(Tool, float charge)` | `TryUse`; the first frame the hold reaches `chargeTime` (where the camera zooms); `ResolveCharge`; `OnAttackSwing`; `OnAttackHit` (melee / ranged) |
| `MeleeWeapon` | `Strike` returns `StrikeResult { hits, glances, Missed }` instead of an `int` | `Strike`; callers adjusted |
| `ShieldBlock` | `BlockingChanged(bool)`, `Blocked(HitInfo)` | The `IsBlocking` edge in `Update`; `Health.OnHit` reports a blocked hit |
| `Loadout` | `Swapped(int set)` | `SetActiveSet` |
| `Hands` | `CarryingChanged(bool)` | `Refresh`, on the `IsCarrying` edge |
| `ItemEvents` (new, static) | `PickedUp(ItemDefinition, Vector3, bool auto)`, `Landed(ItemDefinition, Vector3)`, `Thrown`, `Spilled(ItemDefinition, Vector3, int)`, `Worn(GameObject)`, `Removed`, `Wielded`, `Eaten` | `Pickupable.Interact` (E) and `Inventory.Update` (auto top-up); `Hands.Place` / `Inventory.Place` / `ThrownItem.Land`; `ThrownItem.Launch` (not fumbles); `Inventory.Spill`; `Equipment.Wear` / `Unequip`; `Inventory.EquipToLoadout` / `TryEquipFromWorld`; `FoodUse.TryEat`. Items are passed as definition + position, since an auto-pickup destroys the world item. |
| `PlayerInteractor` | `float HoldProgress`, `Fumbled(WorldItem)`, `DropGhostShown` | `UpdateE` / `EndHold`, `Fumble`, `UpdateQ` |
| `Quickslot` | `EatFailed` | `TryEat` returning false |
| `PlayerTemperature` | `bool IsFreezing` (new; it was only mentioned in a comment), `FreezingChanged(bool)` | The edge at the end of `Update`. Warming back up is `FreezingChanged(false)`, so there's no separate `WarmedUp`. |
| `PlayerExperience` | `Awarded(int xp, Vector3 at)` | `OnKilled`, next to the "+N" |
| `WeaponExperience` | `LeveledUp(ToolKind, int level)` | `Add`, when the track gains a level |
| `PlayerBuffs` | `Added`, `Expired` | `Apply*Buff`; the expiry in `Update` |
| `EnemyBrain` | `StateChanged(State from, State to)` | One `SetState` used by `Aggro`, flee begin and flee done |
| `EnemyAttacker` | `SwingStarted`, `Swung`, `Struck(bool landed)` | Arming in `Update`, `OnAttackSwing`, `OnAttackHit` |
| `EnemyAnimator` | `SpawnStarted` | `Start`, when a spawn state exists (after every Awake/OnEnable, so a listener on the enemy hears it) |
| `Knockback` | `Applied(Vector2 impulse)` | `Apply` |
| `Flammable` | `Ignited(bool fresh)`, `Ticked`, `Extinguished` | `Ignite` (fresh = it wasn't burning), the tick in `Update`, `Extinguish` (only if it was burning) |
| `Fuel` | **changed:** `FuelAdded(float amount, ItemDefinition item)` | `Add`; `FireLight`'s handler updated |
| `ItemProcessor` | `WorkingChanged(bool)`, `JobFinished(ItemDefinition)`, `Stalled` | `Update`. `WorkingChanged` replaces the planned `JobStarted`: the cook / smelt loop needs on and off. |
| `ItemReceiver` | `Deposited(ItemDefinition)`, `static AnyDeposited(ItemReceiver, ItemDefinition)` | Each `Deposit` success (`FuelReceiver`, `ProcessorInputReceiver`, `BuildRequirement`) |
| `BuildRequirement` | `static IngredientSatisfied(BuildRequirement, ItemDefinition)` | `Deposit`, when an ingredient reaches its amount |
| `PlacementGhost` | `static Began(PlacementGhost)`, `Placed(PlacementGhost)`, `Cancelled(Vector3)`, `Completed(GameObject)` | `Init`, `Place`, `Cancel`, `Complete` |
| `CraftingController` | `Crafted(Recipe)`, `CraftFailed(Recipe)` | `TryCraft` (a Placeable recipe raises `PlacementGhost.Began` instead of `Crafted`) |
| `Harvestable` | `Harvested`, `Regrown` | `Interact`, the regrow in `Update` |
| `DropOnHit` / `DropOnDeath` | `Dropped(Vector3 at, int count)` | After the loot is spawned |
| `Projectile` | `Launched`, `HitSomething(Vector2)`, `Expired(Vector2)` | `Launch`, `OnTriggerEnter2D`, the out-of-range despawn |
| `WhirlwindSkill` | `Started`, `Spun(int turn, StrikeResult)`, `Ended` | `TryActivate`, each `OnAnimationEvent` turn, the last turn or an interrupted spin |
| `AxeThrowSkill` | `WindupStarted`, `Released(ThrownAxe)`, `CaughtBack(bool inHand)` | `TryActivate`, the let-go, `Caught` |
| `ThrownAxe` | `Stuck(Vector2)`, `Returning(bool fromTarget)` | `Hit`, `TurnBack` |
| `ActiveSkillSlots` | `CastFailed(int slot)`, `CooldownReady(IActiveSkill)` | `Cast`; the cooldown edge in `Update` |
| `StationInteractable` | `PanelOpened`, `PanelClosed` | `Open`; `Close` / `Closed` (only when it was open) |
| `WorkbenchInteractable` | `Opened`, `Closed` | `Interact` |
| `HudMenu` | `Opened`, `Closed` | `Open` / `OpenCrafting` / `Close`, on the edge |

Not needed: `Health`, `CombatState`, `DayNightCycle` and `EnemyDespawner` already have what's required. `CollideAnimation` stays untouched, because `CollideAudio` gets its own trigger callbacks on the same object.

## 7. Tooling (editor)

| Tool | What it does |
|---|---|
| **`Tools/Audio/Sync Events From sound.md`** | Parses every ID in sound.md's tables. It creates any missing `SoundEvent` assets under `Events/<section>/` with sensible defaults per section: mixer group, space, `rangeScale`, cooldown, priority, and `loop` for rows typed `loop` / `layer`. It reports assets whose ID no longer exists in sound.md. sound.md stays the single source of truth for *what exists*. |
| **`Tools/Audio/Auto-Fill Clips`** | Assigns `Clips/**/<id>_NN.wav` files to the matching event by name. Dropping files into the folder is enough. |
| **SoundEvent inspector** | ▶ Play / ■ Stop preview, with jitter, in edit mode. It shows clip count, total length, peak and loudness per clip, and warns about mismatched variant levels (more than 2 dB apart). |
| **`Tools/Audio/Validate`** | It reports:<ul><li>events with no clips, grouped by priority (P1 first);</li><li>clips not referenced by any event;</li><li>clips breaking the import rules;</li><li>reactors with missing set references;</li><li>prefabs with a Hurtbox but no `SurfaceAudio`;</li><li>Animation Events (`OnFootstep`) on clips whose GameObject has no `FootstepAudio`, which would cause Unity's "AnimationEvent has no receiver" errors;</li><li>a coverage table per sound.md section.</li></ul> |
| **`Tools/Audio/Bake Footstep Events`** | Writes `OnFootstep` events onto the player's run clips at the contact frames from a `FootstepFrames` asset (clip → frame indices). Idempotent: it removes old `OnFootstep` events first and keeps every other event. |
| **`EnemyArtSetup` integration** | `Spec` gains `stepFrames` per tag (run, walk). `CopyClips` writes `OnFootstep` events next to the bite events, where it currently writes an empty array. Each Build adds `CreatureAudio` (with that spec's `CreatureVoice`), `FootstepAudio`, `SurfaceAudio`, `FlammableAudio` and a `LootAudio` where there's loot. |
| **`Tools/Audio/Add Audio To Prefabs`** | One-shot setup. It adds the right reactors and sets to the existing prefabs:<ul><li>trees, ore, logs and stone (Surface, Destructible, Loot, Collide);</li><li>bushes and hemp;</li><li>stations, tools, the Arrow, the skills, BurningEffect and the Player.</li></ul>It's idempotent, the same style as the other setup tools. |
| **`AudioDebugOverlay`** (runtime, F9) | Lists live voices (event, group, gain, pan, follow target), loops (audible or virtual), steals and drops per second, and the pause state. It draws the hearing radius as a ring around the view's centre. Test keys play `AudioConfig.debugTestEvents` at the cursor. Self-contained like `CombatDebug`: delete the file to remove it. (Unity has no runtime API for mixer bus meters; use the Audio Mixer window's meters in play mode.) |

---

## 8. UI call sites

`UiSfx.Play(UiSound)` is called from these handlers. The kit maps each to a sound.md §10.1 event. Item- and fire-specific outcomes call the domain sound instead, through the same `ItemSounds` / `FireAudio` paths, so a drag-feed into the campfire sounds exactly like a Q deposit.

| Script | Calls |
|---|---|
| `HudMenu` | Open, Close (F, Esc, workbench) |
| `TabButtonUI` | Tab / Subtab on a click; Denied when disabled |
| `BagDrag` / `DragIconUI` | Pickup on drag start; Return when unclaimed over UI; Toss when released into the world |
| `BagSlotUI` | Place / Swap / merge-pitch on `OnDrop`; feeding a torch plays the fire's feed sound (via `FuelReceiver` → `Fuel.FuelAdded`); Denied when the drop is refused |
| `BagTabUI` / `Inventory.UseSlot` outcome | Wield → `ToolSounds.draw`; wear → `ItemSounds.equip`; eat → `ItemSounds.use`; no use → nothing |
| `EquipSlotUI` / `EquipmentTabUI` | Wear / remove through `ItemEvents.Worn` / `Removed`; Denied for wrong gear or a full bag |
| `LoadoutSlotUI` | Place / Swap / draw; Denied |
| `ActionSlotUI` / `ActionBarUI` | Spell assign (a magic Place), Swap, Return; hand-slot drag Pickup |
| `QuickslotUI` | Place on assign; eat through `ItemEvents.Eaten`; Denied when empty |
| `BlueprintCellUI` / `CraftingTabUI` | Hover (quiet); Denied on a dimmed click; success through `CraftingController.Crafted` (the craft sound per recipe category, from a `craftSound` field on `Recipe`) |
| `SkillNodeUI` / `SkillTreeUI` | Rank-up (pitch by rank), maxed, spell learned, Denied when locked or with no points |
| `StationPanelUI` / `StationSlotUI` | Open / Close; drops route to the station's own sounds; Denied when refused; take output → item pickup |
| Pause, main, game over and won panels | Click on every button; Hover (quiet); SliderTick in Audio settings |

---

## 9. Removal of the old sfx code

| Remove | Where |
|---|---|
| The `public AudioClip sfx` field and its tooltip | [HitEffect.cs](Assets/Scripts/Combat/Effects/HitEffect.cs). The class comment becomes "contact VFX" only. Its data in `Blood.asset` is dropped on the next save. |
| `if (hitEffect.sfx != null) AudioSource.PlayClipAtPoint(...)` | [SurfaceHitEffect.cs:48-49](Assets/Scripts/Combat/Effects/SurfaceHitEffect.cs#L48-L49). The header comment's "and sfx" wording goes too. `SurfaceHitEffect` stays as the VFX reactor; `SurfaceAudio` is the sound reactor next to it. |
| The `Assets/Sound/` folder | `campfire.wav` (the user's file) **moves** to `Assets/Audio/Clips/SFX/Loops/Fire/` with its meta, keeping its GUID. The empty `Assets/Sound` folder is then removed. |

No other audio code exists.

---

## 10. Sound content plan

### 10.1 Delivery spec

| Property | Spec |
|---|---|
| Format | WAV, 48 kHz, 24-bit as source. Unity compresses on import. |
| Channels | Mono for everything except music and ambience beds (stereo). |
| Heads | **No leading silence** (under 5 ms). The transient must sit at sample 0. Players feel 20 ms of lateness on a swing. |
| Tails | Natural, faded to digital silence. No truncated reverb. |
| Loops | Seamless, cut at zero crossings. Fire layers are **8–20 s** each, torch, burning and slide 4–8 s, beds 60–120 s. All four campfire layers have the same length so they can run in sync. |
| Peak | −1 dBFS true peak. |
| Loudness targets (short-term LUFS, so categories sit right before mixing) | UI −24 · footsteps −28 · item handling −24 · swings −20 · impacts −16 · creature voices −18 · loops −22 · stingers −16 · music −18 (integrated) · ambience beds −30 |
| Variant consistency | Variants within ±1 dB of each other. The jitter adds the variation; mismatched sources sound like random volume bugs. |
| Names | `<sound.md ID>_NN.wav`, e.g. `sfx_player_step_snow_03.wav`. |

### 10.2 Sourcing

1. **Self-recorded foley.** This is cheap and gives the game its own identity.
   - Snow steps: cornstarch in a leather pouch, or real snow.
   - Wood: chop and split real logs.
   - Bone: snap celery, rattle dry sticks.
   - Fire: record a real campfire for the layers and pops.
   - Grunts and breaths: your own voice, pitched.
2. **Royalty-free libraries.**
   - Sonniss GDC Game Audio Bundles: free, commercial use allowed.
   - Kenney: CC0, good for UI and impacts.
   - freesound.org: **check each file's licence.** CC0 is free to use; CC-BY needs attribution; avoid CC-BY-NC.
3. **Synthesis** for UI ticks and the magic layers (spell learn, skill ready): a soft-synth plus a filter. Keep it woody, not chiptune.
4. **Music:** a commissioned composer or your own work. The adaptive design (§10.4) needs **stems**, so specify that up front.

Every file gets a row in **`Assets/Audio/LICENSES.csv`**: file, source, author, licence, URL and notes. The validator flags clips with no row.

### 10.3 Changes to sound.md that come from the system design

These were applied in Phase 1, before the first sync.

- **Footsteps become ground × foot layers** (§4).
  - `sfx_player_step_<ground>` and the new `sfx_ground_step_<ground>` collapse into one shared ground set.
  - Creature `*_step` rows become foot layers: `sfx_foot_bone`, `sfx_foot_paw`, `sfx_foot_hoof` and `sfx_foot_heavypaw`.
  - `sfx_player_step_heavy_<ground>` becomes one carry layer, `sfx_player_carry_layer`.
  - About 60 files are saved.
- **Item sounds become the §7.1 material sets** (`ItemSounds_*`). The per-action rows (`sfx_item_land_<material>`) map onto them.
- **The UI rows map onto the kit.** The per-handler rows in §10.2–10.9 mostly reuse kit entries, so only the kit plus a handful of unique sounds are produced.
- **New IDs this design needs:**
  - `sfx_ui_slider_tick`;
  - `sfx_ground_land_<ground>` (soft land layer);
  - `sfx_craft_<category>`, five of them (§10.5 of sound.md, now explicit);
  - `amb_wind_*` split into bed + gust (already there);
  - `mus_*` delivered as stems (§10.4).

### 10.4 Music deliverables (adaptive)

| Asset | Form |
|---|---|
| `mus_main_menu` | A stereo loop, 1–2 min. |
| `mus_day` | A sparse loop of 90–120 s, mostly silence with motifs. Delivered as **base stem** + **combat stem** at the same length and tempo. |
| `mus_night` | Delivered as **base drone** + **pulse stem** (rises with skeletons alive) + **combat stem** at the same length and tempo. |
| `mus_layer_freezing`, `mus_layer_lowfire` | Short loops that fit over both day and night (no tempo, textural). |
| Stingers | Dusk, night, dawn, game over, won, level-up (short). |

`MusicDirector` starts every stem of a state together with `AudioSource.PlayScheduled` on one `dspTime`, so they're sample-locked. Layers fade in and out by volume, and changing state is a 2–4 s crossfade at the next bar.

### 10.5 Production batches

| Batch | Content | Count (≈ events / files) | Ships with |
|---|---|---|---|
| **A: Core loop** | sound.md §14's first batch:<ul><li>ground steps (snow, grass, ice);</li><li>the campfire's 4 layers, feed, low, out and relight;</li><li>axe swing, heavy swing and charge-ready; glance;</li><li>the Bone, Flesh, Wood and Stone surfaces;</li><li>tree shake and fell; ore hit and break;</li><li>skeleton spawn, aggro, windup, hurt and death; wolf growl, bite and yelp; boar and deer hurt and flee;</li><li>torch loop and burnout;</li><li>wood, stick and stone item sets;</li><li>player hurt, death and freeze-start;</li><li>UI kit core; level-up;</li><li>day and night wind beds;</li><li>dusk, night and dawn stingers;</li><li>combat stem; game over and won stingers.</li></ul> | ~40 / ~140 | Phases 3–6 |
| **B: The rest of P1** | All remaining P1 rows in sound.md. | ~100 / ~310 | Phase 7 |
| **C: P2** | Polish (barks, loops, heartbeat, creaks, UI extras, layers). | ~120 / ~300 | Phase 7 |
| **D: P3** | Unplaced content (Polar Bear, Dummy, Bow, Shield, Sword) and extras. | ~70 / ~180 | When those features land |

---

## 11. Phases

Each phase ends compiling and playable, with an acceptance check. Phases 3 and 4 can swap. Phase 5 can start any time after Phase 1.

### Phase 1: Foundation (groundwork + core runtime)

The old Phase 0 (groundwork) and Phase 1 (core runtime) are one phase: the groundwork can't be tested on its own, and the runtime needs it.

**Code (done):**
1. Removed the old sfx code (§9). `campfire.wav` moved to `Assets/Audio/Clips/SFX/Loops/Fire/sfx_campfire_loop_medium_01.wav` (renamed to its ID, GUID kept); the empty `Assets/Sound` folder is gone.
2. `AudioImportRules` (§3.8); project audio settings (§3.9).
3. `SoundEvent`, `AudioConfig`, `AudioSpace`, `AudioSystem` (bootstrap, voice pool, virtual loops, per-event state, stealing, culling, frame budget, pause, scene unload, listener ownership), `Sfx` + `SoundHandle`, `AudioSettingsStore` (applied in `Start`), and the `GameStateManager.StateChanged` hook pulled forward from Phase 2.
4. Editor: **Tools/Audio/Sync Events From sound.md**, **Auto-Fill Clips**, **Validate**; the SoundEvent inspector (brief, ▶ Play / ■ Stop, Analyse); `AudioDebugOverlay` (F9).
5. sound.md amended per §10.3; placeholder clips for `sfx_ui_click`, `sfx_ui_denied`, `sfx_ground_step_snow` and `sfx_axe_swing` in `Clips/_Placeholder/`; `Assets/Audio/LICENSES.csv` started.

**In the editor (you):**
1. Let Unity import, then run **Tools/Audio/Sync Events From sound.md**. It creates about 230 events, `AudioConfig`, and fills the placeholders and the campfire loop.
2. **Build the mixer** (`Assets/Audio/Mixer/FireGame.mixer`), about 15 minutes. In the Audio Mixer window:
   1. Create the mixer at that path. Rename its "Snapshot" to **Gameplay**.
   2. Groups, by right-clicking a group → Add child group: under **Master** add **Music**, **Ambience**, **SFX**, **Stingers**, **UI**; under **SFX** add **Player**, **Combat**, **Creatures**, **World**.
   3. Exposed volumes: select each of Master, Music, Ambience, SFX and UI, right-click **Volume** in the inspector → *Expose to script*. In the Exposed Parameters dropdown (top right), rename them exactly **MasterVolume**, **MusicVolume**, **AmbienceVolume**, **SfxVolume**, **UiVolume**.
   4. Effects (Add Effect at the bottom of a group's inspector):
      - **Music** and **Ambience**: Lowpass Simple (cutoff 22000), then Duck Volume (threshold −25 dB, ratio 300 %, attack 10 ms, release 400 ms).
      - **Combat** and **Stingers**: Send → *Music\Duck Volume* (level 0 dB), and a second Send → *Ambience\Duck Volume*.
      - **SFX**: Lowpass Simple (cutoff 22000). **World**: Highpass Simple (cutoff 10).
   5. Snapshots (+ in the Snapshots list), each set by selecting it and changing values. Changes in a snapshot only affect that snapshot. **Never change the five exposed volumes in any snapshot.**
      - **Paused:** Music volume −6 dB, Music lowpass 1200 Hz, Ambience volume −8 dB.
      - **Menu:** Player, Combat, Creatures and World at −80 dB; Ambience −10 dB with lowpass 2000 Hz.
      - **End:** Player, Combat, Creatures and World at −80 dB; Ambience −12 dB.
      - **Gameplay:** everything at 0 dB, all filters open (cutoffs as above). Set it as the start snapshot (right-click → *Set as start Snapshot*).
   6. On the mixer asset, set **Update Mode** to *Unscaled Time*, so snapshots fade while the game is paused.
   7. Run **Sync** again (it links the mixer and assigns every event's bus), then **Tools/Audio/Validate**. The config-and-mixer section should be clean.
3. The acceptance run below.

**Accept:**
- Both assemblies compile (verified with Roslyn outside Unity), with no console errors on import.
- A WAV dropped into each `Clips/` folder gets the right import settings.
- One SoundEvent asset per sound.md ID; Validate shows the coverage table.
- In play mode, with **F9** (overlay), **F8** (pick a test event), **F10** (play at the cursor; a loop toggles) and **F11** (×100):
  - ×100 on `sfx_axe_swing` plays at most its `maxVoices` (4) and the overlay shows the drops. The Profiler shows no GC allocation from `Sfx.Play` (the overlay itself allocates; close it to measure).
  - The campfire loop placed off to one side pans that way, fades as you walk away, and turns "virtual" in the overlay beyond the ring. Coming back, it resumes rather than restarting.
  - Zooming out (Page Down) widens the ring, so more is audible.
  - Esc pauses: a playing loop stops, and `sfx_ui_click` (an `ignorePause` UI event) still plays from the inspector's ▶.
  - "Quit to menu" reloads with no errors and no burst of sounds.
  - Stopping and restarting Play mode, a cooldown-limited event plays immediately (the regression test for the ScriptableObject-state bug).

### Phase 2: Gameplay hooks
Add every event in §6. There's no audio yet and no behaviour change.

**Accept:** it compiles, and a play-through behaves exactly as before.

### Phase 3: Combat, creatures, footsteps

**Code (done):**
1. Sets (`Assets/Scripts/Audio/Sets/`): `SurfaceSounds`, `ToolSounds`, `CreatureVoice`, `FootstepProfile`, `GroundSounds`. `AudioConfig` gained the ground sounds, the crit layer and the bark budget.
2. Reactors (`Assets/Scripts/Audio/Reactors/`):
   - `SurfaceAudio`: impact or glance by material, secondaries −6 dB, the crit layer, silent behind a raised shield.
   - `ToolAudio` + `WeaponAudio`: charge loop and ready ting, light / heavy swing, glance-only swings, bow shot, shield raise / lower / block, loadout swap, stow / draw on carrying, wield from the bag, skill denied / ready.
   - `PlayerAudio`, all of it rather than only the combat part, since every hook existed: hurt (heavy, burn), death, heal, knockback, freezing (start, loop, cold tick, relief), the heartbeat below 25 %, XP chime with a streak pitch, level-ups, buffs, kill confirm.
   - `CreatureAudio`: spawn, idle barks through the shared budget, aggro, windup / swing / contact, hurt (burning), death, despawn, knockback, chase and flee loops.
   - `FootstepAudio`: `OnFootstep` events with a stride fallback when moving with no event for 0.5 s. That covers the backpedal whether or not reversed clips fire events, and the polar bear's clips, which have none. Ground step × foot layer, carry layer, an ice-slide loop.
   - `FlammableAudio`: ignite, **the burning loop** (moved here from a `FireAudio` on BurningEffect, so it's one component), tick, extinguish.
   - `ProjectileAudio`, `WhirlwindAudio`, `AxeThrowAudio`. The flying axe is listened to from the skill, so the Thrown Axe prefab needs nothing.
3. Editor:
   - **Tools/Audio/Set Up Audio On Prefabs** (`AudioPrefabSetup`) makes the sets, links every field to its event by ID, sets `AudioConfig`, adds the reactors to the prefabs and bakes the footsteps. It's idempotent and only fills empty fields.
   - **Tools/Audio/Bake Footstep Events** (`FootstepEvents`): one table of contact frames, measured from the art (the frames with the feet spread widest).
   - `EnemyArtSetup` writes the footsteps on every rebuild, and points a new animal at its own voice, feet and flesh.
   - Validate gained the prefab checks: a Hurtbox without `SurfaceAudio`, footstep events without a receiver.
4. sound.md §13 now has impact IDs: `sfx_surface_flesh`, `_bone`, `_player`, `_wood_log`, `_straw`. Trees and ore reuse `sfx_tree_chop` and `sfx_ore_*_hit`.
5. Placeholders for the most-heard combat events: chop, flesh, bone, charge ready, glance, fist swing, skeleton hurt, wolf bite, crit.

**In the editor (you):**
1. Let Unity import, then run **Sync Events From sound.md**. It adds the five new surface events and auto-fills the new placeholders.
2. Run **Tools/Audio/Set Up Audio On Prefabs**, then **Validate**.

**Accept:**
- Fight skeletons and wolves on snow, then on ice. Swings, hits, glances (punch a tree), crits, kills, aggro and deaths are audible and positional where there's a clip.
- An 8-target Whirlwind doesn't clip (the Master meter in the Audio Mixer window stays below 0 dB).
- Footsteps change with the ground. Backpedalling still steps (stride mode if the reversed clip doesn't fire events).
- No "AnimationEvent has no receiver" errors.
- Content: the Batch A combat and creature subset replaces the placeholders as real sounds arrive (§10.5).

### Phase 4: World, items, stations

**Code (done):**
1. Sets: `FireSoundSet` (fuel-crossfaded layers, feeds by fuel type, stage up, low, out, relight, pops, a torch's motion boost), `ItemSounds` (pickup, land, equip, unequip, eat) with **`ItemDefinition.sounds`**, and `WorldSounds` on `AudioConfig`.
2. Reactors:
   - `FireAudio`: layers follow `fuelLevel` with equal-power crossfades from one shared playhead; the edges play their one-shots; feeds play by what was burned. It's silent while disabled, so a stowed or bagged torch and an unbuilt ghost make no sound.
   - `ProcessorAudio`: the cook / smelt loop while working, done, stalled (once per stall), input.
   - `StructureAudio`: its own build-complete, and the workbench's open / close.
   - `DestructibleAudio`: shake on every hit, a creak once below 30 %, and the fell / break / split.
   - `LootAudio`: the pop when loot is knocked loose.
   - `CollideAudio`: beside `CollideAnimation` on the bump trigger, with a per-prop cooldown.
   - `HarvestAudio`; `InteractionAudio`: the Hold-E fill loop pitching up, fumble, throw aim.
3. Directors on the AudioSystem:
   - `ItemAudioDirector` (`ItemEvents`): the item's own sound plus the shared layer and the ground's landing layer. Only while playing, so the starting Pouch behind the main menu is silent.
   - `BuildAudio`: ghost begin, place, cancel; deliveries pitched up as a site fills; ingredient done; build complete.
4. Small gameplay touch: `CollideAnimation.EntityLayers` (read-only), so `CollideAudio` counts the same bodies.
5. **Set Up Audio On Prefabs** now covers the stations, torches, trees, ore, logs, stone, bushes, hemp, dummy and polar bear loot, the player's `InteractionAudio`, `WorldSounds`, and every `ItemDefinition`'s set by material. Validate also flags fires without `FireAudio` and items without `ItemSounds`.
6. sound.md §7.1 and §7.4 now have IDs: `sfx_item_<material>_pickup / _land` and `sfx_equip_* / sfx_unequip_*` for metal, cloth and leather.
7. Placeholders: the log feed, ember pops, stick pickup and landing, log landing, tree shake and fall, bush rustle, snow landing. The campfire's medium layer is still the real `campfire.wav`; its other three layers have no clips yet, so the fire is only heard around half fuel and above.

**In the editor (you):** run **Sync**, then **Set Up Audio On Prefabs**, then **Validate**.

**Accept:**
- A full loop works with sound: chop a tree (shake, chop, loot pop, log landing, fell), carry the log, throw it into the fire (whoosh, whoomf), then watch the fire burn down.
- With all four layers recorded, you can hear the fire getting low with your eyes closed.
- Walking through bushes isn't spammy.
- A stowed or bagged torch is silent.
- Build a campfire: ghost, place, deliveries rising in pitch, complete.

### Phase 5: UI and settings

**Code (done):**
1. `UiSoundKit` (role → event: click, hover, denied, pickup, place, swap, return, toss, open, close, tab, subtab, slider tick, spell assign, skill buy / maxed, spell learn, main play) and `UiSfx.Play(role)`, on `AudioConfig.ui`.
2. The HUD handlers (the UI exception in §2: one call where each outcome is decided). Drop outcomes are read from the calls' own results, so there's no gameplay change:
   - `BagDrag`: pickup on every drag start. Unclaimed drags toss into the world or snap back (`UiDrop.Unclaimed`).
   - Bag slots: place, merge, swap, or denied onto a full stack of the same kind (`UiDrop.MoveSound`). Fuel onto a torch plays that torch's feed, even in the bag, or denied when it's full (`UiDrop.Fed`).
   - Armour boxes, loadout boxes, hand slots: the gear's own equip or draw on success, denied when refused; moves place or swap. Clicking to take off is the gear's unequip, or denied with no room.
   - Skill slots: spell assign / denied; a spell dragged off its slot plays a lower return.
   - Quickslot: place / denied on assign; denied when R or a click finds nothing to eat (`Quickslot.EatFailed`).
   - `HudMenu`: open / close and the tab click. Crafting's Hands / Workbench and the Skills weapon tabs: subtab. Blueprints: a quiet hover.
   - Skill nodes: buy (pitching up two semitones a rank), maxed, spell learned, or denied.
   - Station panel: open / close, denied when nothing went in, and the output's pickup plus a place when taking it (denied with nothing to take or no room).
3. `CraftAudio` (player): a crafted recipe's own **`Recipe.craftSound`** (by category), denied on `CraftFailed`. `MenuAudio` (director): the main-menu Play whoosh, and the pause panel opening and closing.
4. `ButtonSounds` (hover, click) and `AudioSettingsPanel` (five sliders and the unfocused-mute toggle, bound to `AudioSettingsStore`).
5. Editor: **Tools/Audio/Set Up Menu Audio** adds `ButtonSounds` to every button on the four game panels, and builds the pause panel's Audio section, rebuilt fresh each run. **Set Up Audio On Prefabs** now also makes the UI kit, adds `CraftAudio` and sets every recipe's craft sound.
6. sound.md: seven §10 rows that only point at other sounds are now marked as reuses. Their stray events (`sfx_bag_slot_use_wield`, `sfx_bag_to_station`, `sfx_loadout_box_equip`, `sfx_station_drop_fuel`, `_drop_input`, `_take_output`, `_progress_done`) were unreferenced and were deleted.
7. Placeholders for the UI kit, the skill sounds and the main-menu Play.

**In the editor (you):** **Sync**, **Set Up Audio On Prefabs**, then with Camp open **Tools/Audio/Set Up Menu Audio** and **save the scene**; then **Validate**.

**Accept:**
- Every drag, drop, refusal, tab and button has a sound, none of them harsh.
- The sliders work and persist across a restart.
- The UI is never paused or lowpassed: the pause menu's own clicks play while the world is silent.

### Phase 6: Ambience, music, mix

**Code (done):**
1. Sets on `AudioConfig`: `AmbienceSounds` (the two wind beds, five scattered sounds with their intervals, the three stingers) and `MusicSounds` (three bases, three layers, two end stings, fade times). Also the continuous-mix tuning: parameter names, full-shelter warmth, cutoffs.
2. `AmbienceDirector`:
   - **Beds:** the day and night winds crossfaded by the hour. Night rises over dusk to night (19:00 to 22:00) and falls over dawn to morning (05:00 to 08:00). They blow in the menus (under the Menu snapshot) and while paused.
   - **Scattered sounds,** each on its own clock in game time, so they hold still while paused:
     - gusts, more often at night;
     - a trunk creaking at a random tree within hearing (`DestructibleAudio.Creakers`);
     - birds over dawn;
     - owls and far howls at night.
   - **Stingers on the phases:** Dusk at 19:00, **Night at 22:00** and Dawn at 05:00. `IsNight` already starts at dusk, so `NightChanged` would have played the night stinger on top of the dusk one.
3. `MusicDirector`:
   - **Bases:** the menu; day or night while playing (muffled under the pause menu by the Paused snapshot); the won loop. A death plays its sting over silence.
   - **Layers,** each fading in while its condition holds: combat (`CombatState`), freezing, and low fire (the nearest built campfire within 15 units at 15 % fuel or less).
   - Loops start at the top, so a base and a layer recorded to the same tempo stay in step. It survives the scene reload, so the menu theme carries across "Quit to menu".
   - **Simplified:** stems aren't sample-locked by `PlayScheduled`. Two loops started in the same frame at the same playhead are close enough for these textural layers. The night pulse that would follow how many skeletons are alive was left out (§10.4).
4. `MixDirector`: three exposed parameters that no snapshot touches, eased in log frequency over about a second:
   - **Shelter:** the wind is lowpassed more the warmer the player is near a fire. You hear the shelter, and away from the fire the exposure, which covers sound.md's `amb_cold_silence`.
   - **Freezing:** the world thins out under a highpass.
   - **Low health:** below 25 %, the effects muffle toward the edge of death.

   A missing parameter warns once.
5. Editor: **Set Up Audio On Prefabs** makes and fills both sets, and makes `amb_forest_creak` positional. Validate checks the sets and the three parameters.
6. Placeholders (24 kHz, crossfaded seams): both wind beds, a gust, the three stingers, all three bases, all three layers, and both end stings.

**Mixer steps (you, about 3 minutes).** These are extra effects and parameters that **no snapshot** may change:
1. **Ambience:** Add Effect → a **second** Lowpass Simple, below the first. Right-click its *Cutoff freq* → *Expose to script*, and rename it **AmbienceHeatCutoff**. The first lowpass stays the snapshots' (Menu).
2. **SFX:** right-click its Lowpass Simple *Cutoff freq* → expose → **SfxLowHealthCutoff**.
3. **World:** right-click its Highpass Simple *Cutoff freq* → expose → **WorldFreezeCutoff**.
4. Run **Set Up Audio On Prefabs**, then **Validate**.

**Accept:** one full in-game day (300 s) from 16:00:
- The dusk sting and wind turning harsher, the night sting at 22:00, howls and owls.
- The combat layer in fights; the dawn sting and birds; the 09:00 despawn crumble; then the noon victory sting and the won loop.
- The wind softening as you walk up to the fire.
- Music never cuts hard, and big hits duck it audibly but subtly (the Duck Volume sends from Phase 1).

### Phase 7: Content completion and mix pass

**Tooling (done):**
- **Tools → Audio → Sound Import** (`SoundImportWindow`). Downloads go in `Assets/Audio/_Incoming/`, which is git-ignored and imported uncompressed. You fill in a batch's source and licence, drag files onto events and press Import. Each file is levelled to a −1 dBFS peak; one-shots are trimmed of silence with short fades; loop seams get an equal-power crossfade. Files are mono unless they're beds or music. Each is written as `<id>_NN.wav` in its Clips folder by kind, with a `LICENSES.csv` row, and added to the event. The sources are moved to `_Incoming/_Done`.
- **Placeholders unlinked (2026-11-01):** every generated stand-in was taken off its event, so only real sounds play. The files stay in `Clips/_Placeholder`. Auto-Fill never links them, and Validate doesn't count them as unused.
- **`SoundSourcing.md`** lists sources and licence rules, and the 87 silent P1 events in nine batches with their briefs and search terms. The OpenGameArt packs it names were checked CC0.
- **The licence audit is automated:** Validate flags any clip whose licence cell is empty, `unknown` or `FILL IN`. `sfx_campfire_loop_medium_01.wav` (origin unknown) is marked `unknown` until it's replaced.

**Then, by you:**
1. The nine P1 batches in `SoundSourcing.md`, then P2 (and P3 as features land) from Validate's list, through Sound Import. `Validate` reaches 100 % on P1 and P2.
2. A **mix session**, using these scenarios:
   - the core loop at default zoom, and at zoom 1 and 6;
   - a 6-wolf fight at night;
   - a pause-menu sweep;
   - freezing to death;
   - building every structure.

   Set the group levels first, then the events, never the clips.
3. A **performance pass**:
   - live voices under 28 and loops under 16 in the worst scene;
   - no audio GC in a 5-minute session;
   - the audio thread under 1.5 ms (Profiler → Audio).
4. A licence audit: Validate shows no licence problems (no row missing, none unknown).

---

## 12. Performance budget

| Item | Budget |
|---|---|
| One-shot voices | 28 pooled; under 20 typical |
| Loop sources | 16 pooled. Typical: the campfire's 4 layers, a held torch, a furnace's 4 layers and a burning enemy. The wind beds are separate. |
| Music and ambience sources | 6 (3 stems + 2 layers + crossfade) and 2 beds |
| New voices per frame | 10 |
| GC per play | 0 B |
| CPU (main thread) | Under 0.1 ms: one loop over the live voices for gain and pan, plus reactors that only act on events |
| Memory | About 50 MB of decompressed one-shots at full content, about 10 MB of loops, with music and beds streamed |

---

## 13. Risks and open questions

| Risk / question | Mitigation |
|---|---|
| The mixer can't be created from code | A precise checklist in Phase 1, and the mixer is committed once. Exposed parameter names are constants in `AudioConfig`, and validated at start. |
| Footstep events on backwards playback (speed −1) are unconfirmed | Test in Phase 3; stride-mode fallback. |
| Enemy clips are regenerated by `EnemyArtSetup` | Footstep events are written by the same code; the validator catches clips that lose them. |
| Many fires or torches once players build more | Loop virtualisation beyond 1.2 R, and the loop pool cap. |
| The campfire is the emotional core, and four layers are hard to make seamless | Record or source one long fire session and cut all four layers from it, so they share character. Same length and sync start. |
| Music needs stems from a composer | Brief them on the §10.4 spec before work starts. |
| Exposed parameters stop snapshots driving them | Settings and snapshots touch different controls (§3.6). |
| A future move to FMOD | The `Sfx` and `SoundEvent` seam (§3.4) contains it. |
| Off-screen gameplay sounds (skeleton spawns) could be too loud or too quiet | `k` and `R_min` in `AudioConfig`, tuned in the Phase 7 mix session. |

---

Research references:
- [Unity: randomised playlists / Audio Random Container](https://docs.unity3d.com/6000.6/Documentation/Manual/Create-randomized-playlist.html)
- [ARC runtime limitations (Unity forum)](https://discussions.unity.com/t/modifying-and-getting-audiorandomcontainer-data-at-runtime/1562205)
- [Unity 6.4 audio manual](https://docs.unity3d.com/6000.4/Documentation/Manual/Audio.html)
- [Scriptable audio processors](https://docs.unity3d.com/Manual/audio-scriptable-processors.html)
- [AudioMixer values reset between Awake and Start (issue tracker)](https://issuetracker.unity3d.com/issues/audiomixer-values-get-reset-between-awake-and-start)
- [Forum: mixer volume in Awake doesn't apply](https://forum.unity.com/threads/audiomixer-change-volume-in-awake-not-work.371190/)
- [FMOD licensing](https://fmod.com/licensing)

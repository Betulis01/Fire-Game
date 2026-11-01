# Fire Game — Sound Design

Every sound the game needs today, grouped by the thing that makes it. Each row is one **sound event**: something that happens in the game and should be heard.

This list was built by reading every script and prefab in the project and the Camp scene.

- **What it covers:** every entity, prop, item, station, skill and HUD element that exists now.
- **Not yet in the scene:** a few prefabs exist but aren't placed: the Polar Bear, the Target Dummy, the Shield, the Iron Sword and the Bow. They're listed too, marked P3 unless they're needed sooner.

**IDs are the source of truth for the audio assets.** `Tools/Audio/Sync Events From sound.md` keeps one `SoundEvent` asset per ID under `Assets/Audio/Events/`. Edit a row here first, then run the sync. See [SoundSystemPlan.md](SoundSystemPlan.md) for the system that plays these sounds.

Companion docs: [Fire Game.md](Fire%20Game.md) (how the game plays), [Story.md](Story.md) (tone: a frozen, dying world; the fire is alive).

---

## 1. Conventions

### Columns

| Column | Meaning |
|---|---|
| **ID** | The event name, also the file prefix: `sfx_<group>_<subject>_<action>`. Files get `_01`, `_02`, ... per variant. |
| **When** | The moment it plays, and the code that knows about it (the hook to call the sound from). |
| **Sound** | What it should sound like. |
| **Var** | How many variants to record. The game picks one at random and adds ±5–10 % pitch, so repeated actions don't sound stamped out. |
| **Type** | `1×` one-shot, `loop` loops while a condition holds, `layer` a loop whose volume follows a value (e.g. fuel). |
| **P** | Priority: **P1** core loop and feedback you notice missing at once; **P2** polish that sells the world; **P3** later, or not in the scene yet. |

### Sound direction

- **The world is frozen, quiet and hostile.** Most of the soundscape is wind and the crunch of snow. Silence is a tool: let the fire be the loudest, warmest thing on screen.
- **The fire is the emotional centre.** It should sound alive (crackle, breathing, pops), and its sound should shrink audibly as it dies. A player should *hear* the fire getting low before they look at it.
- **Materials are chosen by the victim, not the weapon** (this is how `HitEffect` already works). An axe on a tree and a fist on a tree both sound like wood. The weapon adds its own whoosh on the swing and, for a glance, a "no effect" thud.
- **Pixel-art scale.** Keep sounds short, punchy and slightly stylised rather than hyper-real. UI sounds should be soft and woody or papery, not digital blips. The HUD reads as a carved, muted-blue panel.
- **Positional.** World sounds play at their source with gentle 2D distance falloff around the camera. UI and player-status sounds play non-positionally.

---

## 2. Where the system stands

The audio layer is described in [SoundSystemPlan.md](SoundSystemPlan.md), whose Status table tracks the phases. In short:

- Every ID here has a `SoundEvent` asset under `Assets/Audio/Events/`, kept in sync by **Tools/Audio/Sync Events From sound.md**.
- The old `HitEffect.sfx` / `PlayClipAtPoint` path is gone; hits are `SurfaceAudio` (§13).
- `campfire.wav` is now `Assets/Audio/Clips/SFX/Loops/Fire/sfx_campfire_loop_medium_01.wav`.
- Every gameplay moment in the **When** column has a C# event to hang its sound on (SoundSystemPlan §6).
- Footsteps are Animation Events (`OnFootstep`) on the run and walk clips, baked by **Tools/Audio/Bake Footstep Events**.

---

## 3. Player

### 3.1 Movement

Movement is one speed (`PlayerController.speed`), scaled down by cold, carrying, food buffs and the ground.

**Footsteps are layered** (SoundSystemPlan §4): every walker plays the shared **ground step** for the surface under its feet, and a creature adds its own **foot layer** on top (§6). The player's boots are the ground step itself. A wolf on ice is the ice step plus a paw. The ground types are `GroundType { Snow, Grass, Ice, Stone, Dirt }`. Snow, grass and ice have patches in the scene; stone and dirt exist only as types so far.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_ground_step_snow` | Each footfall on Snow (`GroundSensor.Ground`): the player's `se_run` / `ne_run`, and the ground layer of every creature's step | Deep, dry snow crunch, slightly squeaky (it's −20 °C). The default and most-heard sound in the game. | 8 | 1× | P1 |
| `sfx_ground_step_grass` | Footfall on Grass | Frozen grass: brittle, short crunch, lighter than snow. | 6 | 1× | P1 |
| `sfx_ground_step_ice` | Footfall on Ice | Hard, hollow boot tap with a faint glassy ring. | 6 | 1× | P1 |
| `sfx_ground_step_stone` | Footfall on Stone (type exists, no patch yet) | Boot on frozen rock, gritty. | 6 | 1× | P3 |
| `sfx_ground_step_dirt` | Footfall on Dirt (type exists, no patch yet) | Frozen packed earth, dull. | 6 | 1× | P3 |
| `sfx_player_carry_layer` | Layered on each ground step while `Hands.IsCarrying` (log or stone; speed ×0.75) | A faint creak of the load and a heavier body. The ground step under it is played a little lower and louder. | 4 | 1× | P2 |
| `sfx_player_backpedal` | `*_run_backwards` (moving away from the aim) | Uses the step set. Just make sure the event fires when the clip plays at speed −1. | — | — | P2 |
| `sfx_player_ice_slide` | On Ice while `GroundSensor.SlipTime > 0` and velocity ≠ input (the steering lag) | Soft boot skid on ice. Volume follows the mismatch between velocity and input. | 1 | layer | P2 |
| `sfx_player_stop_skid` | Releasing input on ice (long ease-out) | Short scrape as the slide dies. | 3 | 1× | P3 |

### 3.2 Body: hurt, cold, health

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_player_hurt` | `Health.Damaged` on the player, `DamageType.Combat` | Short grunt plus a cloth/flesh thump. The flesh impact itself comes from the player's surface (§13); this is the voice. | 4 | 1× | P1 |
| `sfx_player_hurt_heavy` | Combat damage ≥ ~25 % of max health in one hit | Bigger pained gasp. | 2 | 1× | P2 |
| `sfx_player_hurt_burn` | Damage of `DamageType.Fire` | Hiss plus a sharp intake of breath. | 2 | 1× | P3 |
| `sfx_player_knockback` | `Knockback.Apply` on the player | Snow scuff of feet sliding back (plays with the hurt sound). | 3 | 1× | P2 |
| `sfx_player_death` | `PlayerHealth` → `Health.Died` (game over) | Final exhale or collapse into snow. Leaves a beat of silence before the game-over sting (§11). | 1 | 1× | P1 |
| `sfx_player_freeze_start` | `PlayerTemperature.IsFreezing` turns true (warmth hits 0) | Sharp shiver gasp plus a frost "crackle" sweep. The warning that health is now draining. | 2 | 1× | P1 |
| `sfx_player_freezing_loop` | While `IsFreezing` | Chattering teeth and shaky breath, quiet under everything else. | 1 | loop | P2 |
| `sfx_player_cold_tick` | `PlayerTemperature.ColdOverflow` (cold damage reaching health) | Tiny ice crack plus a small grunt. Throttle it to at most one per ~1.5 s. | 3 | 1× | P2 |
| `sfx_player_warm_relief` | Warmth goes from freezing back above 0, near a `HeatSource` | Long relieved exhale. | 2 | 1× | P2 |
| `sfx_player_breath_cold` | Idle in the cold (`PlayerTemperature.Temp` below a threshold), every few seconds | Soft visible-breath exhale. Pairs with a breath puff VFX later. | 3 | 1× | P3 |
| `sfx_player_lowhealth_loop` | Health below ~25 % | Muffled heartbeat that speeds up as health drops. | 1 | layer | P2 |
| `sfx_player_heal` | `Health.Healed` (food) | Soft warm swell. Combat text shows the number; this is the feel. | 2 | 1× | P2 |

### 3.3 Hands, carrying, bag

Big items (logs, the big stone) go in the hands, one per hand; everything else goes in the bag.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_player_lift_heavy` | A Big item goes into a hand (`Inventory.TryTake` → `Hands`) | Effort grunt plus the item's pickup sound (§7). | 3 | 1× | P1 |
| `sfx_player_putdown` | Left click while carrying (`ItemUser`: put the load down) | Grunt-less set-down. The item's drop sound carries it. | — | — | P1 |
| `sfx_player_pickup_small` | A Small item goes into the bag (E, or walking over a stack the bag has room on) | Generic "into the pocket" rustle under the item's own pickup sound. | 3 | 1× | P1 |
| `sfx_player_autopickup` | Walking over a matching stack auto-merges it (`Inventory`, auto-pickup) | Lighter, quicker pocket tick, so auto-pickups don't sound like deliberate E presses. | 3 | 1× | P2 |
| `sfx_player_fumble` | `PlayerInteractor.Fumble` (picked up with no room, the item drops beside you) | "Whoops" cloth flap. The item's land sound follows. | 2 | 1× | P2 |
| `sfx_player_bag_spill` | `Inventory.Spill` (swapping to a smaller bag) | Several items tumbling out onto snow. | 2 | 1× | P3 |

### 3.4 Q: drop, deposit, throw

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_player_drop` | Q tap with no receiver (`Hands.Drop`) | Item's drop sound (§7) plus a small release. | — | — | P1 |
| `sfx_player_deposit` | Q tap inside a receiver zone (`ItemReceiver.Deposit`) | The receiver plays its own accept sound (fire feed §8, build site §9). | — | — | P1 |
| `sfx_player_throw_aim` | Q held past `throwHoldTime` (`DropGhost.Begin`) | Soft "wind-up" tick when the ghost appears. | 1 | 1× | P3 |
| `sfx_player_throw` | Q release or left click with the ghost up (`PlayerInteractor.Throw` → `ThrownItem.Launch`) | Effort grunt plus an air whoosh, scaled to the item's weight. | 3 | 1× | P1 |
| `sfx_item_land_<material>` | `ThrownItem` lands outside a receiver | The item's drop sound, a bit louder (§7). | — | — | P1 |
| `sfx_item_land_in_fire` | `ThrownItem` lands in a `FuelReceiver` zone | The fire's fuel-added sound (§8.1) plus a spark burst. | — | — | P1 |

### 3.5 E: interact, equip

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_player_interact_fail` | E with nothing usable, or a refused interact (`Interactable.Interact` returns false) | Soft "nothing there" (very quiet, or none). | 1 | 1× | P3 |
| `sfx_player_hold_equip_fill` | E held on gear: the prompt's hold bar fills (`InteractPrompt.SetHold`) | Rising soft tone or leather creak over `holdTime` (0.35 s). | 1 | layer | P2 |
| `sfx_player_hold_equip_done` | Hold completes: `Inventory.TryEquipFromWorld` | The gear's equip sound (§7.5) plus a satisfying snap. | — | — | P2 |

### 3.6 Eating, buffs, progression

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_player_eat_berry` | `FoodUse` eats a berry (bag click, R, quickslot) | Small crunchy-juicy bite. | 3 | 1× | P1 |
| `sfx_player_eat_meat_raw` | Eats Polar Bear Meat | Wet, chewy tear. A little gross. | 2 | 1× | P3 |
| `sfx_player_eat_meat_cooked` | Eats Polar Bear Meat Cooked | Satisfying chew plus a swallow. | 2 | 1× | P3 |
| `sfx_player_eat_denied` | R or quickslot click on cooldown, or with no food left (`Quickslot`) | Muted "empty" tap. | 1 | 1× | P2 |
| `sfx_player_buff_start` | `PlayerBuffs` adds a modifier | Faint warm shimmer. | 1 | 1× | P3 |
| `sfx_player_buff_end` | A buff expires (`PlayerBuffs.StatsChanged` with fewer modifiers) | Faint falling shimmer. | 1 | 1× | P3 |
| `sfx_player_xp_gain` | `PlayerExperience` floats "+N" at a kill | Tiny soft chime, pitched up slightly per kill in a streak. | 3 | 1× | P2 |
| `sfx_player_levelup` | `PlayerExperience.LeveledUp` | Warm, short fanfare: a rising ember whoosh plus a bell. The best-feeling sound in the game. | 1 | 1× | P1 |
| `sfx_weapon_levelup` | A weapon's level rises (`WeaponExperience.Changed`, level compare) | Lighter metal ring plus a whoosh. "Your axe got better." | 1 | 1× | P2 |

---

## 4. Weapons and tools (the swing)

The swing pipeline (`WeaponUse`) has these moments, all already called from Animation Events:

1. **Press:** `TryUse` arms the swing (windup).
2. **Charge ready:** `OnAttackChargeReady`, button still held. The clip freezes.
3. **Charge resolved:** `ResolveCharge(heavy)` on release, light or heavy by `chargeTime`.
4. **Swing:** `OnAttackSwing`. The swing VFX and the lunge start here.
5. **Contact:** `OnAttackHit`. It connects, glances or misses.
6. **Impact hold:** `Tool.impactHold` (axe and pickaxe stick for a beat). HitStop freezes time.

The **impact** is the victim's sound (§13). The weapon only owns the whoosh, the charge and the "glance".

### 4.1 Shared swing set

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_swing_charge_loop` | Between press and release, once past ~0.1 s | Rising tension (cloth tightening, low hum). Stops on release. | 1 | layer | P2 |
| `sfx_swing_charge_ready` | `OnAttackChargeReady`, or hold ≥ `chargeTime`, i.e. a heavy is now guaranteed (the camera zooms in here) | Clear "ting" or ember flare. The player must hear that the heavy is ready. | 1 | 1× | P1 |
| `sfx_swing_glance` | Contact on a target this tool can't damage (`ToolDamageFilter`, `StrikeTarget.glancing`): a fist or sword on a tree, an axe on ore | Dull, dead thud plus a slight wrist-jar rattle. Must read as "wrong tool", distinct from a real hit. | 3 | 1× | P1 |
| `sfx_swing_miss` | `OnAttackHit` with no targets | Nothing extra; the whoosh is enough. (Optional faint air tail.) | — | — | — |
| `sfx_swing_lunge_step` | Lunge-grace step (`AttackLunge.BeginStep`) | Quick snow scuff of the step-in. | 3 | 1× | P2 |
| `sfx_combat_crit` | A hit that crits (`DamageInfo` crit; combat text already pops it alone) | Extra sharp crack layered over the material impact. | 2 | 1× | P1 |
| `sfx_combat_kill` | The player's blow kills an enemy (`IKillReactor`) | Low, punchy "thump" under the death vocal, to confirm the kill. | 2 | 1× | P2 |

### 4.2 Per weapon

| ID | Weapon (prefab) | Moment | Sound | Var | Type | P |
|---|---|---|---|---|---|---|
| `sfx_fists_swing` | Fists (empty hand) | `OnAttackSwing`, light | Quick cloth-and-air jab. | 3 | 1× | P1 |
| `sfx_fists_swing_heavy` | Fists | Heavy | Bigger haymaker whoosh plus an effort grunt. | 2 | 1× | P2 |
| `sfx_axe_swing` | Stone Axe | Light ("hack" style) | Heavy, low whoosh of a stone head. | 3 | 1× | P1 |
| `sfx_axe_swing_heavy` | Stone Axe | Heavy | Longer, deeper overhead whoosh plus a grunt. | 2 | 1× | P1 |
| `sfx_pickaxe_swing` | Stone Pickaxe (two-handed) | Light | Whoosh with a slight whistle (pointed head). | 3 | 1× | P1 |
| `sfx_pickaxe_swing_heavy` | Stone Pickaxe | Heavy | Deeper overhead whoosh plus a grunt. | 2 | 1× | P2 |
| `sfx_sword_swing` | Iron Sword ("slash" style) | Light | Thin metallic swish with a faint ring. | 3 | 1× | P3 |
| `sfx_sword_swing_heavy` | Iron Sword | Heavy | Long, singing sweep. | 2 | 1× | P3 |
| `sfx_torch_swing` | Torch, Sap Torch | Swing | Whoosh **plus a flame "fwoom"** as the fire is dragged through the air. | 3 | 1× | P1 |
| `sfx_dualwield_combo` | Two melee weapons, L-R-L combo (`ItemUser`) | Each swing | Uses each weapon's swing. Raise the pitch slightly per combo step for rhythm. | — | — | P2 |
| `sfx_bow_draw` | Bow (two-handed) | Press / charging | Wood creak plus string stretch, lengthening with the charge. | 2 | layer | P3 |
| `sfx_bow_ready` | Bow | Full charge (the longest range) | Taut creak peak (the `sfx_swing_charge_ready` cue for the bow). | 1 | 1× | P3 |
| `sfx_bow_release` | Bow | `OnAttackHit` fires the arrow | String twang plus a thump. Louder when more charged. | 3 | 1× | P3 |
| `sfx_arrow_flight` | Arrow (`Projectile`) | While flying | Short whizz that fades as it slows over the last third of its range. | 2 | loop | P3 |
| `sfx_arrow_hit_<material>` | Arrow | `Projectile.OnTriggerEnter2D` on a Hurtbox | Sharp "thock" into flesh, wood or bone (uses the victim's material, plus a point-impact layer). | 3 | 1× | P3 |
| `sfx_arrow_drop` | Arrow | Speed below the floor, despawns | Soft "fff-tk" into the snow. | 2 | 1× | P3 |
| `sfx_shield_raise` | Shield | `ShieldBlock.IsBlocking` → true | Wood-and-leather lift, a slight bump. | 2 | 1× | P3 |
| `sfx_shield_lower` | Shield | `IsBlocking` → false | Softer drop. | 2 | 1× | P3 |
| `sfx_shield_block` | Shield | A hit arrives while blocking (`Health.OnHit` with `DamageReduction`) | Big wooden THUNK plus a grunt. It replaces the flesh impact. | 3 | 1× | P3 |

### 4.3 Torch-specific

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_torch_flame_loop` | A torch is lit and active: drawn in a hand, or lying in the world (`Fuel.fuelLevel > 0`, object active) | Small, fluttering flame. Louder when moving fast, quieter when standing. Goes silent when stowed (carrying) or in the bag, since the object is inactive. | 1 | loop | P1 |
| `sfx_torch_ignite_target` | `Flammable.Ignite` (a torch hit sets something on fire) | "Fwoomp" catch. | 2 | 1× | P1 |
| `sfx_torch_burnout` | A torch's `Fuel` reaches 0 (`FireRender` turns `isOut` on, `hideWhenOut`) | Sputter plus a smoky fizz. A clear "your light is gone" moment. | 2 | 1× | P1 |
| `sfx_torch_low` | Torch fuel crosses ~15 % | Flame sputter (once). | 2 | 1× | P2 |
| `sfx_torch_refuel` | Fuel dragged onto a torch (`Inventory.TryFeed` → `FuelReceiver.Deposit`) | Stick: dry crackle and catch. Sap: sticky hiss-flare. | 2 each | 1× | P2 |
| `sfx_torch_refuel_full` | `TryFeed` onto a full torch (it accepts nothing) | The UI "denied" tap (§10.1). | — | — | P2 |
| `sfx_torch_relight` | Fuel added to an out torch (flame reappears) | Small "fwip" ignition. | 1 | 1× | P2 |

### 4.4 Loadout (weapon sets A/B)

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_loadout_swap` | Scroll or Tab swaps sets (`Loadout` swap; `IsSwapping` for `swapTime` 0.3 s) | Two quick cloth-and-leather moves: stow one, draw the other. Pitched by what's drawn (a metal ring for a sword). | 3 | 1× | P1 |
| `sfx_loadout_swap_denied` | Swap pressed mid-swing (refused) | Very quiet tick, or none. | 1 | 1× | P3 |
| `sfx_loadout_stow` | Picking up a load stows the loadout onto the back (`Hands`, `BackStow` shows the X) | Weapons slapping onto the back strap. | 2 | 1× | P2 |
| `sfx_loadout_draw` | Putting the load down redraws the loadout | Quick draw. | 2 | 1× | P2 |

---

## 5. Skills

| ID | Skill | When | Sound | Var | Type | P |
|---|---|---|---|---|---|---|
| `sfx_skill_cast_denied` | any | Key 1/2 with nothing slotted, no hand holding the weapon, the hand locked, or on cooldown | Muted "denied" thunk. | 1 | 1× | P2 |
| `sfx_skill_ready` | any | The cooldown finishes (`IActiveSkill` cooldown → 0) | Small bright "ready" ping, quiet. | 1 | 1× | P2 |
| `sfx_whirlwind_start` | Whirlwind | `PlayCast("whirlwind")` begins | Effort shout plus a wind-up whoosh. | 2 | 1× | P1 |
| `sfx_whirlwind_spin` | Whirlwind | Each turn: `OnWhirlwindHit` (once per clip loop) | Circular whoosh, one per spin, so it rhythmically "chops" the air. Pitch up a little each turn. | 3 | 1× | P1 |
| `sfx_whirlwind_end` | Whirlwind | The last spin ends | Exhale plus a slowing whoosh tail. | 1 | 1× | P2 |
| `sfx_axethrow_windup` | Axe Throw | Throw clip starts (cast) | Overhand effort grunt. | 2 | 1× | P1 |
| `sfx_axethrow_release` | Axe Throw | `OnAttackHit` lets go → `ThrownAxe` spawns | Release whoosh. | 2 | 1× | P1 |
| `sfx_axethrow_spin_loop` | Thrown Axe | Phases Out and Back | "Whup-whup-whup" spinning loop that tracks the axe position. Faster on the return's "whoosh" half. | 1 | loop | P1 |
| `sfx_axethrow_stick` | Thrown Axe | Contact: hits its target and goes Stuck | Meaty THUNK of the axe sinking in (the victim's flesh or bone impact plays too). | 2 | 1× | P1 |
| `sfx_axethrow_rip` | Thrown Axe | Stuck → Back (`stickTime` over) | Wet or woody rip-out. | 2 | 1× | P2 |
| `sfx_axethrow_miss_turn` | Thrown Axe | No target: it slows and turns back | Whoosh dips in pitch and swings around. | 1 | 1× | P2 |
| `sfx_axethrow_catch` | Thrown Axe | Arriving at the hand (`caught`): "a punch and a jolt of the camera" | Solid palm-on-handle SLAP. | 2 | 1× | P1 |

---

## 6. Enemies and wildlife

All enemies share `EnemyBrain` (Wander / Combat / Flee), `EnemyMover`, `Health`, `Knockback`, `FlashOnHit`, `BloodSplatterOnHit` and `Flammable`. The **flesh / bone impact itself** comes from their `SurfaceAudio` material (§13). The rows below are the creature's own voice and body.

### 6.1 Skeleton (`Enemy.prefab`; 6 in the scene, plus the campfire's spawner)

Hostile. It wanders, and a hit makes it chase forever. It spawns by rising from the ground in a ring around the campfire, despawns at 09:00, and its corpse stays (`keepBody`).

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_skeleton_spawn` | `se_spawn` / `ne_spawn` plays (`EnemyAnimator` Start; `EnemySpawner` spawned it) | Frozen ground cracking, snow and dirt crumbling, then bones knitting together, a rising rattle. Unsettling. Play it so the player can *hear* where one rises. | 3 | 1× | P1 |
| `sfx_skeleton_idle` | While wandering, every 4–10 s at random | Faint bone creak or jaw click. | 4 | 1× | P2 |
| `sfx_foot_bone` | Foot layer on each skeleton footfall in `*_run` (the ground step plays under it) | Light bony clack. | 6 | 1× | P2 |
| `sfx_skeleton_aggro` | `EnemyBrain` goes Wander → Combat (the first hit taken) | Hollow, rattling hiss or scream. "It's coming for you now." | 3 | 1× | P1 |
| `sfx_skeleton_attack_windup` | `*_attack` clip starts (`EnemyAttacker` arms a swing) | Bone creak of the arm cocking back. A dodge tell. | 2 | 1× | P1 |
| `sfx_skeleton_attack_swing` | `OnAttackSwing` (FistsEnemy) | Dry, rattly whoosh. | 3 | 1× | P1 |
| `sfx_skeleton_hurt` | `Health.Damaged` / `se_hit` / `ne_hit` | Bone crack plus a rattle shake. (The bone impact material carries the hit itself.) | 4 | 1× | P1 |
| `sfx_skeleton_death` | `Health.Died` → `*_die` | The skeleton collapses into a pile of bones, ending on a skull bounce. | 3 | 1× | P1 |
| `sfx_skeleton_despawn` | `EnemyDespawner.Despawned` (09:00) | Bones crumbling to frost or dust, a soft sigh. Morning relief. | 2 | 1× | P2 |

### 6.2 Wolf (`Wolf.prefab`; 6 in the scene)

Hostile, the same AI as the skeleton. It has idle, run, bite and death animations, no hit animation (it only flashes), and its corpse stays.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_wolf_idle` | Wandering, every 5–12 s | Panting, sniffing, a low huff. | 4 | 1× | P2 |
| `sfx_foot_paw` | Foot layer on each wolf footfall in `*_run` | Soft padded paw, quick. | 6 | 1× | P3 |
| `sfx_wolf_aggro` | Wander → Combat | Deep snarling growl. | 3 | 1× | P1 |
| `sfx_wolf_chase_loop` | In Combat and moving | Breathy panting and growl under the steps. | 1 | loop | P3 |
| `sfx_wolf_bite_windup` | `*_bite` starts (swing frame 4) | Short snarl. The dodge tell. | 2 | 1× | P1 |
| `sfx_wolf_bite` | `OnAttackHit` (frame 5) | Jaw SNAP. Plays whether it lands or not; a landed bite adds the player's hurt sound. | 3 | 1× | P1 |
| `sfx_wolf_hurt` | `Health.Damaged` | Sharp yelp. | 4 | 1× | P1 |
| `sfx_wolf_death` | `Health.Died` → `*_death` | Pained whimper fading out, then a body flop into snow. | 2 | 1× | P1 |
| `sfx_wolf_howl_distant` | Ambience at night (§11.2), not tied to an actual wolf | Lonely far howl, sometimes answered. | 4 | 1× | P2 |

### 6.3 Boar (`Boar.prefab`; 3 in the scene)

Skittish. It ignores the player, and a hit makes it flee (`FleeBehavior`, speed ×3.6). It has idle, run and death animations, and its corpse stays.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_boar_idle` | Wandering, every 4–10 s | Contented grunts, snuffling and rooting in snow. | 5 | 1× | P2 |
| `sfx_foot_hoof` | Foot layer on each boar footfall | Short hoof trot, heavy-ish. | 6 | 1× | P3 |
| `sfx_boar_hurt` | `Health.Damaged` | Loud, shrill squeal. | 4 | 1× | P1 |
| `sfx_boar_flee` | `FleeBehavior.Begin` (Flee state starts) | Panicked squeal-grunt as it bolts. | 2 | 1× | P1 |
| `sfx_boar_flee_loop` | In Flee, moving | Fast hoof-drumming plus panicked huffs. | 1 | loop | P2 |
| `sfx_boar_death` | `Health.Died` | A last squeal that cuts off, then a heavy thud. | 2 | 1× | P1 |

### 6.4 Deer (`Deer.prefab`; 2 in the scene)

Skittish, like the boar. It has idle, walk, run and death animations. The walk and run clips both exist; today it uses run for all movement.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_deer_idle` | Wandering, every 5–14 s | Soft snort, chewing, an ear-flick rustle. | 4 | 1× | P2 |
| `sfx_foot_hoof_light` | Foot layer on each deer footfall in `*_walk` / `*_run` (louder when running) | Delicate hoof tap; the bounding run plays it harder. | 6 | 1× | P3 |
| `sfx_deer_hurt` | `Health.Damaged` | Short bleat or bark. | 3 | 1× | P1 |
| `sfx_deer_flee` | `FleeBehavior.Begin` | Alarm bark (a deer "warning" call) plus the bound-away start. | 2 | 1× | P1 |
| `sfx_deer_death` | `Health.Died` | Soft bleat fading out, then a body fold into snow. | 2 | 1× | P1 |

### 6.5 Polar Bear (`Polar Bear.prefab`; not in the scene) — P3

Hostile. It has a `DropOnDeath` (meat and leather). Its clips are `n/e/s_idle` and `n/e/s_walk` only.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_bear_idle` | Wandering | Deep huffs and sniffing. | 3 | 1× | P3 |
| `sfx_foot_heavypaw` | Foot layer on each polar bear footfall | Heavy, slow paw thud. | 6 | 1× | P3 |
| `sfx_bear_aggro` | → Combat | Big roar. | 2 | 1× | P3 |
| `sfx_bear_attack` | Swing | Huff plus a heavy claw swipe whoosh. | 3 | 1× | P3 |
| `sfx_bear_hurt` | Damaged | Pained bellow. | 3 | 1× | P3 |
| `sfx_bear_death` | Died | Long groan, then a massive body thud. | 1 | 1× | P3 |

### 6.6 Target Dummy (`TargetDummy.prefab`; not in the scene) — P3

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_dummy_hit` | Hit | Straw-and-sack thump plus a wooden post creak (a "straw" material, §13). | 4 | 1× | P3 |
| `sfx_dummy_break` | Died | Straw burst plus wood snap. | 1 | 1× | P3 |

### 6.7 Shared creature reactions

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_creature_knockback` | `Knockback.Apply` on any enemy | Snow scuff of the shove. Longer on ice (`KnockbackSlide`). | 3 | 1× | P2 |
| `sfx_creature_knockback_ice` | The shove happens on Ice | Body sliding on ice. | 2 | 1× | P3 |
| `sfx_blood_splatter` | `BloodSplatterOnHit` drops a `GroundSplatter` | Very soft wet patter on snow, under the impact. | 3 | 1× | P3 |
| `sfx_burning_ignite` | `Flammable.Ignite` (on anything: enemy, dummy) | Catching "fwoomp" (the same as `sfx_torch_ignite_target`). | — | — | P1 |
| `sfx_burning_loop` | While `Flammable` is burning (`BurningEffect` child) | Crackling fire on a body, medium. | 1 | loop | P1 |
| `sfx_burning_tick` | Each `tickDamage` | Small sizzle pop. | 3 | 1× | P2 |
| `sfx_burning_extinguish` | `Flammable.Extinguish` (timer out, or death) | Smoky fizz-out. | 2 | 1× | P2 |
| `sfx_creature_voice_burning` | Hurt while burning | Same as hurt, but pick the more panicked variant. | — | — | P3 |

---

## 7. Items

An item has three moments the player hears: **pickup** (E or auto-pickup), **drop / land** (Q drop, throw landing, spill, drag into the world, a tree's loot popping out) and, for gear, **equip / unequip**. Group them by **material** so one set covers many items.

### 7.1 Item material sets

Each item's `ItemDefinition` points at an `ItemSounds` set (pickup, land, equip, unequip, eat). Items share sets by material, so one recording covers many items. The ground under a landing adds its own layer (`sfx_ground_land_*`, §9.4).

| ID | Set | Items | Sound | Var | P |
|---|---|---|---|---|---|
| `sfx_item_wood_heavy_pickup` | `wood_heavy` | Scots Pine Wood, Grey Alder Wood (logs, Big) | Pickup: Hollow log knock plus a lift. | 3 | P1 |
| `sfx_item_wood_heavy_land` | `wood_heavy` | Scots Pine Wood, Grey Alder Wood (logs, Big) | Drop / land on snow: Heavy wooden THUD into snow, with a dull bounce. | 4 | P1 |
| `sfx_item_stone_heavy_pickup` | `stone_heavy` | Stone (Big) | Pickup: Gritty scrape plus a lift. | 3 | P1 |
| `sfx_item_stone_heavy_land` | `stone_heavy` | Stone (Big) | Drop / land on snow: Deep stone thump, muffled by snow. | 4 | P1 |
| `sfx_item_stick_pickup` | `stick` | Stick (23 in the scene) | Pickup: Dry twig clatter. | 3 | P1 |
| `sfx_item_stick_land` | `stick` | Stick (23 in the scene) | Drop / land on snow: Light twig tumble. | 3 | P1 |
| `sfx_item_stone_small_pickup` | `stone_small` | Rock, Iron Stone | Pickup: Pebble clack. | 3 | P1 |
| `sfx_item_stone_small_land` | `stone_small` | Rock, Iron Stone | Drop / land on snow: Small stone plunk into snow. | 3 | P1 |
| `sfx_item_sap_pickup` | `sap` | Sap | Pickup: Sticky squelch. | 2 | P2 |
| `sfx_item_sap_land` | `sap` | Sap | Drop / land on snow: Soft "plap". | 2 | P2 |
| `sfx_item_berry_pickup` | `berry` | Red/Blue Berries | `sfx_bush_pick` (§9.3): a berry is picked off its bush. | — | — |
| `sfx_item_berry_land` | `berry` | Red/Blue Berries | Drop / land on snow: Soft berry patter. | 2 | P1 |
| `sfx_item_fibre_pickup` | `fibre` | Hemp, Burlap | Pickup: Soft fibrous rustle. | 2 | P2 |
| `sfx_item_fibre_land` | `fibre` | Hemp, Burlap | Drop / land on snow: Soft cloth flop. | 2 | P2 |
| `sfx_item_leather_pickup` | `leather` | Leather, Pouch, Small Bag | Pickup: Leather creak or flap. | 2 | P2 |
| `sfx_item_leather_land` | `leather` | Leather, Pouch, Small Bag | Drop / land on snow: Leather flop. | 2 | P2 |
| `sfx_item_metal_bar_pickup` | `metal_bar` | Iron Bar | Pickup: Metal clink, weighty. | 2 | P2 |
| `sfx_item_metal_bar_land` | `metal_bar` | Iron Bar | Drop / land on snow: Dull clank (muffled by snow). | 3 | P2 |
| `sfx_item_meat_pickup` | `meat` | Polar Bear Meat (raw, cooked) | Pickup: Wet slap. | 2 | P3 |
| `sfx_item_meat_land` | `meat` | Polar Bear Meat (raw, cooked) | Drop / land on snow: Wet thud. | 2 | P3 |
| `sfx_item_tool_wood_pickup` | `tool_wood` | Stone Axe, Stone Pickaxe, Torch, Sap Torch, Bow, Shield | Pickup: Wooden handle grab, a knock. | 3 | P1 |
| `sfx_item_tool_wood_land` | `tool_wood` | Stone Axe, Stone Pickaxe, Torch, Sap Torch, Bow, Shield | Drop / land on snow: Wooden clatter into snow. | 3 | P1 |
| `sfx_item_tool_metal_pickup` | `tool_metal` | Iron Sword | Pickup: Metal-on-leather grab. | 2 | P3 |
| `sfx_item_tool_metal_land` | `tool_metal` | Iron Sword | Drop / land on snow: Metal clatter, a short ring. | 3 | P3 |
| `sfx_item_armour_metal_pickup` | `armour_metal` | Iron Helmet | Pickup: Metal clank. | 2 | P2 |
| `sfx_item_armour_metal_land` | `armour_metal` | Iron Helmet | Drop / land on snow: Helmet bonk and roll. | 2 | P2 |
| `sfx_item_armour_cloth_pickup` | `armour_cloth` | Burlap Pants | Pickup: Cloth swish. | 2 | P2 |
| `sfx_item_armour_cloth_land` | `armour_cloth` | Burlap Pants | Drop / land on snow: Soft flop. | 2 | P2 |

### 7.2 Item-specific moments

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_item_loot_pop` | `DropOnHit` / `DropOnDeath` scatter loot (logs and sap off a tree, stone off ore, meat off a bear) | Quick "pop" toss, then the item's land sound. | 3 | 1× | P1 |
| `sfx_item_merge_stack` | A pickup merges onto a bag stack | Folded into `sfx_player_pickup_small`; maybe pitch up slightly. | — | — | P2 |

### 7.3 Hittable carried items (the log and the big stone)

These are both items and targets: chop a log for 3 sticks, mine the big stone for 3 rocks.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_log_hit` | Axe hits a log (`Health.Damaged`; `HitAnimation`) | Axe "chock" into lying wood (the wood material, §13). | — | — | P1 |
| `sfx_log_split` | Log `Health.Died` → 3 Sticks (`DropOnDeath`) | Satisfying wood SPLIT crack plus sticks scattering. | 3 | 1× | P1 |
| `sfx_log_bump` | Walking into a log (`CollideAnimation` → "collide") | Soft wooden knock or roll. | 3 | 1× | P2 |
| `sfx_stone_hit` | Pickaxe hits the big stone | Stone material (§13). | — | — | P1 |
| `sfx_stone_break` | Stone `Died` → 3 Rocks | Rock crack-and-crumble. | 2 | 1× | P1 |
| `sfx_stone_bump` | Walking into the big stone (`CollideAnimation`) | Faint scrape (the stone barely moves). | 2 | 1× | P3 |

### 7.4 Bag and gear (world → body)

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_equip_metal` | Metal gear worn (Iron Helmet): from the bag, Hold E, or the Player tab | Clank onto the head, a settle. | 2 | 1× | P1 |
| `sfx_equip_cloth` | Cloth gear worn (Burlap Pants) | Cloth swish, a settle. | 2 | 1× | P1 |
| `sfx_equip_leather` | A bag worn (Pouch, Small Bag) | Strap creak and buckle. | 2 | 1× | P1 |
| `sfx_unequip_metal` | Metal gear taken off into the bag | A softer, reversed clank. | 2 | 1× | P2 |
| `sfx_unequip_cloth` | Cloth gear taken off | A softer swish. | 2 | 1× | P2 |
| `sfx_unequip_leather` | (bags are only swapped, never taken off; kept for leather gear later) | A strap loosening. | 1 | 1× | P3 |
| `sfx_bag_swap` | Wearing a new bag (the contents carry over) | Two straps and a rustle of items shifting. | 1 | 1× | P2 |

---

## 8. Stations and fire

### 8.1 Campfire (`Campfire.prefab`)

It has `Fuel` (a FIFO of burning items), `FireRender` with 4 stages (ember, small, medium, roaring), and an unlit `outSprite`. It also carries `HeatSource`, `FireLight`, `CookingProcessor`, `FuelReceiver`, `ProcessorInputReceiver`, `EnemySpawner` and `StationInteractable`. **This is the most important sound in the game.**

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_campfire_loop_ember` | `FireRender` stage 0 | Faint glowing hiss and the odd tick. Lonely. | 1 | layer | P1 |
| `sfx_campfire_loop_small` | Stage 1 | Gentle crackle. | 1 | layer | P1 |
| `sfx_campfire_loop_medium` | Stage 2 | Steady, cosy crackle (`campfire.wav` could be this one). | 1 | layer | P1 |
| `sfx_campfire_loop_roaring` | Stage 3 | Big breathing roar plus loud pops. | 1 | layer | P1 |
| `sfx_campfire_pop` | Random every 1–6 s, more often at high fuel | Single sharp ember pop or log shift. Breaks up the loop's repetition. | 6 | 1× | P1 |
| `sfx_campfire_feed_stick` | `Fuel.FuelAdded` with a Stick (Q, a throw, the station panel) | Twig catch crackle-flare. | 3 | 1× | P1 |
| `sfx_campfire_feed_log` | `FuelAdded` with Wood | Heavy log drop into coals, a WHOOMF plus a spark burst. | 3 | 1× | P1 |
| `sfx_campfire_feed_sap` | `FuelAdded` with Sap | Resin hiss and flare, a bright sizzle. | 2 | 1× | P2 |
| `sfx_campfire_stage_up` | `FuelStage` rises | A breathy "whoosh" of the fire growing. | 2 | 1× | P2 |
| `sfx_campfire_low_warning` | Fuel crosses ~15 % (still stage 0) | Sputtering gasp. Should make you look at the fire. | 2 | 1× | P1 |
| `sfx_campfire_out` | `fuelLevel` hits 0 → `outSprite` | Hiss plus a smoke "fff" and a final ember tick, then silence. Dread. | 1 | 1× | P1 |
| `sfx_campfire_relight` | Fuel added to an out fire | Flint-less "catch": embers re-flaring into a small flame. | 2 | 1× | P1 |
| `sfx_campfire_cook_loop` | `CookingProcessor` has a job and `Fuel` is burning | Meat sizzle over the crackle. | 1 | loop | P3 |
| `sfx_campfire_cook_done` | A job finishes into the output (`ItemProcessor`) | Sizzle swell plus a soft "done" pop. | 1 | 1× | P3 |
| `sfx_campfire_build_complete` | `PlacementGhost.Complete` for the campfire | The first fire catching: a match-less flare into small. | 1 | 1× | P1 |

Mixing note: the stage loops are **layers**. Cross-fade between them by `fuelLevel`, not by hard stage switches, so the fire swells and dies smoothly. Their audible radius should roughly match `HeatSource` range: you hear the warmth.

### 8.2 Furnace (`Furnace.prefab`)

The same fuel and stage set as the campfire (ember, small, medium, roaring), plus a `SmeltingProcessor`.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_furnace_loop_ember` | `FireRender` stage 0 | Faint glow inside stone: a low hiss and the odd tick. | 1 | layer | P1 |
| `sfx_furnace_loop_small` | Stage 1 | Enclosed, muffled crackle. | 1 | layer | P1 |
| `sfx_furnace_loop_medium` | Stage 2 | A steady, bassy roar, a fire inside stone. | 1 | layer | P1 |
| `sfx_furnace_loop_roaring` | Stage 3 | A full roar with the chimney drawing hard. | 1 | layer | P1 |
| `sfx_furnace_feed` | `FuelAdded` | Fuel thrown into a stone mouth, a muffled whoomf. | 3 | 1× | P1 |
| `sfx_furnace_input` | Ore deposited (`ProcessorInputReceiver`) | Ore clatters into the crucible. | 3 | 1× | P1 |
| `sfx_furnace_smelt_loop` | A job is running and fuel is burning | Low bubbling of molten metal plus a hum. | 1 | loop | P2 |
| `sfx_furnace_bar_done` | A job finishes (Iron Bar into the output) | Metallic "tink" plus a quench hiss. | 2 | 1× | P1 |
| `sfx_furnace_out` | Fuel hits 0 | Deep sigh plus cooling-stone ticks. | 1 | 1× | P1 |
| `sfx_furnace_output_full` | The output is full or mismatched, so the job waits at full progress | Single dull warning knock (once per stall). | 1 | 1× | P3 |
| `sfx_furnace_build_complete` | Built | Stones settling, then a first ignition rumble. | 1 | 1× | P2 |

### 8.3 Workbench (`Workbench.prefab`)

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_workbench_open` | E toggles `HudMenu.OpenCrafting(atWorkbench: true)` | Wooden knock plus tools shifting on the bench. | 2 | 1× | P1 |
| `sfx_workbench_close` | E again, or the menu closed | Lighter knock. | 1 | 1× | P2 |
| `sfx_workbench_build_complete` | Built | Hammering flourish: three taps and a settle. | 1 | 1× | P2 |

### 8.4 Building: placement and build sites

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_build_ghost_begin` | `PlacementGhost.Begin` (a Placeable recipe clicked; the menu closes) | Blueprint "unroll", a paper flap. | 1 | 1× | P1 |
| `sfx_build_ghost_place` | Left click places it → `BuildRequirement` added | Stake driven into snow, a firm thunk. | 2 | 1× | P1 |
| `sfx_build_ghost_cancel` | `PlacementGhost.CancelActive` (Esc or F) | Paper crumple or swoosh away. | 1 | 1× | P1 |
| `sfx_build_deposit_<material>` | `BuildRequirement.Deposit` accepts an ingredient (Q, a throw, a drag) | The item's material land sound plus a "counted" tick, pitched up per ingredient as the site fills. | — | 1× | P1 |
| `sfx_build_ingredient_done` | One ingredient type becomes satisfied | Small confirm chime. | 1 | 1× | P2 |
| `sfx_build_complete` | All satisfied → `PlacementGhost.Complete` (the ghost turns opaque) | Construction "poof": wood and stone settling plus a bright confirm, then the structure's own `_build_complete`. | 1 | 1× | P1 |

### 8.5 Heat (not a station)

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_heat_enter` | Player enters a `HeatSource` range from the cold | A soft warm swell in the mix (the wind ducks a little). | 1 | 1× | P2 |
| `sfx_heat_ambience` | Inside heat | Rather than a sound: duck the wind (§11.1) by warmth gained. | — | mix | P2 |

---

## 9. Environment props

### 9.1 Trees: Scots Pine (4), Grey Alder (8)

Axe only (`ToolDamageFilter`). Each damaging hit has a 50 % chance to drop a log and 15 % to drop sap (`DropOnHit`, max 2), and a felled tree drops its reward. `HitAnimation` plays a hit shake, `OccluderFade` fades a tree you stand behind, and the tree is destroyed on death (no stump).

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_tree_chop` | An axe hit deals damage (wood material, §13) | Solid "THOCK" of stone axe into frozen trunk. | 5 | 1× | P1 |
| `sfx_tree_shake` | `HitAnimation` "Hit" plays (on any hit, glance included) | Needles and branches shaking, plus **snow sliding off branches** (a soft "fwump"). | 4 | 1× | P1 |
| `sfx_tree_glance` | A fist or sword hits the tree (no damage) | The weapon's `sfx_swing_glance` plus a duller wood knock. | — | — | P1 |
| `sfx_tree_creak` | Health below ~30 % after a hit | A deep, ominous creak (the tree is about to go). | 3 | 1× | P2 |
| `sfx_tree_fell` | `Health.Died` (the tree vanishes, loot pops) | Big crack, a falling swoosh, and a muffled CRASH into snow with a snow-burst tail. The tree has no fall animation yet, so the sound can carry the moment. | 2 | 1× | P1 |
| `sfx_tree_wind_creak` | Ambient: random trees creak in strong wind, at night | Distant trunk creaks. | 4 | 1× | P3 |

Scots Pine (conifer) is more needle hiss; Grey Alder (deciduous, bare in winter) is more twig rattle. The shake set can differ per tree type: 2 sets × 4.

### 9.2 Ore: Stone Ore (5), Iron Ore (4)

Pickaxe only. Each damaging hit has a 50 % chance to drop stone; the ore breaks on death.

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_ore_stone_hit` | A pickaxe hit on Stone Ore (stone material) | Sharp rock "chk" plus small debris. | 5 | 1× | P1 |
| `sfx_ore_iron_hit` | A pickaxe hit on Iron Ore (an iron-ore material) | Rock hit with a metallic ring under it. | 5 | 1× | P1 |
| `sfx_ore_glance` | An axe or fist on ore | The axe: "TING" of stone on stone, sparky and wrong. A fist: dull smack. | 3 | 1× | P1 |
| `sfx_ore_break_stone` | Stone Ore `Died` | Rock splitting, then rubble tumble. | 2 | 1× | P1 |
| `sfx_ore_break_iron` | Iron Ore `Died` | Heavier split with a metallic clank in the rubble. | 2 | 1× | P1 |

### 9.3 Bushes and plants

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_bush_rustle` | Walking through a berry bush (`CollideAnimation` "Colliding" → true; 44 in the scene) | Frozen-leaf rustle plus a little snow shaken off. Throttle it, since bushes are everywhere. | 5 | 1× | P1 |
| `sfx_bush_pick` | A berry picked from a bush (its child berries are taken: E pickup) | Small "plik" pluck plus a branch spring. | 3 | 1× | P1 |
| `sfx_bush_regrow` | `BerryRespawner` grows a new berry (after 60 s) | None, or a near-silent soft pop if the player is close. | 1 | 1× | P3 |
| `sfx_hemp_rustle` | Walking through a Hemp Plant (`CollideAnimation`; 5 in the scene) | Dry stalk swish. | 3 | 1× | P2 |
| `sfx_hemp_harvest` | `Harvestable.Interact` (E: you get Hemp; the leaves and snow layers hide) | Fibrous rip plus snow puff. | 3 | 1× | P1 |
| `sfx_hemp_regrow` | Regrows after `regrowTime` | None (or a very soft rustle near the player). | — | — | P3 |

### 9.4 Snow and ground (non-footstep)

| ID | When | Sound | Var | Type | P |
|---|---|---|---|---|---|
| `sfx_ground_land_snow` | Things landing on snow (an item drops, a body falls, a tree fells) | A generic soft snow-burst layer to put under land sounds. | 4 | 1× | P1 |
| `sfx_ground_land_grass` | Things landing on frozen grass | A short brittle rustle under the land sound. | 3 | 1× | P2 |
| `sfx_ground_land_ice` | Things landing on ice | A hard tap with a little skitter. | 3 | 1× | P2 |
| `sfx_occluder_fade` | Walking behind a tree (`OccluderFade`) | None. Purely visual. | — | — | — |
| `sfx_snowtrail` | `SnowTrail` stamps | None. The footsteps cover it. | — | — | — |

---

## 10. HUD and UI

The HUD has the stats (top left), the action bar (bottom), the **F menu** with its Player, Crafting and Skills tabs, the station panel, tooltips, interact prompts, nameplates and combat text. Menus are Main, Pause, Game Over and Game Won.

UI sounds play **non-positionally** on a UI bus. Keep them soft, woody and papery, matching the muted-blue carved frames.

### 10.1 Shared UI kit

These are reused by everything below. Record them once.

| ID | Used for | Sound | Var | P |
|---|---|---|---|---|
| `sfx_ui_hover` | Hovering a clickable box (button, tab, blueprint, skill node) | Barely-there soft tick. Optional; keep it very quiet. | 2 | P3 |
| `sfx_ui_click` | Generic button or tab press | Short wooden tap. | 3 | P1 |
| `sfx_ui_denied` | Any refused action: crafting without ingredients, a locked node, a wrong drop, a full torch, a cast with no hand | Muted double thunk, low. | 2 | P1 |
| `sfx_ui_pickup` | Start dragging anything (an item, a spell) | Lift: soft paper or cloth lift. | 2 | P1 |
| `sfx_ui_place` | Dropping a dragged thing onto a valid target | Set down: a satisfying soft "tock". | 3 | P1 |
| `sfx_ui_swap` | A drop that swaps two things | Two quick tocks. | 2 | P2 |
| `sfx_ui_return` | A drag released over nothing valid and returned to its slot | Soft reverse whoosh. | 1 | P2 |
| `sfx_ui_toss` | A drag released over the world (the item goes on the ground) | Lift, then the item's real drop sound in the world. | 1 | P2 |
| `sfx_ui_open` | A panel opens | Leather or paper flap open. | 2 | P1 |
| `sfx_ui_close` | A panel closes | Flap close. | 2 | P1 |
| `sfx_ui_slider_tick` | Dragging a volume slider in the Audio settings (throttled), on that slider's own bus | Tiny soft tick, so you hear the level you're setting. | 1 | P2 |

### 10.2 F menu (`HudMenu`, `TabButtonUI`)

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_menu_open` | F opens the menu (always on the Player tab) | `sfx_ui_open` (a book-or-satchel open, slightly bigger) | P1 |
| `sfx_menu_close` | F or Esc closes it | `sfx_ui_close` | P1 |
| `sfx_menu_tab` | Clicking the Player, Crafting or Skills tab (`TabButtonUI.Clicked`) | Page turn. | P1 |
| `sfx_menu_subtab` | Sub-tabs: Crafting Hands / Workbench; Skills Axe / Sword / Pickaxe / Wand / Bow | Smaller page flick. | P2 |
| `sfx_menu_subtab_disabled` | Clicking a greyed-out sub-tab (a weapon without a tree) | `sfx_ui_denied` (or silent; it ignores clicks today) | P3 |

### 10.3 Player tab: bag, equipment, loadouts

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_bag_slot_use_wield` | Bag slot click → `Inventory.UseSlot` wields a tool | `sfx_loadout_draw` (the tool's draw) | P1 |
| `sfx_bag_slot_use_wear` | Click → wears gear | `sfx_equip_<material>` (§7.4) | P1 |
| `sfx_bag_slot_use_eat` | Click → eats food | `sfx_player_eat_<food>` (§3.6) | P1 |
| `sfx_bag_slot_use_none` | Click on something with no use (a stick, a rock) | Silent, or `sfx_ui_denied` very quiet | P3 |
| `sfx_bag_drag` | Begin dragging a bag stack (`BagDrag`, `DragIconUI` shows) | `sfx_ui_pickup` | P1 |
| `sfx_bag_move` | Dropped on an empty bag slot (`Move`) | `sfx_ui_place` | P1 |
| `sfx_bag_merge` | Dropped on a matching stack (merge) | `sfx_ui_place` plus a tiny pitch-up | P2 |
| `sfx_bag_swap_slots` | Dropped on a different item | `sfx_ui_swap` | P2 |
| `sfx_bag_feed_torch` | Fuel dropped onto a torch's box (`Inventory.TryFeed` in BagSlotUI, LoadoutSlotUI or ActionSlotUI) | `sfx_torch_refuel` (§4.3) | P2 |
| `sfx_bag_to_world` | Released over the world (`Inventory.DropAt`) | `sfx_ui_toss`, then the world drop | P1 |
| `sfx_bag_to_station` | Released over the world on a fire or station zone | `sfx_campfire_feed_*` / `sfx_furnace_feed`: the station's own feed | P1 |
| `sfx_equip_box_wear` | A bag stack dropped onto an armour box (`EquipSlotUI`) | `sfx_equip_<material>` | P1 |
| `sfx_equip_box_remove` | Click a filled armour box → it goes into the bag | `sfx_unequip_<material>` | P1 |
| `sfx_equip_box_wrong` | Dropping non-gear or wrong-slot gear on a box | `sfx_ui_denied` | P2 |
| `sfx_equip_box_bag_full` | Click to remove with no bag room (nothing happens) | `sfx_ui_denied` | P2 |
| `sfx_loadout_box_equip` | A bag item dropped onto a loadout box (`EquipToLoadout`) | `sfx_loadout_draw` (the tool's draw) | P1 |
| `sfx_loadout_box_swap` | A loadout item dragged onto another loadout box (either set) | `sfx_ui_swap` plus a light weapon knock | P2 |
| `sfx_loadout_box_remove` | A loadout box clicked → back into the bag | `sfx_ui_place` plus a leather tuck | P2 |
| `sfx_twohanded_displace` | Equipping a two-handed tool knocks the Off Hand item into the bag | `sfx_ui_swap` | P3 |

### 10.4 Action bar (`ActionBarUI`, `ActionSlotUI`, `QuickslotUI`)

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_actionbar_set_swap` | Set A/B highlight changes (the same moment as `sfx_loadout_swap`) | Covered by `sfx_loadout_swap` | — |
| `sfx_actionbar_spell_assign` | A learned spell dropped from a skill tree onto skill slot 1/2 | Rune "slot-in": a soft magical tock | P2 |
| `sfx_actionbar_spell_swap` | A spell dragged between the two skill slots | `sfx_ui_swap` | P3 |
| `sfx_actionbar_spell_clear` | A spell dragged off a slot onto nothing (the slot empties) | `sfx_ui_return` (pitched down) | P3 |
| `sfx_actionbar_hand_drag` | A hand slot's item dragged (like a loadout box) | `sfx_ui_pickup` | P2 |
| `sfx_quickslot_assign` | A food stack dropped onto the R slot | `sfx_ui_place` plus a berry rustle | P2 |
| `sfx_quickslot_eat` | R or a click | `sfx_player_eat_<food>` | P1 |
| `sfx_quickslot_empty` | R with none left (the slot dimmed) | `sfx_ui_denied` | P2 |
| `sfx_skill_cooldown_ready` | A skill slot's cooldown overlay finishes | `sfx_skill_ready` (§5) | P2 |

### 10.5 Crafting tab (`CraftingTabUI`, `BlueprintCellUI`, `IngredientRowUI`)

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_craft_hover` | `BlueprintCellUI.HoverStarted` (the details show) | `sfx_ui_hover` | P3 |
| `sfx_craft_denied` | Clicking a dimmed blueprint (missing ingredients, or not near a workbench) | `sfx_ui_denied` | P1 |
| `sfx_craft_wood` | `CraftingController.Crafted`, a wood recipe: Torch, Bow | Twigs binding, a cord pulled tight, then a little "done" sparkle. About 0.6 s. | P1 |
| `sfx_craft_stone_tool` | A stone tool: Stone Axe, Stone Pickaxe | Lashing a stone head to a haft: cord, knock, sparkle. | P1 |
| `sfx_craft_cloth` | A cloth recipe: Burlap, Burlap Pants, Small Bag | Weaving and a cloth snap, then the sparkle. | P1 |
| `sfx_craft_metal` | A metal recipe: Iron Sword | Hammer on hot iron, two strikes and a ring. | P2 |
| `sfx_craft_sap` | A sap recipe: Sap Torch | Sticky squelch and a wrap, then the sparkle. | P2 |
| `sfx_craft_placeable` | Clicking a Placeable (Campfire, Workbench, Furnace) closes the menu → ghost | `sfx_build_ghost_begin` (§8.4) | P1 |
| `sfx_craft_no_room` | An Instant recipe whose output has nowhere to go (not crafted) | `sfx_ui_denied` | P2 |
| `sfx_craft_unlocked` | A recipe becomes craftable while the tab is open (the cell undims) | Very soft "available" twinkle. Optional. | P3 |

### 10.6 Skills tab (`SkillsTabUI`, `SkillTreeUI`, `SkillNodeUI`)

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_skill_node_buy` | A click spends a point (a passive rank goes up) | Carved-rune "clunk" plus a rising tone; pitch rises with the rank. | P1 |
| `sfx_skill_node_maxed` | A node reaches max rank (children unlock) | Fuller chord, and the connecting lines "light up" | P2 |
| `sfx_skill_spell_learn` | A Spell node learned (maxRank 1) | Bigger magical whoosh-and-bell | P1 |
| `sfx_skill_node_locked` | Clicking a locked node, or with no points | `sfx_ui_denied` | P1 |
| `sfx_skill_spell_drag` | Dragging a learned spell toward the action bar | `sfx_ui_pickup` (magic tint) | P2 |
| `sfx_skill_point_available` | `WeaponExperience` grants a point (a weapon level) | Covered by `sfx_weapon_levelup` (§3.6) | — |

### 10.7 Station panel (`StationPanelUI`, `StationSlotUI`)

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_station_panel_open` | E on the campfire or furnace → `StationPanelUI.Show` | `sfx_ui_open`, plus the station's fire momentarily louder | P1 |
| `sfx_station_panel_close` | E again, walking away, or another station takes over (`Closed`) | `sfx_ui_close` | P1 |
| `sfx_station_drop_fuel` | A bag stack dropped on the fuel box → `FuelReceiver` | `sfx_campfire_feed_*` / `sfx_furnace_feed` (§8): the station's own feed | P1 |
| `sfx_station_drop_input` | Dropped on the input box → `ProcessorInputReceiver` | `sfx_furnace_input` (food onto the fire: none yet) | P1 |
| `sfx_station_drop_refused` | A wrong item for that box (not accepted) | `sfx_ui_denied` | P1 |
| `sfx_station_take_output` | Output box click → `StationPanelUI.TakeOutput` (items into the bag) | `sfx_item_*_pickup` plus `sfx_ui_place` | P1 |
| `sfx_station_take_none` | Output click with no room in the bag, or empty | `sfx_ui_denied` | P2 |
| `sfx_station_progress_done` | The progress bar completes (the job finishes) | `sfx_campfire_cook_done` / `sfx_furnace_bar_done` (§8) | P1 |

### 10.8 Prompts, tooltips, nameplates, combat text

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_prompt_focus` | E focus moves to a new interactable (`PlayerInteractor` focus change; the highlight turns on) | Extremely soft tick, or silent. **Don't** make this annoying. | P3 |
| `sfx_prompt_hold_fill` | The Hold E bar fills | `sfx_player_hold_equip_fill` (§3.5) | P2 |
| `sfx_tooltip` | A tooltip shows | **Silent.** | — |
| `sfx_nameplate` | A nameplate appears or drains | **Silent.** The hit sound covers it. | — |
| `sfx_combat_text` | A damage number pops | **Silent.** The crit has its own sound (§4.1). | — |
| `sfx_aim_indicator` | The combat arrow appears (`CombatState` → in combat) | Silent; combat music handles it (§11.3). | — |
| `sfx_target_preview` | The soft-target highlight moves to a new primary | Silent. (Optional faint tick for gamepad players later.) | P3 |

### 10.9 Game menus (`GameStateManager`)

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_menu_main_play` | Main menu "Play" (`PlayGame`) | A big, warm ember whoosh as the fire lights; the music crossfades into the game | P1 |
| `sfx_menu_pause` | Esc → Paused (only when nothing is on top) | `sfx_ui_open`, plus the world audio ducks to ~20 % with a low-pass filter (a mixer snapshot) | P1 |
| `sfx_menu_resume` | "Resume", or Esc again | `sfx_ui_close`, and the duck releases | P1 |
| `sfx_menu_button_hover` | Hovering a menu button | `sfx_ui_hover` | P3 |
| `sfx_menu_button_click` | Try Again, Replay, Quit to menu | `sfx_ui_click` | P1 |
| `sfx_zoom` | Page Up / Page Down (`CameraZoomControl`) | Silent, or a very faint lens "whff". | P3 |
| `sfx_inputmode_switch` | `UserInput.ModeChanged` (mouse ↔ gamepad) | Silent. | — |

---

## 11. World: ambience, time and music

### 11.1 Ambience beds (always-on loops)

| ID | When | Sound | Type | P |
|---|---|---|---|---|
| `amb_wind_day` | Daytime phases (Dawn, Morning, Noon, Afternoon) | Steady, cold, open-field wind with soft gusts. Low and constant. | loop | P1 |
| `amb_wind_night` | Dusk and Night (`DayNightCycle.IsNight`) | Harsher, whistling wind plus occasional strong gusts. Crossfade it in over Dusk. | loop | P1 |
| `amb_gust` | Random every 10–30 s, louder at night | One-shot gust swell plus snow hiss across the ground. | 1× (6 var) | P2 |
| `amb_forest_creak` | Random, when near trees | Distant trunk creaks, branch snaps, snow dropping off a branch. | 1× (6 var) | P2 |
| `amb_dawn_birds` | Dawn → Morning | A few sparse, cold crows or ravens. Life returning. | 1× (4 var) | P2 |
| `amb_night_owl` | Night | Owl calls, rare. | 1× (3 var) | P3 |
| `amb_night_howl` | Night | `sfx_wolf_howl_distant` (§6.2) | 1× | P2 |
| `amb_cold_silence` | Far from any fire | Mix rule, not a file: with no `HeatSource` near, high-pass the wind slightly and remove warmth, so being away from the fire *feels* exposed. | mix | P2 |

### 11.2 Time of day (`DayNightCycle.PhaseChanged`, `NightChanged`)

The day is 300 s long and play starts at 16:00. Phases: Dawn 5, Morning 8, Noon 12, Afternoon 16, Dusk 19, Night 22.

| ID | When | Sound | P |
|---|---|---|---|
| `sfx_time_dusk` | `PhaseChanged(Dusk)` | Low, ominous swell. The dark is coming. | P1 |
| `sfx_time_night` | `PhaseChanged(Night)`, 22:00: the skeleton hours (`IsNight` already starts at dusk, so it can't be the cue) | Deeper sting plus a distant howl. Make it unmistakable. | P1 |
| `sfx_time_dawn` | `PhaseChanged(Dawn)` / `NightChanged(false)` | Soft rising warmth and the first bird. Relief. | P1 |
| `sfx_time_despawn_hour` | 09:00 (skeletons crumble) | Covered by the per-skeleton `sfx_skeleton_despawn`. Maybe a soft chord on top. | P2 |

### 11.3 Music (states)

| ID | When | Feel | P |
|---|---|---|---|
| `mus_main_menu` | `GameState.MainMenu` | A sparse, lonely, warm theme: one instrument by a fire. | P1 |
| `mus_day_explore` | Playing, daytime, not in combat | Very sparse, mostly silence with occasional motifs. Don't fight the ambience. | P2 |
| `mus_night_tension` | Playing, at night | A low drone and pulse that rises with the number of skeletons alive (or with distance from the fire). | P1 |
| `mus_combat_layer` | `CombatState.Changed(true)` → false after `leaveDelay` | A percussive layer added on top of the day or night music; it fades out when combat ends. | P1 |
| `mus_freezing_layer` | `PlayerTemperature.IsFreezing` | A dissonant, icy high layer. Danger. | P2 |
| `mus_low_fire_layer` | Campfire fuel < ~15 % | A thin, nervous layer: the fire is dying. | P2 |
| `mus_game_over` | `GameState.GameOver` ("You died!") | A short, mournful sting, then quiet wind. | P1 |
| `mus_game_won` | `GameState.GameWon` ("You survived the Night!") via `WinCondition` | Warm, triumphant sunrise sting into a gentle loop. | P1 |

---

## 12. Code hooks that don't exist yet

Most events above already have a method or C# event to hang a sound on. These don't, and need a small addition:

1. **Footsteps.** Add an Animation Event (e.g. `OnFootstep`) on the contact frames of the run clips:
   - **Player:** `se_run`, `ne_run`; backwards plays the same clip.
   - **Skeleton:** `Enemy/*_run`.
   - **Wolf:** `Wolf/*_run`.
   - **Boar:** `Boar/*_run`.
   - **Deer:** `Deer/*_walk`, `*_run`.

   The receiver reads `GroundSensor.Ground` and plays the surface set. Note: the enemy clips are copied from the Aseprite tags by `EnemyArtSetup`, so the events must be re-added on rebuild, the same way the bite events are.
2. **Enemy state changes.** `EnemyBrain` has no event for Wander → Combat (aggro) or → Flee. Add `StateChanged`, or call the sound where `OnHit` switches state.
3. **Enemy spawn.** `EnemyAnimator` plays the spawn state on Start. Hook the sound there, or use `EnemySpawner` right after it instantiates.
4. **Fire stage, low and out.** `FireRender` already detects the stage change and `isOut`. Expose them as events (`StageChanged(int)`, `WentOut`, `Relit`), and drive the loop layers from `Fuel.fuelLevel`.
5. **ItemProcessor job done.** It needs a `JobFinished` event (and a `Stalled` event for output full).
6. **Freezing on/off.** `PlayerTemperature.IsFreezing` is polled. An edge-triggered `FreezingChanged(bool)` makes it one line.
7. **Weapon level up.** `WeaponExperience.Changed` fires on every XP gain. Add `LeveledUp(ToolKind, int)` like `PlayerExperience` has.
8. **Shield raise and lower.** `ShieldBlock.IsBlocking` is recomputed every Update. Fire a sound on the edge.
9. **Swing glance.** `MeleeWeapon.Strike` knows `StrikeTarget.glancing`. Play the weapon's glance sound there (the victim's material sound still plays via SurfaceAudio).
10. **UI drops.** Every drop target already returns through `BagDrag.Claim*`, and a successful claim plus the resulting `Inventory` / `Loadout` / `Equipment.Changed` is the place to play "place / swap / denied". `ui_return` is the case where `OnEndDrag` sees no claim and no world drop.

A small **audio layer** to add before the sounds themselves:

- **`SoundEvent`** (ScriptableObject): its clips, volume, pitch range, a cooldown (a throttle for bushes and cold ticks), positional or not, and its mixer group. Same pattern as `HitEffect`, which already owns its clip.
- **`Sfx.Play(SoundEvent, position)`:** a pooled AudioSource player. It replaces `AudioSource.PlayClipAtPoint`, which spawns a 3D source at the hit point, so the camera's Z distance would affect its volume in this 2D game.
- **A loop emitter component** (`SoundLoop`): a SoundEvent plus a 0..1 value (fuel level, speed), for the fire layers, the torch, burning, the wind and the heartbeat.
- **AudioMixer groups:**
  - Master
  - Music
  - Ambience
  - World SFX
  - Creatures
  - Player
  - UI

  Plus a **Paused** snapshot (low-pass and duck the world) and a **HitStop** snapshot (optional: a 50 ms dip on big hits to sell the freeze).

---

## 13. Surface materials (SurfaceSounds sets)

Hits sound like **what was struck**. `SurfaceAudio` on the victim plays its material's impact (or its glance knock, when the striking tool can't damage it), whatever the weapon. One `SurfaceSounds` set per material; the `HitEffect` assets keep only the VFX. Tree and ore impacts reuse their §9 rows.

| ID | Set | Used by | Sound | Var | P |
|---|---|---|---|---|---|
| `sfx_surface_flesh` | Flesh | Wolf, Boar, Deer, Polar Bear | Meaty thwack plus a wet layer. | 5 | P1 |
| `sfx_surface_bone` | Bone | Skeleton (`Enemy.prefab`) | Dry crack plus a bone rattle. **No blood**: skeletons shouldn't bleed. | 5 | P1 |
| `sfx_surface_player` | Player | Player | Cloth plus a body thump (with the hurt voice from §3.2). | 4 | P1 |
| `sfx_tree_chop` | Wood | Scots Pine, Grey Alder | `sfx_tree_chop` (§9.1). Glance: the weapon's own glance only. | — | — |
| `sfx_surface_wood_log` | Wood Log | Scots Pine Wood, Grey Alder Wood (logs) | Higher, hollower chop into lying wood. | 4 | P1 |
| `sfx_ore_stone_hit` | Stone | Stone Ore, Stone (big) | `sfx_ore_stone_hit` (§9.2); glance `sfx_ore_glance`. | — | — |
| `sfx_ore_iron_hit` | Iron Ore | Iron Ore | `sfx_ore_iron_hit` (§9.2); glance `sfx_ore_glance`. | — | — |
| `sfx_surface_straw` | Straw | Target Dummy | Sack-and-straw thump. | 4 | P3 |
| `sfx_shield_block` | (shield) | A blocked hit on the player | `sfx_shield_block` (§4.2) replaces the player's impact. | — | — |

A crit adds `sfx_combat_crit` (§4.1) on top of any material. A secondary (cleave) target plays its impact 6 dB down.

Note: the skeleton (`Enemy.prefab`) and the Target Dummy still point at the Blood **VFX** (`HitEffect`), so skeletons bleed visually. That's separate from the sound.

---

## 14. Totals

These are the counts of unique sound **events** in each section. Rows that only reuse another sound aren't counted.

| Section | P1 | P2 | P3 |
|---|---|---|---|
| Player (§3) | ~22 | ~20 | ~12 |
| Weapons and torch (§4) | ~15 | ~12 | ~16 |
| Skills (§5) | 8 | 4 | 0 |
| Creatures (§6) | ~25 | ~15 | ~22 |
| Items (§7) | ~14 | ~10 | ~6 |
| Stations and building (§8) | ~22 | ~10 | ~4 |
| Props (§9) | ~16 | ~4 | ~4 |
| UI (§10) | ~10 shared kit sounds, plus ~12 unique | ~8 | ~6 |
| Ambience, time, music (§11) | ~9 | ~9 | ~1 |

Roughly **140 P1 events**. With variants, that's about **450 audio files for P1**, and a little over 900 for everything.

**Suggested first batch** (the core loop, about 40 events):
- the player's snow, grass and ice steps;
- the four campfire layers and its feed, low, out and relight sounds;
- the axe swing and charge-ready;
- the glance;
- the wood, stone and bone impacts;
- tree shake and fell; ore hit and break;
- the skeleton's spawn, aggro, attack, hurt and death;
- the wolf's growl, bite and yelp;
- the boar and deer hurt and flee sounds;
- the torch's flame loop and burnout;
- pickup and drop for wood, sticks and stone;
- the player's hurt, death and freeze-start;
- the UI kit's click, denied, pickup, place, open and close;
- the menu open, close and tab sounds;
- level-up;
- day and night wind;
- the dusk, night and dawn stings;
- the combat music layer;
- the game over and game won music.

That set alone makes the game feel finished on audio.

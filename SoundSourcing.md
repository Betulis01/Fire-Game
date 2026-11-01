# Sound sourcing

Where to find the real sounds, how to bring them in, and the **P1** sounds still without clips (87 when this was written on 2026-11-01). The full catalogue is `sound.md`; Tools → Audio → Validate always has the current list (P2 and P3 come after these).

## How to bring a batch in
1. Download a pack or some files. Put them anywhere under `Assets/Audio/_Incoming/`. That folder isn't in git.
2. Open **Tools → Audio → Sound Import**. Fill in the batch: Source (the pack or page name), Author, Licence, URL. Use one batch per pack, so every licence row is right.
3. Audition files with ▶ on the left. Drag the good ones onto events on the right. The list starts with silent events, P1 first. Expand an event to read its brief.
4. Press **Import**. Each file is levelled to a −1 dBFS peak. One-shots are trimmed of silence; loops get their seam crossfaded. Each is written as `<id>_NN.wav` in the right Clips folder, with a licence row, and added to the event. The sources move to `_Incoming/_Done`.
5. Select the event and press ▶ Play to hear it with its jitter. When a batch is in, run **Validate**.

Several variants per event (the Var column) keep repeats from sounding mechanical. One good file is still better than none, and two or three takes cut from one recording count as variants.

## Where to look
**Licences that are fine:** CC0 (no strings), CC-BY (credit the author; the Author column feeds a credits screen later), and royalty-free libraries that allow commercial use.
**Avoid:** NonCommercial (NC) licences, which block selling the game; NoDerivatives (ND), since trimming is a change; and anything ripped from games or films.

| Source | Licence | Good for |
|---|---|---|
| [Kenney](https://kenney.nl/assets/category:Audio): Impact Sounds, RPG Audio, Interface Sounds, UI Audio | CC0 | Footsteps (grass, snow, wood, concrete), wood, metal and stone impacts, cloth and leather, UI. Download in the browser. |
| [Sonniss GDC Game Audio Bundles](https://sonniss.com/gameaudiogdc) | Royalty-free, commercial use, no attribution | Professional fire, creature, foley and ambience libraries (many GB). The best source for the campfire and the animals. |
| [Freesound](https://freesound.org) (free account; filter Licence → *Creative Commons 0*) | CC0, or CC-BY with credit | Everything, especially voices, animals and long fire recordings. Check each file's licence. |
| [OpenGameArt](https://opengameart.org) packs below | CC0 (each checked on 2026-11-01) | Game-ready stand-ins and fillers. |

OpenGameArt packs that fit, all CC0:
- [80 CC0 RPG SFX](https://opengameart.org/content/80-cc0-rpg-sfx) by rubberduck: creature hurt and death, blades, stones, wood, item handling, fire spells.
- [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) by rubberduck: metal, tools, wood, doors, a wooden box opening.
- [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) by rubberduck: footsteps (wood, wet), stones, wood and metal hits.
- [42 Snow and Gravel Footsteps](https://opengameart.org/content/42-snow-and-gravel-footsteps) by Corsica_S: snow steps (to replace the placeholder) and gravel.
- [Walking on snow sound](https://opengameart.org/content/walking-on-snow-sound) by IgnasD: snow steps.
- [Fire Crackling](https://opengameart.org/content/fire-crackling) by AntumDeluge: campfire layers, pops.
- [Fireplace Sound loop](https://opengameart.org/content/fireplace-sound-loop) by PagDev: campfire and furnace loops.
- [Swishes Sound Pack](https://opengameart.org/content/swishes-sound-pack) by artisticdude: swings, whirlwind, throws.
- [RPG Sound Pack](https://opengameart.org/content/rpg-sound-pack) by artisticdude: swings, equipment, misc.
- [Bones rattle](https://opengameart.org/content/bones-rattle) by congusbongus: skeleton movement, spawn, death.
- [SkeletonSFXShortPack](https://opengameart.org/content/skeletonsfxshortpack) by Colodical: skeleton voices and attacks.
- [Zombie / Skeleton / Monster Voice Effects](https://opengameart.org/content/zombie-skeleton-monster-voice-effects) by ArcadeParty: skeleton aggro and death.
- [Wolf Monster Sound](https://opengameart.org/content/wolf-monster-sound) by CaveboyTup: wolf aggro.
- [20 Rustles of dry leaves](https://opengameart.org/content/20-rustles-dry-leaves) by qubodup: bush picking, hemp, tree shake.

rubberduck's packs were recorded on a phone, so they're fine as fillers but are the first to replace later.

## P1 still silent, in batches
In order: the batches heard most come first.

### 1. Fire: campfire, furnace, torch (20)
*The heart of the game; one long recording cut into every layer keeps them one fire.*

**Search for:** *campfire loop*, *fireplace crackle*, *bonfire*, *fire whoosh ignite*, *fire extinguish hiss*, *forge fire*, *metal ingot clink*, *torch loop*.

**Try:** OpenGameArt **Fire Crackling** and **Fireplace Sound loop**; Sonniss GDC bundles (fire libraries); Freesound CC0 *campfire*. Cut ember, small, medium and roaring from one long session, quiet to loud. The current medium layer has an unknown licence, so replace it too.

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_campfire_loop_ember` | Faint glowing hiss and the odd tick. Lonely. | 1 |
| `sfx_campfire_loop_small` | Gentle crackle. | 1 |
| `sfx_campfire_loop_roaring` | Big breathing roar plus loud pops. | 1 |
| `sfx_campfire_feed_stick` | Twig catch crackle-flare. | 3 |
| `sfx_campfire_low_warning` | Sputtering gasp. Should make you look at the fire. | 2 |
| `sfx_campfire_out` | Hiss plus a smoke "fff" and a final ember tick, then silence. Dread. | 1 |
| `sfx_campfire_relight` | Flint-less "catch": embers re-flaring into a small flame. | 2 |
| `sfx_campfire_build_complete` | The first fire catching: a match-less flare into small. | 1 |
| `sfx_furnace_loop_ember` | Faint glow inside stone: a low hiss and the odd tick. | 1 |
| `sfx_furnace_loop_small` | Enclosed, muffled crackle. | 1 |
| `sfx_furnace_loop_medium` | A steady, bassy roar, a fire inside stone. | 1 |
| `sfx_furnace_loop_roaring` | A full roar with the chimney drawing hard. | 1 |
| `sfx_furnace_feed` | Fuel thrown into a stone mouth, a muffled whoomf. | 3 |
| `sfx_furnace_input` | Ore clatters into the crucible. | 3 |
| `sfx_furnace_bar_done` | Metallic "tink" plus a quench hiss. | 2 |
| `sfx_furnace_out` | Deep sigh plus cooling-stone ticks. | 1 |
| `sfx_torch_flame_loop` | Small, fluttering flame. Louder when moving fast, quieter when standing. Goes silent when stowed (carrying) or in the bag, since the object is inactive. | 1 |
| `sfx_torch_ignite_target` | "Fwoomp" catch. | 2 |
| `sfx_torch_burnout` | Sputter plus a smoky fizz. A clear "your light is gone" moment. | 2 |
| `sfx_burning_loop` | Crackling fire on a body, medium. | 1 |

### 2. Player: body and voice (9)
*Heard constantly; worth the best picks.*

**Search for:** *male grunt pain*, *death groan*, *shiver cold breath*, *level up chime*, *eating crunch*, *lift effort grunt*, *pick up cloth rustle*, *throw whoosh*, *punch body impact*.

**Try:** Freesound CC0 (voices); OpenGameArt **80 CC0 RPG SFX** (impacts); Kenney **Impact Sounds** (body hits).

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_player_hurt` | Short grunt plus a cloth/flesh thump. The flesh impact itself comes from the player's surface (§13); this is the voice. | 4 |
| `sfx_player_death` | Final exhale or collapse into snow. Leaves a beat of silence before the game-over sting (§11). | 1 |
| `sfx_player_freeze_start` | Sharp shiver gasp plus a frost "crackle" sweep. The warning that health is now draining. | 2 |
| `sfx_player_levelup` | Warm, short fanfare: a rising ember whoosh plus a bell. The best-feeling sound in the game. | 1 |
| `sfx_player_eat_berry` | Small crunchy-juicy bite. | 3 |
| `sfx_player_lift_heavy` | Effort grunt plus the item's pickup sound (§7). | 3 |
| `sfx_player_pickup_small` | Generic "into the pocket" rustle under the item's own pickup sound. | 3 |
| `sfx_player_throw` | Effort grunt plus an air whoosh, scaled to the item's weight. | 3 |
| `sfx_surface_player` | Cloth plus a body thump (with the hurt voice from §3.2). | 4 |

### 3. Footsteps (2)
*Every step the player takes on these grounds.*

**Search for:** *footstep grass*, *footsteps ice*, *walking frozen ground*. Cut single steps from a walking take, 4 to 6 each.

**Try:** Kenney **Impact Sounds** (footsteps on grass, snow and more); OpenGameArt **42 Snow and Gravel Footsteps** and **Walking on snow sound**.

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_ground_step_grass` | Frozen grass: brittle, short crunch, lighter than snow. | 6 |
| `sfx_ground_step_ice` | Hard, hollow boot tap with a faint glassy ring. | 6 |

### 4. Weapons and tool swings (4)
**Search for:** *heavy whoosh*, *swish*, *fire whoosh swing*, *unsheathe weapon*.

**Try:** OpenGameArt **Swishes Sound Pack** and **RPG Sound Pack**; Kenney **RPG Audio** (draw knife, cloth).

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_axe_swing_heavy` | Longer, deeper overhead whoosh plus a grunt. | 2 |
| `sfx_pickaxe_swing` | Whoosh with a slight whistle (pointed head). | 3 |
| `sfx_torch_swing` | Whoosh plus a flame "fwoom" as the fire is dragged through the air. | 3 |
| `sfx_loadout_swap` | Two quick cloth-and-leather moves: stow one, draw the other. Pitched by what's drawn (a metal ring for a sword). | 3 |

### 5. Creatures (15)
**Search for:** *bones rattle*, *skeleton collapse*, *undead groan*, *wolf growl*, *wolf snarl*, *dog yelp*, *pig squeal*, *boar grunt*, *deer bleat*, *hooves gallop*.

**Try:** OpenGameArt **Bones rattle**, **SkeletonSFXShortPack**, **Zombie / Skeleton / Monster Voice Effects**, **Wolf Monster Sound**; Freesound CC0 (real animals); Sonniss GDC bundles (animal libraries).

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_skeleton_spawn` | Frozen ground cracking, snow and dirt crumbling, then bones knitting together, a rising rattle. Unsettling. Play it so the player can *hear* where one rises. | 3 |
| `sfx_skeleton_aggro` | Hollow, rattling hiss or scream. "It's coming for you now." | 3 |
| `sfx_skeleton_attack_windup` | Bone creak of the arm cocking back. A dodge tell. | 2 |
| `sfx_skeleton_attack_swing` | Dry, rattly whoosh. | 3 |
| `sfx_skeleton_death` | The skeleton collapses into a pile of bones, ending on a skull bounce. | 3 |
| `sfx_wolf_aggro` | Deep snarling growl. | 3 |
| `sfx_wolf_bite_windup` | Short snarl. The dodge tell. | 2 |
| `sfx_wolf_hurt` | Sharp yelp. | 4 |
| `sfx_wolf_death` | Pained whimper fading out, then a body flop into snow. | 2 |
| `sfx_boar_hurt` | Loud, shrill squeal. | 4 |
| `sfx_boar_death` | A last squeal that cuts off, then a heavy thud. | 2 |
| `sfx_boar_flee` | Panicked squeal-grunt as it bolts. | 2 |
| `sfx_deer_hurt` | Short bleat or bark. | 3 |
| `sfx_deer_death` | Soft bleat fading out, then a body fold into snow. | 2 |
| `sfx_deer_flee` | Alarm bark (a deer "warning" call) plus the bound-away start. | 2 |

### 6. Skills (7)
**Search for:** *spin whoosh loop*, *whirl*, *throw whoosh*, *axe thunk wood*, *catch grab*.

**Try:** OpenGameArt **Swishes Sound Pack**; Kenney **Impact Sounds** (wood impacts for the stick).

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_whirlwind_start` | Effort shout plus a wind-up whoosh. | 2 |
| `sfx_whirlwind_spin` | Circular whoosh, one per spin, so it rhythmically "chops" the air. Pitch up a little each turn. | 3 |
| `sfx_axethrow_windup` | Overhand effort grunt. | 2 |
| `sfx_axethrow_release` | Release whoosh. | 2 |
| `sfx_axethrow_spin_loop` | "Whup-whup-whup" spinning loop that tracks the axe position. Faster on the return's "whoosh" half. | 1 |
| `sfx_axethrow_stick` | Meaty THUNK of the axe sinking in (the victim's flesh or bone impact plays too). | 2 |
| `sfx_axethrow_catch` | Solid palm-on-handle SLAP. | 2 |

### 7. Resources: ore, stone, wood, plants (10)
**Search for:** *pickaxe rock*, *stone hit*, *rock break*, *metal clang*, *wood split*, *log hit*, *leaves rustle*, *grass pull*.

**Try:** Kenney **Impact Sounds** (mining, wood, metal impacts); OpenGameArt **80 CC0 RPG SFX** (stones, wood), **100 CC0 SFX #2** (stones, wood and metal hits), **20 Rustles of dry leaves**.

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_ore_stone_hit` | `sfx_ore_stone_hit` (§9.2); glance `sfx_ore_glance`. | — |
| `sfx_ore_iron_hit` | `sfx_ore_iron_hit` (§9.2); glance `sfx_ore_glance`. | — |
| `sfx_ore_glance` | The axe: "TING" of stone on stone, sparky and wrong. A fist: dull smack. | 3 |
| `sfx_ore_break_stone` | Rock splitting, then rubble tumble. | 2 |
| `sfx_ore_break_iron` | Heavier split with a metallic clank in the rubble. | 2 |
| `sfx_stone_break` | Rock crack-and-crumble. | 2 |
| `sfx_log_split` | Satisfying wood SPLIT crack plus sticks scattering. | 3 |
| `sfx_surface_wood_log` | Higher, hollower chop into lying wood. | 4 |
| `sfx_bush_pick` | Small "plik" pluck plus a branch spring. | 3 |
| `sfx_hemp_harvest` | Fibrous rip plus snow puff. | 3 |

### 8. Items (12)
**Search for:** *stone drop*, *pebble*, *wood drop*, *pop*, *cloth rustle*, *leather creak*, *chainmail*, *armor equip*.

**Try:** Kenney **RPG Audio** (cloth, leather, metal) and **Impact Sounds**; OpenGameArt **80 CC0 RPG SFX** (item stone, item wood).

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_item_berry_land` | Drop / land on snow: Soft berry patter. | 2 |
| `sfx_item_loot_pop` | Quick "pop" toss, then the item's land sound. | 3 |
| `sfx_item_stone_heavy_land` | Drop / land on snow: Deep stone thump, muffled by snow. | 4 |
| `sfx_item_stone_heavy_pickup` | Pickup: Gritty scrape plus a lift. | 3 |
| `sfx_item_stone_small_land` | Drop / land on snow: Small stone plunk into snow. | 3 |
| `sfx_item_stone_small_pickup` | Pickup: Pebble clack. | 3 |
| `sfx_item_tool_wood_land` | Drop / land on snow: Wooden clatter into snow. | 3 |
| `sfx_item_tool_wood_pickup` | Pickup: Wooden handle grab, a knock. | 3 |
| `sfx_item_wood_heavy_pickup` | Pickup: Hollow log knock plus a lift. | 3 |
| `sfx_equip_cloth` | Cloth swish, a settle. | 2 |
| `sfx_equip_leather` | Strap creak and buckle. | 2 |
| `sfx_equip_metal` | Clank onto the head, a settle. | 2 |

### 9. Building and crafting (8)
**Search for:** *hammer wood*, *construction*, *place wood*, *knapping stone*, *sewing cloth*, *wooden box open*.

**Try:** Kenney **RPG Audio** and **Impact Sounds**; OpenGameArt **100 CC0 SFX** (wood, tools, wooden box open).

| ID | What it should sound like (sound.md) | Var |
|---|---|---|
| `sfx_build_ghost_begin` | Blueprint "unroll", a paper flap. | 1 |
| `sfx_build_ghost_place` | Stake driven into snow, a firm thunk. | 2 |
| `sfx_build_ghost_cancel` | Paper crumple or swoosh away. | 1 |
| `sfx_build_complete` | Construction "poof": wood and stone settling plus a bright confirm, then the structure's own `_build_complete`. | 1 |
| `sfx_craft_wood` | Twigs binding, a cord pulled tight, then a little "done" sparkle. About 0.6 s. |  |
| `sfx_craft_stone_tool` | Lashing a stone head to a haft: cord, knock, sparkle. |  |
| `sfx_craft_cloth` | Weaving and a cloth snap, then the sparkle. |  |
| `sfx_workbench_open` | Wooden knock plus tools shifting on the bench. | 2 |

## Then
- **P2 and P3**: the same way, from Validate's list.
- **The mix session** (SoundSystemPlan Phase 7, step 2), once most P1 sounds are real: group levels first, then event Volume dB, never the clips.
- **Before a release**: Validate shows no licence problems. The current campfire medium loop is flagged until it's replaced.

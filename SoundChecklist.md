# Sound checklist

Every sound event in the game, by `sound.md` section. Tick a box once the event has real, licensed clips (import them with **Tools → Audio → Sound Import**). Boxes already ticked had clips when this list was made on 2026-11-02. **Tools → Audio → Validate** always has the live state.

**Still to find: 285** of 289 — P1 147, P2 85, P3 53.

- **P1** = needed for the game to feel right, **P2** = polish, **P3** = later / not in the game yet.
- **×N** = how many variants `sound.md` asks for (more variants = less repetition; one is still better than none).
- 🔁 = a loop (it needs a clean seam). ⚠ = has clips, but their licence is unknown.
- Where to look and how to import: `SoundSourcing.md`.

## 03 Player  (0/38 done)

### 3.1 Movement

- [ ] `sfx_ground_step_grass` **P1** ×6 — Footfall on Grass — Frozen grass: brittle, short crunch, lighter than snow.
- [ ] `sfx_ground_step_ice` **P1** ×6 — Footfall on Ice — Hard, hollow boot tap with a faint glassy ring.
- [ ] `sfx_ground_step_snow` **P1** ×8 — Each footfall on Snow (GroundSensor.Ground): the player's se_run / ne_run, and the ground layer of every creature's step — Deep, dry snow crunch, slightly squeaky (it's −20 °C). The default and most-heard sound in the game.
- [ ] `sfx_player_carry_layer` **P2** ×4 — Layered on each ground step while Hands.IsCarrying (log or stone; speed ×0.75) — A faint creak of the load and a heavier body. The ground step under it is played a little lower and louder.
- [ ] `sfx_player_ice_slide` **P2** ×1 🔁 — On Ice while GroundSensor.SlipTime > 0 and velocity ≠ input (the steering lag) — Soft boot skid on ice. Volume follows the mismatch between velocity and input.
- [ ] `sfx_ground_step_dirt` **P3** ×6 — Footfall on Dirt (type exists, no patch yet) — Frozen packed earth, dull.
- [ ] `sfx_ground_step_stone` **P3** ×6 — Footfall on Stone (type exists, no patch yet) — Boot on frozen rock, gritty.
- [ ] `sfx_player_stop_skid` **P3** ×3 — Releasing input on ice (long ease-out) — Short scrape as the slide dies.

### 3.2 Body: hurt, cold, health

- [ ] `sfx_player_death` **P1** ×1 — PlayerHealth → Health.Died (game over) — Final exhale or collapse into snow. Leaves a beat of silence before the game-over sting (§11).
- [ ] `sfx_player_freeze_start` **P1** ×2 — PlayerTemperature.IsFreezing turns true (warmth hits 0) — Sharp shiver gasp plus a frost "crackle" sweep. The warning that health is now draining.
- [ ] `sfx_player_hurt` **P1** ×4 — Health.Damaged on the player, DamageType.Combat — Short grunt plus a cloth/flesh thump. The flesh impact itself comes from the player's surface (§13); this is the voice.
- [ ] `sfx_player_cold_tick` **P2** ×3 — PlayerTemperature.ColdOverflow (cold damage reaching health) — Tiny ice crack plus a small grunt. Throttle it to at most one per ~1.5 s.
- [ ] `sfx_player_freezing_loop` **P2** ×1 🔁 — While IsFreezing — Chattering teeth and shaky breath, quiet under everything else.
- [ ] `sfx_player_heal` **P2** ×2 — Health.Healed (food) — Soft warm swell. Combat text shows the number; this is the feel.
- [ ] `sfx_player_hurt_heavy` **P2** ×2 — Combat damage ≥ ~25 % of max health in one hit — Bigger pained gasp.
- [ ] `sfx_player_knockback` **P2** ×3 — Knockback.Apply on the player — Snow scuff of feet sliding back (plays with the hurt sound).
- [ ] `sfx_player_lowhealth_loop` **P2** ×1 🔁 — Health below ~25 % — Muffled heartbeat that speeds up as health drops.
- [ ] `sfx_player_warm_relief` **P2** ×2 — Warmth goes from freezing back above 0, near a HeatSource — Long relieved exhale.
- [ ] `sfx_player_breath_cold` **P3** ×3 — Idle in the cold (PlayerTemperature.Temp below a threshold), every few seconds — Soft visible-breath exhale. Pairs with a breath puff VFX later.
- [ ] `sfx_player_hurt_burn` **P3** ×2 — Damage of DamageType.Fire — Hiss plus a sharp intake of breath.

### 3.3 Hands, carrying, bag

- [ ] `sfx_player_lift_heavy` **P1** ×3 — A Big item goes into a hand (Inventory.TryTake → Hands) — Effort grunt plus the item's pickup sound (§7).
- [ ] `sfx_player_pickup_small` **P1** ×3 — A Small item goes into the bag (E, or walking over a stack the bag has room on) — Generic "into the pocket" rustle under the item's own pickup sound.
- [ ] `sfx_player_autopickup` **P2** ×3 — Walking over a matching stack auto-merges it (Inventory, auto-pickup) — Lighter, quicker pocket tick, so auto-pickups don't sound like deliberate E presses.
- [ ] `sfx_player_fumble` **P2** ×2 — PlayerInteractor.Fumble (picked up with no room, the item drops beside you) — "Whoops" cloth flap. The item's land sound follows.
- [ ] `sfx_player_bag_spill` **P3** ×2 — Inventory.Spill (swapping to a smaller bag) — Several items tumbling out onto snow.

### 3.4 Q: drop, deposit, throw

- [ ] `sfx_player_throw` **P1** ×3 — Q release or left click with the ghost up (PlayerInteractor.Throw → ThrownItem.Launch) — Effort grunt plus an air whoosh, scaled to the item's weight.
- [ ] `sfx_player_throw_aim` **P3** ×1 — Q held past throwHoldTime (DropGhost.Begin) — Soft "wind-up" tick when the ghost appears.

### 3.5 E: interact, equip

- [ ] `sfx_player_hold_equip_fill` **P2** ×1 🔁 — E held on gear: the prompt's hold bar fills (InteractPrompt.SetHold) — Rising soft tone or leather creak over holdTime (0.35 s).
- [ ] `sfx_player_interact_fail` **P3** ×1 — E with nothing usable, or a refused interact (Interactable.Interact returns false) — Soft "nothing there" (very quiet, or none).

### 3.6 Eating, buffs, progression

- [ ] `sfx_player_eat_berry` **P1** ×3 — FoodUse eats a berry (bag click, R, quickslot) — Small crunchy-juicy bite.
- [ ] `sfx_player_levelup` **P1** ×1 — PlayerExperience.LeveledUp — Warm, short fanfare: a rising ember whoosh plus a bell. The best-feeling sound in the game.
- [ ] `sfx_player_eat_denied` **P2** ×1 — R or quickslot click on cooldown, or with no food left (Quickslot) — Muted "empty" tap.
- [ ] `sfx_player_xp_gain` **P2** ×3 — PlayerExperience floats "+N" at a kill — Tiny soft chime, pitched up slightly per kill in a streak.
- [ ] `sfx_weapon_levelup` **P2** ×1 — A weapon's level rises (WeaponExperience.Changed, level compare) — Lighter metal ring plus a whoosh. "Your axe got better."
- [ ] `sfx_player_buff_end` **P3** ×1 — A buff expires (PlayerBuffs.StatsChanged with fewer modifiers) — Faint falling shimmer.
- [ ] `sfx_player_buff_start` **P3** ×1 — PlayerBuffs adds a modifier — Faint warm shimmer.
- [ ] `sfx_player_eat_meat_cooked` **P3** ×2 — Eats Polar Bear Meat Cooked — Satisfying chew plus a swallow.
- [ ] `sfx_player_eat_meat_raw` **P3** ×2 — Eats Polar Bear Meat — Wet, chewy tear. A little gross.

## 04 Weapons and tools  (0/35 done)

### 4.1 Shared swing set

- [ ] `sfx_combat_crit` **P1** ×2 — A hit that crits (DamageInfo crit; combat text already pops it alone) — Extra sharp crack layered over the material impact.
- [ ] `sfx_swing_charge_ready` **P1** ×1 — OnAttackChargeReady, or hold ≥ chargeTime, i.e. a heavy is now guaranteed (the camera zooms in here) — Clear "ting" or ember flare. The player must hear that the heavy is ready.
- [ ] `sfx_swing_glance` **P1** ×3 — Contact on a target this tool can't damage (ToolDamageFilter, StrikeTarget.glancing): a fist or sword on a tree, an axe on ore — Dull, dead thud plus a slight wrist-jar rattle. Must read as "wrong tool", distinct from a real hit.
- [ ] `sfx_combat_kill` **P2** ×2 — The player's blow kills an enemy (IKillReactor) — Low, punchy "thump" under the death vocal, to confirm the kill.
- [ ] `sfx_swing_charge_loop` **P2** ×1 🔁 — Between press and release, once past ~0.1 s — Rising tension (cloth tightening, low hum). Stops on release.
- [ ] `sfx_swing_lunge_step` **P2** ×3 — Lunge-grace step (AttackLunge.BeginStep) — Quick snow scuff of the step-in.

### 4.2 Per weapon

- [ ] `sfx_axe_swing` **P1** ×3 — Stone Axe — When: Light ("hack" style) — Heavy, low whoosh of a stone head.
- [ ] `sfx_axe_swing_heavy` **P1** ×2 — Stone Axe — When: Heavy — Longer, deeper overhead whoosh plus a grunt.
- [ ] `sfx_fists_swing` **P1** ×3 — Fists (empty hand) — When: OnAttackSwing, light — Quick cloth-and-air jab.
- [ ] `sfx_pickaxe_swing` **P1** ×3 — Stone Pickaxe (two-handed) — When: Light — Whoosh with a slight whistle (pointed head).
- [ ] `sfx_torch_swing` **P1** ×3 — Torch, Sap Torch — When: Swing — Whoosh plus a flame "fwoom" as the fire is dragged through the air.
- [ ] `sfx_fists_swing_heavy` **P2** ×2 — Fists — When: Heavy — Bigger haymaker whoosh plus an effort grunt.
- [ ] `sfx_pickaxe_swing_heavy` **P2** ×2 — Stone Pickaxe — When: Heavy — Deeper overhead whoosh plus a grunt.
- [ ] `sfx_arrow_drop` **P3** ×2 — Arrow — When: Speed below the floor, despawns — Soft "fff-tk" into the snow.
- [ ] `sfx_arrow_flight` **P3** ×2 🔁 — Arrow (Projectile) — When: While flying — Short whizz that fades as it slows over the last third of its range.
- [ ] `sfx_bow_draw` **P3** ×2 🔁 — Bow (two-handed) — When: Press / charging — Wood creak plus string stretch, lengthening with the charge.
- [ ] `sfx_bow_ready` **P3** ×1 — Bow — When: Full charge (the longest range) — Taut creak peak (the sfx_swing_charge_ready cue for the bow).
- [ ] `sfx_bow_release` **P3** ×3 — Bow — When: OnAttackHit fires the arrow — String twang plus a thump. Louder when more charged.
- [ ] `sfx_shield_block` **P3** ×3 — Shield — When: A hit arrives while blocking (Health.OnHit with DamageReduction) — Big wooden THUNK plus a grunt. It replaces the flesh impact.
- [ ] `sfx_shield_lower` **P3** ×2 — Shield — When: IsBlocking → false — Softer drop.
- [ ] `sfx_shield_raise` **P3** ×2 — Shield — When: ShieldBlock.IsBlocking → true — Wood-and-leather lift, a slight bump.
- [ ] `sfx_sword_swing` **P3** ×3 — Iron Sword ("slash" style) — When: Light — Thin metallic swish with a faint ring.
- [ ] `sfx_sword_swing_heavy` **P3** ×2 — Iron Sword — When: Heavy — Long, singing sweep.

### 4.3 Torch-specific

- [ ] `sfx_torch_burnout` **P1** ×2 — A torch's Fuel reaches 0 (FireRender turns isOut on, hideWhenOut) — Sputter plus a smoky fizz. A clear "your light is gone" moment.
- [ ] `sfx_torch_flame_loop` **P1** ×1 🔁 — A torch is lit and active: drawn in a hand, or lying in the world (Fuel.fuelLevel > 0, object active) — Small, fluttering flame. Louder when moving fast, quieter when standing. Goes silent when stowed (carrying) or in the bag, since the object is inactive.
- [ ] `sfx_torch_draw` **P1** ×2 — A torch is drawn or switched to: wielded from the bag, a loadout swap, redrawn after carrying (Tool_Torch.draw) — A quick flame "fwoosh" as the fire is pulled out, plus a wooden handle grab. The flame loop swells in under it.
- [ ] `sfx_torch_ignite_target` **P1** ×2 — Flammable.Ignite (a torch hit sets something on fire) — "Fwoomp" catch.
- [ ] `sfx_torch_stow` **P2** ×2 — A torch is stowed onto the back while carrying (Tool_Torch.stow) — A soft flutter-snuff as the flame is tucked away.
- [ ] `sfx_torch_low` **P2** ×2 — Torch fuel crosses ~15 % — Flame sputter (once).
- [ ] `sfx_torch_refuel` **P2** ×2 — Fuel dragged onto a torch (Inventory.TryFeed → FuelReceiver.Deposit) — Stick: dry crackle and catch. Sap: sticky hiss-flare.
- [ ] `sfx_torch_relight` **P2** ×1 — Fuel added to an out torch (flame reappears) — Small "fwip" ignition.

### 4.4 Loadout (weapon sets A/B)

- [ ] `sfx_loadout_swap` **P1** ×3 — Scroll or Tab swaps sets (Loadout swap; IsSwapping for swapTime 0.3 s) — Two quick cloth-and-leather moves: stow one, draw the other. Pitched by what's drawn (a metal ring for a sword).
- [ ] `sfx_loadout_draw` **P2** ×2 — Putting the load down redraws the loadout — Quick draw.
- [ ] `sfx_loadout_stow` **P2** ×2 — Picking up a load stows the loadout onto the back (Hands, BackStow shows the X) — Weapons slapping onto the back strap.
- [ ] `sfx_loadout_swap_denied` **P3** ×1 — Swap pressed mid-swing (refused) — Very quiet tick, or none.

## 05 Skills  (0/12 done)

### 5. Skills

- [ ] `sfx_axethrow_catch` **P1** ×2 — Thrown Axe — When: Arriving at the hand (caught): "a punch and a jolt of the camera" — Solid palm-on-handle SLAP.
- [ ] `sfx_axethrow_release` **P1** ×2 — Axe Throw — When: OnAttackHit lets go → ThrownAxe spawns — Release whoosh.
- [ ] `sfx_axethrow_spin_loop` **P1** ×1 🔁 — Thrown Axe — When: Phases Out and Back — "Whup-whup-whup" spinning loop that tracks the axe position. Faster on the return's "whoosh" half.
- [ ] `sfx_axethrow_stick` **P1** ×2 — Thrown Axe — When: Contact: hits its target and goes Stuck — Meaty THUNK of the axe sinking in (the victim's flesh or bone impact plays too).
- [ ] `sfx_axethrow_windup` **P1** ×2 — Axe Throw — When: Throw clip starts (cast) — Overhand effort grunt.
- [ ] `sfx_whirlwind_spin` **P1** ×3 — Whirlwind — When: Each turn: OnWhirlwindHit (once per clip loop) — Circular whoosh, one per spin, so it rhythmically "chops" the air. Pitch up a little each turn.
- [ ] `sfx_whirlwind_start` **P1** ×2 — Whirlwind — When: PlayCast("whirlwind") begins — Effort shout plus a wind-up whoosh.
- [ ] `sfx_axethrow_miss_turn` **P2** ×1 — Thrown Axe — When: No target: it slows and turns back — Whoosh dips in pitch and swings around.
- [ ] `sfx_axethrow_rip` **P2** ×2 — Thrown Axe — When: Stuck → Back (stickTime over) — Wet or woody rip-out.
- [ ] `sfx_skill_cast_denied` **P2** ×1 — any — When: Key 1/2 with nothing slotted, no hand holding the weapon, the hand locked, or on cooldown — Muted "denied" thunk.
- [ ] `sfx_skill_ready` **P2** ×1 — any — When: The cooldown finishes (IActiveSkill cooldown → 0) — Small bright "ready" ping, quiet.
- [ ] `sfx_whirlwind_end` **P2** ×1 — Whirlwind — When: The last spin ends — Exhale plus a slowing whoosh tail.

## 06 Enemies and wildlife  (1/43 done)

### 6.1 Skeleton (`Enemy.prefab`; 6 in the scene, plus the campfire's spawner)

- [ ] `sfx_skeleton_aggro` **P1** ×3 — EnemyBrain goes Wander → Combat (the first hit taken) — Hollow, rattling hiss or scream. "It's coming for you now."
- [ ] `sfx_skeleton_attack_swing` **P1** ×3 — OnAttackSwing (FistsEnemy) — Dry, rattly whoosh.
- [ ] `sfx_skeleton_attack_windup` **P1** ×2 — *_attack clip starts (EnemyAttacker arms a swing) — Bone creak of the arm cocking back. A dodge tell.
- [ ] `sfx_skeleton_death` **P1** ×3 — Health.Died → *_die — The skeleton collapses into a pile of bones, ending on a skull bounce.
- [ ] `sfx_skeleton_hurt` **P1** ×4 — Health.Damaged / se_hit / ne_hit — Bone crack plus a rattle shake. (The bone impact material carries the hit itself.)
- [ ] `sfx_skeleton_spawn` **P1** ×3 — se_spawn / ne_spawn plays (EnemyAnimator Start; EnemySpawner spawned it) — Frozen ground cracking, snow and dirt crumbling, then bones knitting together, a rising rattle. Unsettling. Play it so the player can *hear* where one rises.
- [ ] `sfx_foot_bone` **P2** ×6 — Foot layer on each skeleton footfall in *_run (the ground step plays under it) — Light bony clack.
- [ ] `sfx_skeleton_despawn` **P2** ×2 — EnemyDespawner.Despawned (09:00) — Bones crumbling to frost or dust, a soft sigh. Morning relief.
- [ ] `sfx_skeleton_idle` **P2** ×4 — While wandering, every 4–10 s at random — Faint bone creak or jaw click.

### 6.2 Wolf (`Wolf.prefab`; 6 in the scene)

- [ ] `sfx_wolf_aggro` **P1** ×3 — Wander → Combat — Deep snarling growl.
- [ ] `sfx_wolf_bite` **P1** ×3 — OnAttackHit (frame 5) — Jaw SNAP. Plays whether it lands or not; a landed bite adds the player's hurt sound.
- [ ] `sfx_wolf_bite_windup` **P1** ×2 — *_bite starts (swing frame 4) — Short snarl. The dodge tell.
- [ ] `sfx_wolf_death` **P1** ×2 — Health.Died → *_death — Pained whimper fading out, then a body flop into snow.
- [ ] `sfx_wolf_hurt` **P1** ×4 — Health.Damaged — Sharp yelp.
- [ ] `sfx_wolf_howl_distant` **P2** ×4 — Ambience at night (§11.2), not tied to an actual wolf — Lonely far howl, sometimes answered.
- [ ] `sfx_wolf_idle` **P2** ×4 — Wandering, every 5–12 s — Panting, sniffing, a low huff.
- [ ] `sfx_foot_paw` **P3** ×6 — Foot layer on each wolf footfall in *_run — Soft padded paw, quick.
- [ ] `sfx_wolf_chase_loop` **P3** ×1 🔁 — In Combat and moving — Breathy panting and growl under the steps.

### 6.3 Boar (`Boar.prefab`; 3 in the scene)

- [ ] `sfx_boar_death` **P1** ×2 — Health.Died — A last squeal that cuts off, then a heavy thud.
- [x] `sfx_boar_flee` **P1** ×2 — FleeBehavior.Begin (Flee state starts) — Panicked squeal-grunt as it bolts.
- [ ] `sfx_boar_hurt` **P1** ×4 — Health.Damaged — Loud, shrill squeal.
- [ ] `sfx_boar_flee_loop` **P2** ×1 🔁 — In Flee, moving — Fast hoof-drumming plus panicked huffs.
- [ ] `sfx_boar_idle` **P2** ×5 — Wandering, every 4–10 s — Contented grunts, snuffling and rooting in snow.
- [ ] `sfx_foot_hoof` **P3** ×6 — Foot layer on each boar footfall — Short hoof trot, heavy-ish.

### 6.4 Deer (`Deer.prefab`; 2 in the scene)

- [ ] `sfx_deer_death` **P1** ×2 — Health.Died — Soft bleat fading out, then a body fold into snow.
- [ ] `sfx_deer_flee` **P1** ×2 — FleeBehavior.Begin — Alarm bark (a deer "warning" call) plus the bound-away start.
- [ ] `sfx_deer_hurt` **P1** ×3 — Health.Damaged — Short bleat or bark.
- [ ] `sfx_deer_idle` **P2** ×4 — Wandering, every 5–14 s — Soft snort, chewing, an ear-flick rustle.
- [ ] `sfx_foot_hoof_light` **P3** ×6 — Foot layer on each deer footfall in *_walk / *_run (louder when running) — Delicate hoof tap; the bounding run plays it harder.

### 6.5 Polar Bear (`Polar Bear.prefab`; not in the scene) — P3

- [ ] `sfx_bear_aggro` **P3** ×2 — → Combat — Big roar.
- [ ] `sfx_bear_attack` **P3** ×3 — Swing — Huff plus a heavy claw swipe whoosh.
- [ ] `sfx_bear_death` **P3** ×1 — Died — Long groan, then a massive body thud.
- [ ] `sfx_bear_hurt` **P3** ×3 — Damaged — Pained bellow.
- [ ] `sfx_bear_idle` **P3** ×3 — Wandering — Deep huffs and sniffing.
- [ ] `sfx_foot_heavypaw` **P3** ×6 — Foot layer on each polar bear footfall — Heavy, slow paw thud.

### 6.6 Target Dummy (`TargetDummy.prefab`; not in the scene) — P3

- [ ] `sfx_dummy_break` **P3** ×1 — Died — Straw burst plus wood snap.
- [ ] `sfx_dummy_hit` **P3** ×4 — Hit — Straw-and-sack thump plus a wooden post creak (a "straw" material, §13).

### 6.7 Shared creature reactions

- [ ] `sfx_burning_loop` **P1** ×1 🔁 — While Flammable is burning (BurningEffect child) — Crackling fire on a body, medium.
- [ ] `sfx_burning_extinguish` **P2** ×2 — Flammable.Extinguish (timer out, or death) — Smoky fizz-out.
- [ ] `sfx_burning_tick` **P2** ×3 — Each tickDamage — Small sizzle pop.
- [ ] `sfx_creature_knockback` **P2** ×3 — Knockback.Apply on any enemy — Snow scuff of the shove. Longer on ice (KnockbackSlide).
- [ ] `sfx_blood_splatter` **P3** ×3 — BloodSplatterOnHit drops a GroundSplatter — Very soft wet patter on snow, under the impact.
- [ ] `sfx_creature_knockback_ice` **P3** ×2 — The shove happens on Ice — Body sliding on ice.

## 07 Items  (0/47 done)

### 7.1 Item material sets

- [ ] `sfx_item_berry_land` **P1** ×2 — Drop / land on snow: Soft berry patter.
- [ ] `sfx_item_stick_land` **P1** ×3 — Drop / land on snow: Light twig tumble.
- [ ] `sfx_item_stick_pickup` **P1** ×3 — Pickup: Dry twig clatter.
- [ ] `sfx_item_stone_heavy_land` **P1** ×4 — Drop / land on snow: Deep stone thump, muffled by snow.
- [ ] `sfx_item_stone_heavy_pickup` **P1** ×3 — Pickup: Gritty scrape plus a lift.
- [ ] `sfx_item_stone_small_land` **P1** ×3 — Drop / land on snow: Small stone plunk into snow.
- [ ] `sfx_item_stone_small_pickup` **P1** ×3 — Pickup: Pebble clack.
- [ ] `sfx_item_tool_wood_land` **P1** ×3 — Drop / land on snow: Wooden clatter into snow.
- [ ] `sfx_item_tool_wood_pickup` **P1** ×3 — Pickup: Wooden handle grab, a knock.
- [ ] `sfx_item_wood_heavy_land` **P1** ×4 — Drop / land on snow: Heavy wooden THUD into snow, with a dull bounce.
- [ ] `sfx_item_wood_heavy_pickup` **P1** ×3 — Pickup: Hollow log knock plus a lift.
- [ ] `sfx_item_armour_cloth_land` **P2** ×2 — Drop / land on snow: Soft flop.
- [ ] `sfx_item_armour_cloth_pickup` **P2** ×2 — Pickup: Cloth swish.
- [ ] `sfx_item_armour_metal_land` **P2** ×2 — Drop / land on snow: Helmet bonk and roll.
- [ ] `sfx_item_armour_metal_pickup` **P2** ×2 — Pickup: Metal clank.
- [ ] `sfx_item_fibre_land` **P2** ×2 — Drop / land on snow: Soft cloth flop.
- [ ] `sfx_item_fibre_pickup` **P2** ×2 — Pickup: Soft fibrous rustle.
- [ ] `sfx_item_leather_land` **P2** ×2 — Drop / land on snow: Leather flop.
- [ ] `sfx_item_leather_pickup` **P2** ×2 — Pickup: Leather creak or flap.
- [ ] `sfx_item_metal_bar_land` **P2** ×3 — Drop / land on snow: Dull clank (muffled by snow).
- [ ] `sfx_item_metal_bar_pickup` **P2** ×2 — Pickup: Metal clink, weighty.
- [ ] `sfx_item_sap_land` **P2** ×2 — Drop / land on snow: Soft "plap".
- [ ] `sfx_item_sap_pickup` **P2** ×2 — Pickup: Sticky squelch.
- [ ] `sfx_item_meat_land` **P3** ×2 — Drop / land on snow: Wet thud.
- [ ] `sfx_item_meat_pickup` **P3** ×2 — Pickup: Wet slap.
- [ ] `sfx_item_tool_metal_land` **P3** ×3 — Drop / land on snow: Metal clatter, a short ring.
- [ ] `sfx_item_tool_metal_pickup` **P3** ×2 — Pickup: Metal-on-leather grab.

### 7.2 Item-specific moments

- [ ] `sfx_item_loot_pop` **P1** ×3 — DropOnHit / DropOnDeath scatter loot (logs and sap off a tree, stone off ore, meat off a bear) — Quick "pop" toss, then the item's land sound.

### 7.3 Hittable carried items (the log and the big stone)

- [ ] `sfx_log_split` **P1** ×3 — Log Health.Died → 3 Sticks (DropOnDeath) — Satisfying wood SPLIT crack plus sticks scattering.
- [ ] `sfx_stone_break` **P1** ×2 — Stone Died → 3 Rocks — Rock crack-and-crumble.
- [ ] `sfx_log_bump` **P2** ×3 — Walking into a log (CollideAnimation → "collide") — Soft wooden knock or roll.
- [ ] `sfx_stone_bump` **P3** ×2 — Walking into the big stone (CollideAnimation) — Faint scrape (the stone barely moves).
- [ ] `sfx_log_hit_pickaxe` **P1** ×3 — A pickaxe hits a log (no damage; Surface_WoodLog.byTool) — The pick point stabbing lying wood: a hollow "thok" plus a splinter.
- [ ] `sfx_log_hit_fist` **P1** ×3 — A fist hits a log (no damage) — A soft, hollow knock on a log.
- [ ] `sfx_log_hit_sword` **P1** ×3 — A sword hits a log (no damage) — A shallow blade "tock" into lying wood, a thin ring.
- [ ] `sfx_log_hit_torch` **P1** ×3 — A torch hits a log (no damage) — Wood on wood, a hollow clack plus a small ember fizz.
- [ ] `sfx_stone_hit_axe` **P1** ×3 — An axe hits the big stone (no damage; Surface_Rock.byTool) — The axe head clanging off a loose boulder: a "TING" with a little rock shift.
- [ ] `sfx_stone_hit_fist` **P1** ×3 — A fist hits the big stone (no damage) — A dull smack on rock.
- [ ] `sfx_stone_hit_sword` **P1** ×3 — A sword hits the big stone (no damage) — A metal scrape-clang on rock.
- [ ] `sfx_stone_hit_torch` **P1** ×3 — A torch hits the big stone (no damage) — A wood knock on rock plus an ember fizz.

### 7.4 Bag and gear (world → body)

- [ ] `sfx_equip_cloth` **P1** ×2 — Cloth gear worn (Burlap Pants) — Cloth swish, a settle.
- [ ] `sfx_equip_leather` **P1** ×2 — A bag worn (Pouch, Small Bag) — Strap creak and buckle.
- [ ] `sfx_equip_metal` **P1** ×2 — Metal gear worn (Iron Helmet): from the bag, Hold E, or the Player tab — Clank onto the head, a settle.
- [ ] `sfx_bag_swap` **P2** ×1 — Wearing a new bag (the contents carry over) — Two straps and a rustle of items shifting.
- [ ] `sfx_unequip_cloth` **P2** ×2 — Cloth gear taken off — A softer swish.
- [ ] `sfx_unequip_metal` **P2** ×2 — Metal gear taken off into the bag — A softer, reversed clank.
- [ ] `sfx_unequip_leather` **P3** ×1 — (bags are only swapped, never taken off; kept for leather gear later) — A strap loosening.

## 08 Stations and fire  (0/35 done)

### 8.1 Campfire (`Campfire.prefab`)

- [ ] `sfx_campfire_build_complete` **P1** ×1 — PlacementGhost.Complete for the campfire — The first fire catching: a match-less flare into small.
- [ ] `sfx_campfire_feed_log` **P1** ×3 — FuelAdded with Wood — Heavy log drop into coals, a WHOOMF plus a spark burst.
- [ ] `sfx_campfire_feed_stick` **P1** ×3 — Fuel.FuelAdded with a Stick (Q, a throw, the station panel) — Twig catch crackle-flare.
- [ ] `sfx_campfire_loop_ember` **P1** ×1 🔁 — FireRender stage 0 — Faint glowing hiss and the odd tick. Lonely.
- [ ] `sfx_campfire_loop_medium` **P1** ×1 🔁 ⚠ licence unknown: sfx_campfire_loop_medium_01.wav — Stage 2 — Steady, cosy crackle (campfire.wav could be this one).
- [ ] `sfx_campfire_loop_roaring` **P1** ×1 🔁 — Stage 3 — Big breathing roar plus loud pops.
- [ ] `sfx_campfire_loop_small` **P1** ×1 🔁 — Stage 1 — Gentle crackle.
- [ ] `sfx_campfire_low_warning` **P1** ×2 — Fuel crosses ~15 % (still stage 0) — Sputtering gasp. Should make you look at the fire.
- [ ] `sfx_campfire_out` **P1** ×1 — fuelLevel hits 0 → outSprite — Hiss plus a smoke "fff" and a final ember tick, then silence. Dread.
- [ ] `sfx_campfire_pop` **P1** ×6 — Random every 1–6 s, more often at high fuel — Single sharp ember pop or log shift. Breaks up the loop's repetition.
- [ ] `sfx_campfire_relight` **P1** ×2 — Fuel added to an out fire — Flint-less "catch": embers re-flaring into a small flame.
- [ ] `sfx_campfire_feed_sap` **P2** ×2 — FuelAdded with Sap — Resin hiss and flare, a bright sizzle.
- [ ] `sfx_campfire_stage_up` **P2** ×2 — FuelStage rises — A breathy "whoosh" of the fire growing.
- [ ] `sfx_campfire_cook_done` **P3** ×1 — A job finishes into the output (ItemProcessor) — Sizzle swell plus a soft "done" pop.
- [ ] `sfx_campfire_cook_loop` **P3** ×1 🔁 — CookingProcessor has a job and Fuel is burning — Meat sizzle over the crackle.

### 8.2 Furnace (`Furnace.prefab`)

- [ ] `sfx_furnace_bar_done` **P1** ×2 — A job finishes (Iron Bar into the output) — Metallic "tink" plus a quench hiss.
- [ ] `sfx_furnace_feed` **P1** ×3 — FuelAdded — Fuel thrown into a stone mouth, a muffled whoomf.
- [ ] `sfx_furnace_input` **P1** ×3 — Ore deposited (ProcessorInputReceiver) — Ore clatters into the crucible.
- [ ] `sfx_furnace_loop_ember` **P1** ×1 🔁 — FireRender stage 0 — Faint glow inside stone: a low hiss and the odd tick.
- [ ] `sfx_furnace_loop_medium` **P1** ×1 🔁 — Stage 2 — A steady, bassy roar, a fire inside stone.
- [ ] `sfx_furnace_loop_roaring` **P1** ×1 🔁 — Stage 3 — A full roar with the chimney drawing hard.
- [ ] `sfx_furnace_loop_small` **P1** ×1 🔁 — Stage 1 — Enclosed, muffled crackle.
- [ ] `sfx_furnace_out` **P1** ×1 — Fuel hits 0 — Deep sigh plus cooling-stone ticks.
- [ ] `sfx_furnace_build_complete` **P2** ×1 — Built — Stones settling, then a first ignition rumble.
- [ ] `sfx_furnace_smelt_loop` **P2** ×1 🔁 — A job is running and fuel is burning — Low bubbling of molten metal plus a hum.
- [ ] `sfx_furnace_output_full` **P3** ×1 — The output is full or mismatched, so the job waits at full progress — Single dull warning knock (once per stall).

### 8.3 Workbench (`Workbench.prefab`)

- [ ] `sfx_workbench_open` **P1** ×2 — E toggles HudMenu.OpenCrafting(atWorkbench: true) — Wooden knock plus tools shifting on the bench.
- [ ] `sfx_workbench_build_complete` **P2** ×1 — Built — Hammering flourish: three taps and a settle.
- [ ] `sfx_workbench_close` **P2** ×1 — E again, or the menu closed — Lighter knock.

### 8.4 Building: placement and build sites

- [ ] `sfx_build_complete` **P1** ×1 — All satisfied → PlacementGhost.Complete (the ghost turns opaque) — Construction "poof": wood and stone settling plus a bright confirm, then the structure's own _build_complete.
- [ ] `sfx_build_ghost_begin` **P1** ×1 — PlacementGhost.Begin (a Placeable recipe clicked; the menu closes) — Blueprint "unroll", a paper flap.
- [ ] `sfx_build_ghost_cancel` **P1** ×1 — PlacementGhost.CancelActive (Esc or F) — Paper crumple or swoosh away.
- [ ] `sfx_build_ghost_place` **P1** ×2 — Left click places it → BuildRequirement added — Stake driven into snow, a firm thunk.
- [ ] `sfx_build_ingredient_done` **P2** ×1 — One ingredient type becomes satisfied — Small confirm chime.

### 8.5 Heat (not a station)

- [ ] `sfx_heat_enter` **P2** ×1 — Player enters a HeatSource range from the cold — A soft warm swell in the mix (the wind ducks a little).

## 09 Environment props  (3/27 done)

### 9.1 Trees: Scots Pine (4), Grey Alder (8)

- [x] `sfx_tree_chop` **P1** ×5 — An axe hit deals damage (wood material, §13) — Solid "THOCK" of stone axe into frozen trunk.
- [ ] `sfx_tree_hit_pickaxe` **P1** ×3 — A pickaxe hits the tree (no damage; Surface_Wood.byTool) — The pick point biting bark: a dull "thunk" plus a splinter.
- [ ] `sfx_tree_hit_fist` **P1** ×3 — A fist hits the tree (no damage) — A knuckle or glove smack on a frozen trunk, soft and dull.
- [ ] `sfx_tree_hit_sword` **P1** ×3 — A sword hits the tree (no damage) — The blade biting shallow: a short wood "tock" with a thin ring.
- [ ] `sfx_tree_hit_torch` **P1** ×3 — A torch hits the tree (no damage) — A wooden club knock plus a small ember fizz.
- [ ] `sfx_tree_glance` **P1** ×2 — Any other tool hits the tree (an arrow; no damage) — A duller wood knock. The weapon's sfx_swing_glance plays with it.
- [ ] `sfx_tree_fell` **P1** ×2 — Health.Died (the tree vanishes, loot pops) — Big crack, a falling swoosh, and a muffled CRASH into snow with a snow-burst tail. The tree has no fall animation yet, so the sound can carry the moment.
- [ ] `sfx_tree_shake` **P1** ×4 — HitAnimation "Hit" plays (on any hit, glance included) — Needles and branches shaking, plus snow sliding off branches (a soft "fwump").
- [ ] `sfx_tree_creak` **P2** ×3 — Health below ~30 % after a hit — A deep, ominous creak (the tree is about to go).
- [ ] `sfx_tree_wind_creak` **P3** ×4 — Ambient: random trees creak in strong wind, at night — Distant trunk creaks.

### 9.2 Ore: Stone Ore (5), Iron Ore (4)

- [ ] `sfx_ore_break_iron` **P1** ×2 — Iron Ore Died — Heavier split with a metallic clank in the rubble.
- [ ] `sfx_ore_break_stone` **P1** ×2 — Stone Ore Died — Rock splitting, then rubble tumble.
- [ ] `sfx_ore_glance` **P1** ×3 — Any other tool on ore or the big stone (an arrow) — A short stone knock.
- [ ] `sfx_ore_hit_axe` **P1** ×3 — An axe hits Stone Ore or Iron Ore (no damage; byTool) — "TING" of the axe head on rock, sparky and wrong.
- [ ] `sfx_ore_hit_fist` **P1** ×3 — A fist hits ore (no damage) — A dull smack on rock.
- [ ] `sfx_ore_hit_sword` **P1** ×3 — A sword hits ore (no damage) — A metal scrape-clang on rock.
- [ ] `sfx_ore_hit_torch` **P1** ×3 — A torch hits ore (no damage) — A wood knock on rock plus an ember fizz.
- [x] `sfx_ore_iron_hit` **P1** ×5 — A pickaxe hit on Iron Ore (an iron-ore material) — Rock hit with a metallic ring under it.
- [x] `sfx_ore_stone_hit` **P1** ×5 — A pickaxe hit on Stone Ore (stone material) — Sharp rock "chk" plus small debris.

### 9.3 Bushes and plants

- [ ] `sfx_bush_pick` **P1** ×3 — A berry picked from a bush (its child berries are taken: E pickup) — Small "plik" pluck plus a branch spring.
- [ ] `sfx_bush_rustle` **P1** ×5 — Walking through a berry bush (CollideAnimation "Colliding" → true; 44 in the scene) — Frozen-leaf rustle plus a little snow shaken off. Throttle it, since bushes are everywhere.
- [ ] `sfx_hemp_harvest` **P1** ×3 — Harvestable.Interact (E: you get Hemp; the leaves and snow layers hide) — Fibrous rip plus snow puff.
- [ ] `sfx_hemp_rustle` **P2** ×3 — Walking through a Hemp Plant (CollideAnimation; 5 in the scene) — Dry stalk swish.
- [ ] `sfx_bush_regrow` **P3** ×1 — BerryRespawner grows a new berry (after 60 s) — None, or a near-silent soft pop if the player is close.

### 9.4 Snow and ground (non-footstep)

- [ ] `sfx_ground_land_snow` **P1** ×4 — Things landing on snow (an item drops, a body falls, a tree fells) — A generic soft snow-burst layer to put under land sounds.
- [ ] `sfx_ground_land_grass` **P2** ×3 — Things landing on frozen grass — A short brittle rustle under the land sound.
- [ ] `sfx_ground_land_ice` **P2** ×3 — Things landing on ice — A hard tap with a little skitter.

## 10 HUD and UI  (0/25 done)

### 10.1 Shared UI kit

- [ ] `sfx_ui_click` **P1** ×3 — Generic button or tab press — Short wooden tap.
- [ ] `sfx_ui_close` **P1** ×2 — A panel closes — Flap close.
- [ ] `sfx_ui_denied` **P1** ×2 — Any refused action: crafting without ingredients, a locked node, a wrong drop, a full torch, a cast with no hand — Muted double thunk, low.
- [ ] `sfx_ui_open` **P1** ×2 — A panel opens — Leather or paper flap open.
- [ ] `sfx_ui_pickup` **P1** ×2 — Start dragging anything (an item, a spell) — Lift: soft paper or cloth lift.
- [ ] `sfx_ui_place` **P1** ×3 — Dropping a dragged thing onto a valid target — Set down: a satisfying soft "tock".
- [ ] `sfx_ui_return` **P2** ×1 — A drag released over nothing valid and returned to its slot — Soft reverse whoosh.
- [ ] `sfx_ui_slider_tick` **P2** ×1 — Dragging a volume slider in the Audio settings (throttled), on that slider's own bus — Tiny soft tick, so you hear the level you're setting.
- [ ] `sfx_ui_swap` **P2** ×2 — A drop that swaps two things — Two quick tocks.
- [ ] `sfx_ui_toss` **P2** ×1 — A drag released over the world (the item goes on the ground) — Lift, then the item's real drop sound in the world.
- [ ] `sfx_ui_hover` **P3** ×2 — Hovering a clickable box (button, tab, blueprint, skill node) — Barely-there soft tick. Optional; keep it very quiet.

### 10.2 F menu (`HudMenu`, `TabButtonUI`)

- [ ] `sfx_menu_tab` **P1** — Clicking the Player, Crafting or Skills tab (TabButtonUI.Clicked) — Page turn.
- [ ] `sfx_menu_subtab` **P2** — Sub-tabs: Crafting Hands / Workbench; Skills Axe / Sword / Pickaxe / Wand / Bow — Smaller page flick.

### 10.4 Action bar (`ActionBarUI`, `ActionSlotUI`, `QuickslotUI`)

- [ ] `sfx_actionbar_spell_assign` **P2** — A learned spell dropped from a skill tree onto skill slot 1/2 — Rune "slot-in": a soft magical tock

### 10.5 Crafting tab (`CraftingTabUI`, `BlueprintCellUI`, `IngredientRowUI`)

- [ ] `sfx_craft_cloth` **P1** — A cloth recipe: Burlap, Burlap Pants, Small Bag — Weaving and a cloth snap, then the sparkle.
- [ ] `sfx_craft_stone_tool` **P1** — A stone tool: Stone Axe, Stone Pickaxe — Lashing a stone head to a haft: cord, knock, sparkle.
- [ ] `sfx_craft_wood` **P1** — CraftingController.Crafted, a wood recipe: Torch, Bow — Twigs binding, a cord pulled tight, then a little "done" sparkle. About 0.6 s.
- [ ] `sfx_craft_metal` **P2** — A metal recipe: Iron Sword — Hammer on hot iron, two strikes and a ring.
- [ ] `sfx_craft_sap` **P2** — A sap recipe: Sap Torch — Sticky squelch and a wrap, then the sparkle.
- [ ] `sfx_craft_unlocked` **P3** — A recipe becomes craftable while the tab is open (the cell undims) — Very soft "available" twinkle. Optional.

### 10.6 Skills tab (`SkillsTabUI`, `SkillTreeUI`, `SkillNodeUI`)

- [ ] `sfx_skill_node_buy` **P1** — A click spends a point (a passive rank goes up) — Carved-rune "clunk" plus a rising tone; pitch rises with the rank.
- [ ] `sfx_skill_spell_learn` **P1** — A Spell node learned (maxRank 1) — Bigger magical whoosh-and-bell
- [ ] `sfx_skill_node_maxed` **P2** — A node reaches max rank (children unlock) — Fuller chord, and the connecting lines "light up"

### 10.8 Prompts, tooltips, nameplates, combat text

- [ ] `sfx_prompt_focus` **P3** — E focus moves to a new interactable (PlayerInteractor focus change; the highlight turns on) — Extremely soft tick, or silent. Don't make this annoying.

### 10.9 Game menus (`GameStateManager`)

- [ ] `sfx_menu_main_play` **P1** — Main menu "Play" (PlayGame) — A big, warm ember whoosh as the fire lights; the music crossfades into the game

## 11 World  (0/17 done)

### 11.1 Ambience beds (always-on loops)

- [ ] `amb_wind_day` **P1** 🔁 — Daytime phases (Dawn, Morning, Noon, Afternoon) — Steady, cold, open-field wind with soft gusts. Low and constant.
- [ ] `amb_wind_night` **P1** 🔁 — Dusk and Night (DayNightCycle.IsNight) — Harsher, whistling wind plus occasional strong gusts. Crossfade it in over Dusk.
- [ ] `amb_dawn_birds` **P2** ×4 — Dawn → Morning — A few sparse, cold crows or ravens. Life returning.
- [ ] `amb_forest_creak` **P2** ×6 — Random, when near trees — Distant trunk creaks, branch snaps, snow dropping off a branch.
- [ ] `amb_gust` **P2** ×6 — Random every 10–30 s, louder at night — One-shot gust swell plus snow hiss across the ground.
- [ ] `amb_night_owl` **P3** ×3 — Night — Owl calls, rare.

### 11.2 Time of day (`DayNightCycle.PhaseChanged`, `NightChanged`)

- [ ] `sfx_time_dawn` **P1** — PhaseChanged(Dawn) / NightChanged(false) — Soft rising warmth and the first bird. Relief.
- [ ] `sfx_time_dusk` **P1** — PhaseChanged(Dusk) — Low, ominous swell. The dark is coming.
- [ ] `sfx_time_night` **P1** — PhaseChanged(Night), 22:00: the skeleton hours (IsNight already starts at dusk, so it can't be the cue) — Deeper sting plus a distant howl. Make it unmistakable.

### 11.3 Music (states)

- [ ] `mus_combat_layer` **P1** 🔁 — CombatState.Changed(true) → false after leaveDelay — A percussive layer added on top of the day or night music; it fades out when combat ends.
- [ ] `mus_game_over` **P1** — GameState.GameOver ("You died!") — A short, mournful sting, then quiet wind.
- [ ] `mus_game_won` **P1** 🔁 — GameState.GameWon ("You survived the Night!") via WinCondition — Warm, triumphant sunrise sting into a gentle loop.
- [ ] `mus_main_menu` **P1** 🔁 — GameState.MainMenu — A sparse, lonely, warm theme: one instrument by a fire.
- [ ] `mus_night_tension` **P1** 🔁 — Playing, at night — A low drone and pulse that rises with the number of skeletons alive (or with distance from the fire).
- [ ] `mus_day_explore` **P2** 🔁 — Playing, daytime, not in combat — Very sparse, mostly silence with occasional motifs. Don't fight the ambience.
- [ ] `mus_freezing_layer` **P2** 🔁 — PlayerTemperature.IsFreezing — A dissonant, icy high layer. Danger.
- [ ] `mus_low_fire_layer` **P2** 🔁 — Campfire fuel < ~15 % — A thin, nervous layer: the fire is dying.

## 13 Surface materials  (0/10 done)

### 13. Surface materials (SurfaceSounds sets)

- [ ] `sfx_surface_bone` **P1** ×5 — Dry crack plus a bone rattle. No blood: skeletons shouldn't bleed.
- [ ] `sfx_surface_flesh` **P1** ×5 — Wolf, Boar, Deer, Polar Bear; any tool without its own sound (an arrow) — Meaty thwack plus a wet layer.
- [ ] `sfx_surface_flesh_axe` **P1** ×4 — An axe hit on a creature (Surface_Flesh.byTool) — A heavy chop into meat: a deep "thunk" with a wet bite.
- [ ] `sfx_surface_flesh_pickaxe` **P1** ×3 — A pickaxe hit on a creature (Surface_Flesh.byTool) — A blunt stab: a dull punch with a short wet crunch.
- [ ] `sfx_surface_flesh_fist` **P1** ×4 — A punch on a creature (Surface_Flesh.byTool) — A body thump into fur, no wet layer.
- [ ] `sfx_surface_flesh_sword` **P1** ×4 — A sword hit on a creature (Surface_Flesh.byTool) — A quick slice: a sharp swish-cut with a light wet tail.
- [ ] `sfx_surface_flesh_torch` **P1** ×3 — A torch hit on a creature (Surface_Flesh.byTool) — A wooden club thud plus a singe hiss of burnt fur.
- [ ] `sfx_surface_player` **P1** ×4 — Cloth plus a body thump (with the hurt voice from §3.2).
- [ ] `sfx_surface_wood_log` **P1** ×4 — Higher, hollower chop into lying wood.
- [ ] `sfx_surface_straw` **P3** ×4 — Sack-and-straw thump.

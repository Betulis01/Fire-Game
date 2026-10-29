# Inventory & Hauling — Design Plan

## Vision

Hauling is the core loop. The campfire is your lifegiver: you leave it to chop, mine and forage, and you carry what you gathered back before the cold and the dark catch you.

**The tension of hauling should come from the world, not from the controls.** The world means:

- the cold draining your warmth
- enemies catching you while your arms are full
- the distance back to the fire
- the weight you carry

Picking things up, fighting and depositing must feel fast and predictable. The risk is in *what you choose to carry and when*, never in fighting the input.

---

## Why it feels clunky today

1. **E grabs the item nearest your feet, not what you aim at.**
   - [PlayerInteractor.cs:212](Assets/Scripts/Entities/Player/PlayerInteractor.cs#L212) ranks items by distance from the player and ignores the cursor.
   - Every chop scatters logs, sticks and sap around the tree, so the nearest item is often the wrong one.
   - Your own axe lying in that pile is just another item.
2. **Picking up quietly drops your weapon.**
   - When both hands are full, E swaps the selected hand's item onto the ground ([PlayerInteractor.cs:198](Assets/Scripts/Entities/Player/PlayerInteractor.cs#L198)).
   - Grab a log while holding an axe and a torch, and one of them lands in the debris pile.
3. **Too much to keep track of.**
   - Q and E act on the "selected hand", which you change with Tab or the scroll wheel.
   - Dropping takes three steps: hold Q, aim, release.
4. **No quick way out of carrying.**
   - When a skeleton shows up while your hands are full of logs, you have to drop, find your weapon, aim at it, pick it up, and only then fight.
   - This is where the flow breaks.
5. **Hands do three jobs at once.** They are the weapon loadout, the carrying space and part of the controls (left click = left hand, right click = right hand). Clicking left, right, left to alternate hands doesn't feel good.

---

## The model

Split the hands' three jobs into three separate places:

| | **Loadouts** (Player tab) | **Bag** | **Hands** |
|---|---|---|---|
| Holds | weapons, tools, shield, torch | small items (sticks, berries, sap) **and** gear (tools, weapons, armour); 1 slot each | big items: **logs and stone** |
| Capacity | 2 sets × (Main Hand + Off Hand) | 2 slots to start; crafted bags add more | **one big item per hand** (2 per trip) |
| Changes | on purpose: Hold E, the Player tab, scroll swap | press E; walking over an item tops up its stack in the bag if there's room | constantly, in the world |

The player is always in one of two states:

- **Armed:** the hands are empty, so the active loadout is drawn and shows on the hand anchors.
- **Carrying:** a hand holds a big item, so the loadout is stowed on the back or belt. You're slower and can't attack.

**Core rule: nothing you own is ever dropped as a side effect.** Items move between loadout, bag and hands. They reach the ground only when you drop them, or as the very last fallback.

---

## HUD

The HUD shows the model directly: what you can do (action bar), what you've got equipped (Player tab), what's in your pockets (Bag tab) and what's in your arms (carry indicator). Each piece is built in the phase that needs it; the phase tag says which.

### Action bar *(Phase 3; Quickslot filled in Phase 6)*

One bar holds every action you can take. From left to right:

| Skill 1 | Skill 2 | Main Hand | Off Hand | Quickslot |
|---|---|---|---|---|
| `1` | `2` | Left click | Right click | consumable key |
| main-hand weapon skill | off-hand weapon skill | attack | off-hand action (block, torch, …) | food from the bag |

- **Each slot shows the icon of the item or skill behind it.** The Main Hand and Off Hand slots show the active loadout set, and the Quickslot shows the assigned food with its bag count.
- **Using an action shows its cooldown on that slot.** Examples: a skill's cooldown, an attack's recovery, the eating delay.
- **It replaces today's hand bar** (`HandBarUI` / `HandSlotUI` with `ActionSlotUI` between them), because the hands no longer drive the controls.

### Loadout sets *(Phase 4)*

- **Both sets show next to the action bar,** small, with the active one highlighted.
- **A swap that takes time** shows as a cooldown on the Main Hand and Off Hand slots (see open question 5).

### Player tab *(Phase 2; set B added in Phase 4)*

- **Main Hand and Off Hand slots** for set A (and set B from Phase 4), next to the armour slots and the stats column.
- **The same item tooltip** as today works on hover for loadout and armour slots.

### Bag tab *(Phase 1)*

- **Slot count follows the worn bag:** 2 to start, 6 with the crafted Small Bag.
- **Gear shows in the bag like any other item,** one slot each, with the same tooltip.
- **Drag and click keep working** as today ([BagSlotUI](Assets/Scripts/Render/UI/HUD/BagSlotUI.cs)): drag to the world, a station or a slot, and drag onto a loadout slot to equip.
- **Assigning the Quickslot** happens here (Phase 6), for example by dragging a food stack onto the Quickslot.

### Carry indicator *(Phase 2)*

- **Shows the big items in your hands** while you carry them, so you can read your load at a glance (see open question 6 for where).

### World prompts *(Phase 5)*

- **Aimed item** lit with the existing interact highlight.
- **Prompt:** `E Take` and `Hold E Equip`, with a fill ring (the existing `RadialGauge`) while E is held.
- **Receiver zones** (fire, furnace, build site) keep their deposit prompts; the build site keeps listing what it still needs.

---

## Roadmap (in build order)

### Phase 1: Item sizes and the starting bag

The foundation everything else routes through.

- **Item size.** Each item is either **Small** (goes into the bag) or **Big** (goes into the hands).
  - Gear counts as Small: tools, weapons and armour take one bag slot each.
  - **Logs and stone are Big.** One per hand, no stacking in the hands.
  - Store the size on the item (`ItemDefinition`) so routing never needs special cases.
- **Starting bag.** The player starts wearing a **2-slot bag**. The crafted Small Bag (6 slots) becomes the first upgrade.
- **Simple routing for E** (until Phase 5 refines it):
  - A Big item goes into a free hand.
  - A Small item goes into the bag, merging onto a matching stack first.
- **Bag auto-pickup only tops up what you already carry.** Walking over a Small item puts it in the bag **only if the bag has a stack of that item with room to fill**. It never takes a slot of its own: once every matching stack is full, auto-pickup stops. Anything new, or a fresh stack, needs an E press, so the bag never fills with things you didn't choose. Today [Inventory.cs](Assets/Scripts/Entities/Player/Inventory.cs) picks up everything you walk over, so this narrows it.
- **Crafting** keeps drawing ingredients from the bag and the hands together. With stone as a Big item, some recipes no longer fit in two hands (see *Crafting with Big items* below).
- **HUD:** the Bag tab's slot count follows the worn bag; gear shows in it.

### Phase 2: Loadout slots and the Armed / Carrying states

This phase removes the biggest source of friction.

- **Main Hand and Off Hand slots** go in the HUD's Player tab, next to the armour slots.
  - The existing `Equipment` already handles slot swaps and gear stats, so it can host these. New `EquipSlot` values are appended at the end, as its comment says.
- **Picking up a big item stows the loadout. Dropping or depositing the last one draws it again.**
- **Left click while carrying drops your load at your feet and draws your weapons in one move.** Fighting is always one button away.
- **The "selected hand" goes away** (Tab and the scroll wheel). Routing never depends on it, so there's one less thing to track.
- **Carrying penalties** create the tension: lower move speed and no attacks. These are tuning knobs and can grow later (heavier loads, stamina; stone could weigh more than wood).
- **HUD:** loadout slots in the Player tab; the carry indicator.

### Crafting with Big items

Stone becoming Big breaks recipes that need more Big items than two hands hold:

| Recipe | Big items needed | Fits in two hands? |
|---|---|---|
| Stone Axe / Stone Pickaxe | 1 wood + 2 stone | **no** (3) |
| Furnace (placed) | 10 stone | yes, delivered over 5 trips |
| Campfire (placed) | 1 stone (+ 2 sticks) | yes |
| Workbench (placed) | 2 wood | yes |
| Iron Sword | 1 wood (+ 4 iron bars) | yes |

Placed buildings already work by delivery (build sites take deposits), so they're fine; the Furnace just becomes a real hauling job. **Instant recipes at a station need a way to use Big items you're not holding.** Options:

- **(a) Deposit at the station.** The workbench takes Big items like a build site does, and its recipes draw from what's been deposited. Fits the hauling loop best.
- **(b) Draw from the ground nearby.** Big items lying within the station's reach count as ingredients.
- **(c) Rebalance the recipes** so no instant recipe needs more than two Big items.

Recommendation: **(a)**. It turns the workbench into a small stockpile and uses the same deposit verb (Q) as the fire.

### Phase 3: Combat controls

| Input | Action |
|---|---|
| Left click | Main-hand attack: tap for a light attack, hold for a heavy one |
| Right click | The off-hand item's action (below) |
| 1 / 2 | Main-hand weapon skill / off-hand weapon skill |

- **Off-hand actions:**
  - shield: block (hold)
  - torch: thrust to ignite, or raise it for more light and warmth
  - second weapon: off-hand strike or parry
  - empty: a shove or kick
- **Dual wielding without awkward clicking.** With a weapon in each hand, repeated left clicks alternate left, right, left as a combo. The alternation is only visual.
- **Two-handed weapons** (the bow; a great axe later) fill both slots. The bow draws and releases on left click.
- **The off hand is the survival choice.** A shield keeps you alive in a fight. A torch gives warmth, light and fire, but you can't block. This puts the cold-versus-combat tension directly into your gear.
- **Animation barely changes.** The `_l`/`_r` clips stay as visual detail of which hand swings.
- **Code impact:** everything that takes a `HandSide` changes meaning from "which button was pressed" to main hand or off hand:
  - `WeaponUse`
  - `ItemUser`
  - `ShieldBlock`
  - `ActiveSkillSlots`
  - `TargetPreview`
  - the Q/E routing in `PlayerInteractor`
- **HUD:** the action bar, with cooldowns; the Quickslot stays empty until Phase 6.

### Phase 4: Two loadouts and the scroll swap

- **Two sets, A and B, each with a Main Hand and an Off Hand.** Typical use is a gathering set (axe and pickaxe) and a fighting set (sword and shield). The sets are player-defined rather than hard-coded, because the axe is both a tool and a weapon (Whirlwind, the axe skill tree).
  - Example: "axe + torch" and "sword + shield".
  - Example: "axe + pickaxe" and "axe + shield".
- **Scroll wheel (gamepad: North button) swaps between the sets.**
  - The swap takes a short beat and can't happen mid-swing, so swapping is a deliberate moment in combat.
  - While carrying, scrolling picks which set gets drawn when the load comes off.
- **HUD:** both sets next to the action bar, the active one highlighted; set B in the Player tab.

### Phase 5: Picking up feels right

- **Pick up what you aim at.**
  - Use the same idea as combat targeting: the item nearest the cursor (or stick direction) within reach wins.
  - The choice is sticky, so it doesn't flicker between items, and the existing interact highlight shows it.
  - **Priorities:** your own tools and weapons come first when you're missing gear; items you just dropped come last.
- **Press E** takes the item, using the Phase 1 routing: big items to the hands, everything else to the bag.
  - Because pressing and holding share a key, a press triggers on release.
- **Hold E** (~0.35 s) **equips or wears** the item.
  - A weapon or tool goes into a slot it fits in the active set that is empty. If none is free, it replaces the main-hand item.
  - A helmet or a bag works the same way: one verb for all gear.
  - **What gets replaced goes to the bag, else to a free hand, else onto the ground.** The ground is the last resort.
- **HUD:** the world prompts (`E Take` / `Hold E Equip` with a fill ring).

### Phase 6: Consumable quickslot

- **One quickslot with its own key.** R is free; F is the menu.
- **You assign a food type** to it. It shows how many you carry in the bag, and the key eats one.
- **Eating works while carrying.** Taking a berry from your pocket shouldn't break a hauling trip.
- **HUD:** the action bar's Quickslot; assigning it from the Bag tab.

### Phase 7: Dropping, depositing and throwing (Q)

- **Tap Q:**
  - Inside a receiver zone (fire, furnace, build site, workbench), it deposits a big item there.
  - Elsewhere, it drops a big item at your feet.
- **Hold Q, then release: throw a big item** a short distance toward your aim.
  - A log thrown into the fire becomes fuel; one thrown onto a woodpile is stored.
  - Throwing is fast, fun, and fits "the fire is the lifegiver".
  - It replaces today's hold-Q-to-aim drop ghost.
- **Bag items** keep their HUD route: drag from the Bag tab to the world, a station or a slot.

### Phase 8: Hauling as progression

Hauling then grows over a run instead of staying a flat cost.

- **Woodpile.** A buildable pile next to the fire that the fire feeds itself from.
  - The day's goal becomes "fill the pile before night".
  - The pile can be a throw target.
- **Stone pile** (or a shared stockpile) next to the workbench and furnace, so stone you haul in has a home before it's used.
- **Sled or pack frame.** Carry 3–4 big items at the cost of speed.
- **Bigger bags.** The crafted 6-slot bag first, then larger tiers for small items and gear.
- **Forward fires and torches** warm a zone away from camp, so you can haul further from the main fire.

---

## Terrain (parallel track)

Not a phase: it doesn't depend on the inventory work and can be built at any point. It belongs in this plan because terrain decides how expensive a hauling route is.

### The rules

| Ground | Movement |
|---|---|
| **Snow** | slower |
| **Grass / stone** | normal speed |
| **Ice** *(later)* | slippery: you keep sliding after you stop pushing, and turning is slow |

- **Applies to every entity:** the player and enemies alike. Enemies wading through snow can be outrun on a stone path.
- **Stacks with the other speed effects:** cold, food buffs and the carrying penalty all multiply together. A full-handed trip through deep snow in the cold is the slowest you can be, which is exactly the hauling tension.
- **Knockback slides further on ice.** Fights on a frozen lake become dangerous for both sides.

### How entities detect it

- **Each ground tile has a terrain type** (Snow, Grass, Stone, Ice). It's stored on the tile asset itself, or in one lookup table from tile to terrain, so painting the map is all it takes.
- **A shared ground-sensing component** on the player and enemies reads the tile under the entity's feet. It only re-checks when the entity moves into a new cell, and gives back a speed multiplier and a traction value.
- **Movement uses them:**
  - `PlayerController` and `EnemyMover` multiply their speed by the terrain's multiplier.
  - Ice lowers traction: velocity eases toward the input slowly instead of snapping. `PlayerController` already eases velocity when you let go, so ice is that same easing turned way down.
- **The snow trail follows it.** `SnowTrail` already has a `CanStamp` hook meant for "only on snow tiles", so trenches appear only where there's snow.

### Ideas that grow out of it

- **Packed trails.** Walking the same snow again and again packs it into a faster path, so your regular hauling routes turn into trails on their own and the map shows where you've been.
- **Laid paths.** Placing stone or plank paths gives normal speed through snow. This is a hauling-progression option next to the woodpile and the sled in Phase 8.
- **The sled likes snow.** It could be slow on grass and stone but fast on snow and ice, the opposite of walking, so choosing a route depends on what you're carrying.
- **Thawing changes the ground.** When a region thaws (see [Story.md](Story.md)), snow turns to grass and the whole area becomes faster to cross.

---

## Open questions

1. **Does a stowed torch keep burning and warming you,** or does carrying force a choice between light and wood?
2. **Is the bag full?** Can a Small item spill into a free hand as the fallback (which counts as carrying), or does it stay on the ground?
3. **Swapping or removing bags.** Do the contents transfer into the new bag, with the overflow going to the hands and then the ground? Can the starting bag be taken off at all?
4. **Bag stack sizes.** The hand-era sizes (sticks 4, iron bars 8) may need revisiting once stacks live in the bag.
5. **Swap timing.** How long does a loadout swap take, and does stowing or drawing after carrying cost any time? If it does, the swap shows as a cooldown on the Main Hand and Off Hand slots.
6. **Where the carry indicator lives.** Options: the Main Hand and Off Hand slots switch to the carried items while carrying, or a small separate indicator by the action bar.
7. **More Big items.** Is Iron Stone (the iron ore chunk) Big like stone? Polar bear meat or a whole carcass would also make some resources worth a dedicated trip. Undecided for now.
8. **Crafting with Big items.** Pick (a), (b) or (c) from *Crafting with Big items*.
9. **How slow is snow?** For example 70% of normal speed. Does the carrying penalty grow on snow, so hauling through it hurts more than walking through it?

---

## Related issues noticed (outside this plan)

These came up while analysing the game and affect the core loop:

- **Fuel and enemy spawns pull the wrong way.**
  - `EnemySpawner`'s comment says a dying fire makes the dark more dangerous.
  - The code does the opposite: a full fire spawns fastest and an empty fire spawns nothing.
  - Its night-only check is commented out, so skeletons spawn all day.
  - A deliberate version could be a real trade-off: a big fire is warm but draws more enemies, a low fire is quiet but cold.
- **The win condition is probably a test value.** Camp starts at 10:00, so a "survive to noon" win fires about 25 seconds in.
- **The loop ends at noon.** Nights don't escalate and nothing carries over. The long-term goal is now explored in [Story.md](Story.md). Possible directions:
  - escalating nights
  - areas or resources that unlock by day (iron beyond the treeline)
  - meta progression if the game becomes a roguelite
- **Loot and gear gaps.**
  - Skeletons drop nothing.
  - Leather has no use (see *Leatherworking* under Ideas).
  - The Iron Helmet can't be crafted (see *Blacksmithing* under Ideas).
  - The axe tree's two passive nodes have no stats yet.
- **Gathering out-levels fighting.** Weapon XP comes from every hit that deals damage, so chopping trees levels the axe faster than combat does.

---

## Ideas

- **Orc enemy.** Bring in an old orc sprite as a new enemy. Now part of the enemy factions in [Story.md](Story.md).

### Blacksmithing

Its own crafting line, built around iron from the furnace.

- **Anvil.** A new placed station (delivered like a build site; heavy, so a real haul). Iron gear moves from the workbench to the anvil:
  - Iron Sword, iron axe and iron pickaxe.
  - **The Iron Helmet becomes craftable here** (fixes "can't be crafted"), followed by iron chest and legs.
- **Forging uses the combat system.** Instead of a menu click, you equip a hammer and strike the anvil. Every hit advances the piece, with the impact hold and sparks the melee system already has, so crafting feels physical and uses verbs you already know.
- **Sharpening (grindstone).** A station that gives a weapon or tool a temporary edge, such as bonus damage for a number of hits or until dawn. A small whetstone item could do a weaker version in the field.
- **Better tools gather better.** An iron axe could fell faster or loosen more logs per hit, so blacksmithing feeds straight back into hauling.
- **Upgrades over replacements.** Reinforce an existing weapon with iron bars (+damage, +armour) instead of only crafting new ones, so a favourite weapon can grow with you.
- **Placement matters.** The anvil sits next to the furnace, and both should be near the fire, because standing still to forge in the cold costs warmth.

### Leatherworking

Its own line, built around animals (polar bear first). Gives leather a use.

- **Skinning rack.** A placed station. A kill drops a **carcass as a Big item** (see open question 7) that you haul back and hang on the rack, which yields hides and meat.
- **Tanning takes time,** like smelting: a hide on the rack (or a tanning tub) becomes leather over a while, maybe faster near the fire. Sap could be the tanning agent, which gives sap a second use.
- **Skinning knife.** A small tool. Skinning in the field gives a hide directly, so you don't haul the carcass but get less from it. That's a choice between a heavy trip and a lighter, poorer one.
- **What leather makes:**
  - **Bigger bags**, the bag tiers in Phase 8.
  - **Sled or pack frame straps**, Phase 8's hauling upgrades.
  - A quiver for the bow, and shield straps.
  - **Leather and fur armour.**
- **Warmth as a gear stat.** Fur and leather armour adds insulation, which slows how fast the cold drains your warmth. Iron armour protects more but is cold, with little or no insulation. Choosing between warmth and armour ties gear back to the core loop.

### Shared ideas for both crafts

- **Profession progression.** Blacksmithing and leatherworking each level up through use, the same way weapon XP does, and unlock recipes or quality as they go.
- **Quality tiers.** A higher profession level, or better materials, gives better versions of the same item.
- **Stations are a hauling target.** Iron bars, stone, carcasses and hides all have to reach their station, so each new craft adds a reason to make trips.

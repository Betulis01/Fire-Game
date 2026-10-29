# Inventory & Hauling — Design Plan

## Vision

Hauling is the core loop. The campfire is your lifegiver: you leave it to chop, mine and forage, and you carry what you gathered back before the cold and the dark catch you.

**The tension of hauling should come from the world, not from the controls.** The world means:

- the cold draining your warmth
- enemies catching you while your arms are full
- the distance back to the fire
- the weight you carry
- the ground you cross (snow, grass, ice)

Picking things up, fighting and depositing must feel fast and predictable. The risk is in *what you choose to carry and when*, never in fighting the input.

---

## The model

The hands' three old jobs are split into three places (built; see **DONE**):

| | **Loadouts** (Player tab, action bar) | **Bag** (Player tab) | **Hands** |
|---|---|---|---|
| Holds | weapons, tools, shield, torch | small items (sticks, berries, sap, rocks) **and** gear (tools, weapons, armour); 1 slot each | big items: **logs and stone** |
| Capacity | 2 sets × (Main Hand + Off Hand); a two-handed tool (bow, pickaxe) fills a set | 2 slots (the starting Pouch); the crafted Small Bag gives 6 | **one big item per hand** (2 per trip) |
| Changes | on purpose: Hold E, the Player tab, drag, scroll / Tab swap | E; walking over an item tops up a stack the bag already has | constantly, in the world |

The player is always in one of two states:

- **Armed:** the hands are empty, so the active loadout is drawn on the hand anchors.
- **Carrying:** a hand holds a big item, so the whole loadout is stowed (a torch goes dark). You're slower and can't attack; left click puts the load down and draws the weapons.

**Core rule: nothing you own is ever dropped as a side effect.** Items move between loadout, bag and hands. They reach the ground only when you drop them, or as the very last fallback.

---

## Still to do

### Phase 3 leftovers: off-hand actions that need new animations

- **Torch:** raise it (hold right click) for more light and warmth. Today right click swings it, which ignites.
- **Second weapon:** an off-hand parry.
- **Empty off hand:** a shove or kick. Today it's the fist punch.

### Breaking stone into rocks

- **Hitting a Stone (the Big item) breaks it into Rocks.** Give the Stone prefab Health, a Hurtbox that a pickaxe can damage (`ToolDamageFilter`), and `DropOnDeath` with a few Rocks, like logs already work with an axe. That gives Rock a second source besides Stone Ore.

### Phase 8: Hauling as progression

Hauling then grows over a run instead of staying a flat cost.

- **Woodpile.** A buildable pile next to the fire that the fire feeds itself from.
  - The day's goal becomes "fill the pile before night".
  - The pile can be a throw target (Q throwing already lands thrown items in any receiver zone).
- **Stone pile** (or a shared stockpile) next to the workbench and furnace, so stone you haul in has a home before it's used.
- **Workbench stockpile.** The workbench takes Big items like a build site does, and its recipes draw from what's been deposited. Not needed today (the tool recipes moved to Rock), but it fits the loop once recipes need more Big items than two hands hold.
- **Sled or pack frame.** Carry 3–4 big items at the cost of speed.
- **Bigger bags.** Larger tiers after the Small Bag, for small items and gear.
- **Forward fires and torches** warm a zone away from camp, so you can haul further from the main fire.

### Carry indicator

The HUD doesn't show the carried logs or stone themselves. You see them in the hands in the world, and the action bar's Main / Off Hand slots dim to show the stowed loadout. Revisit if loads get more varied (sled, carcasses).

---

## Terrain (parallel track)

Not a phase: it doesn't depend on the inventory work. It belongs in this plan because terrain decides how expensive a hauling route is.

### The rules

| Ground | Movement |
|---|---|
| **Snow** | slower |
| **Grass / stone** | normal speed |
| **Ice** | slippery: you keep sliding after you stop pushing, and turning is slow |

- **Applies to every entity:** the player and enemies alike. Enemies wading through snow can be outrun on a grass patch or a stone path.
- **Stacks with the other speed effects:** cold, food buffs and the carrying penalty all multiply together. A full-handed trip through deep snow in the cold is the slowest you can be, which is exactly the hauling tension.
- **Knockback slides further on ice.** Fights on a frozen lake become dangerous for both sides.

### How the ground is built

- **Base ground tilemap** (32 px isometric tiles): plain snow everywhere, painted fast with the fill tool. Its terrain is the default (Snow).
- **Ground Detail tilemap** (built; see DONE): big tiles that overlap freely, such as grass patches, ice, dirt and thawed spots. These give the ground its natural, non-square look.
- Patch art holds **only the terrain itself** (the grass with its soft edge, no snow top or dirt sides), since the patch's pixels are what detection reads.

### How entities detect it: pixel sampling

Cell-by-cell detection is too coarse for organic patches, so detection follows the art's pixels.

- **`TerrainTile : Tile`**, used for every Ground Detail tile, adds:
  - `terrain`: Grass, Ice, Stone, Dirt, …
  - **a baked pixel mask:** which of the sprite's pixels count as this terrain (alpha above a threshold, ignoring the soft outline). An editor step bakes it from the sprite, so no texture has to be readable at runtime. It's re-baked when the art changes, linked by name like the other tools.
- **`TerrainMap`** (one in the scene) answers `At(worldPoint)`:
  1. Find the Ground Detail tiles whose sprite covers the point: the cells within a patch's reach (±2 cells for the 4×4 patches).
  2. For each, turn the point into sprite pixels (undo the cell position and the tile's offset) and look it up in the mask.
  3. The top-most covering tile wins (the tilemap's draw order); none means the base ground's terrain.
- **`GroundSensor`** on the player and enemies:
  - samples the point under the feet whenever it has moved by about a pixel (1/32 unit), never more than once a frame;
  - returns a speed multiplier and a traction value;
  - eases the multiplier over ~0.15 s so stepping across a ragged edge doesn't stutter.
- **Movement uses them:**
  - `PlayerController` and `EnemyMover` multiply their speed by the terrain's multiplier.
  - Ice lowers traction: velocity eases toward the input slowly instead of snapping. `PlayerController` already eases velocity when you let go, so ice is that same easing turned way down.
- **The snow trail follows it.** `SnowTrail.CanStamp` asks `TerrainMap.At`, so trenches only appear where there's snow, down to the pixel.
- **Cost:** a query is a handful of cell lookups and mask reads, run only when an entity moves. It's negligible next to rendering, even with dozens of enemies.

### Ideas that grow out of it

- **Packed trails.** Walking the same snow again and again packs it into a faster path, so your regular hauling routes turn into trails on their own and the map shows where you've been.
- **Laid paths.** Placing stone or plank paths gives normal speed through snow. This is a hauling-progression option next to the woodpile and the sled in Phase 8.
- **The sled likes snow.** It could be slow on grass and stone but fast on snow and ice, the opposite of walking, so choosing a route depends on what you're carrying.
- **Thawing changes the ground.** When a region thaws (see [Story.md](Story.md)), snow turns to grass and the whole area becomes faster to cross.

---

## Open questions

1. **Bag stack sizes.** The hand-era sizes (sticks 4, iron bars 8) may need revisiting now that stacks live in the bag.
2. **More Big items.** Polar bear meat or a whole carcass would make some resources worth a dedicated trip (see *Leatherworking*). Undecided for now.
3. **How slow is snow?** For example 70% of normal speed. Does the carrying penalty grow on snow, so hauling through it hurts more than walking through it?
4. **Armour and Hold E.** All gear now needs Hold E (or the Player tab) to go on. Keep it, or let armour go straight into an empty slot on E?
5. **Crafting with a full bag.** A recipe whose output has nowhere to go is refused, but its blueprint still looks available. Grey it out instead?

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

- **Skinning rack.** A placed station. A kill drops a **carcass as a Big item** (see open question 2) that you haul back and hang on the rack, which yields hides and meat.
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

---

## DONE

### What it fixed

The old setup was clunky in five ways, all fixed by Phases 1–7:

1. E grabbed the item nearest your feet instead of what you aimed at.
2. Picking up with both hands full dropped your weapon.
3. Q and E acted on a "selected hand" changed with Tab or the scroll wheel.
4. There was no quick way out of carrying when a fight started.
5. The hands were the loadout, the carrying space and the controls at once.

### Phases 1 + 2: Item sizes, the bag, loadouts, Armed / Carrying

- **Item size** on `ItemDefinition` (`size`: Small / Big). Wood and Stone are Big, and Stone no longer stacks. Everything else is Small.
- **Rock:** a new Small item (stacks to 8) that Stone Ore drops alongside Stone. The Stone Axe and Stone Pickaxe take 1 Wood + 2 Rock, so no instant recipe needs more Big items than two hands hold. Campfire and Furnace still take Stone, delivered to their build sites.
- **Hands** (`Hands`) carry Big items only, one per hand. They draw the active loadout while empty and stow it while carrying.
- **Loadout** (`Loadout`): sets A and B × Main Hand (left click) and Off Hand (right click), for tools, weapons, shields and torches.
- **Starting bag:** the player starts wearing the 2-slot **Pouch** (`Equipment.startingGear`); the Small Bag (6 slots) is crafted from 4 Sticks at the Workbench.
- **Bag** (`Inventory`):
  - auto-pickup only tops up a stack the bag already has;
  - bags are only ever swapped, never taken off; the contents carry over, and a smaller bag spills what no longer fits.
- **E routing** (`Inventory.TryTake`): Big → a free hand. Everything else, gear included, → the bag. With no room, nothing happens.
- **Carrying:**
  - 75% move speed (`PlayerController.carrySpeedMultiplier`) and no attacks;
  - left click puts the load down and draws the weapons;
  - the stowed loadout is inactive, so a torch goes dark and gives no warmth.
- **The selected hand is gone.** Scroll and Tab now swap loadout sets.
- **Crafting** draws from the hands and the bag together. The output is routed like an E press, and a recipe is refused if the output has nowhere to go. Items match by asset or by id, so Scots Pine and Grey Alder logs both count as Wood.
- **Player tab:** armour, stats, loadout sets A and B, and the bag at the bottom. The separate Bag tab and the hand-bar hand slots are removed.
  - **Bag slot click:** wield, wear, or eat one.
  - **Drag:** between bag slots, onto loadout and armour boxes, onto a station menu, or into the world.
  - **Loadout items:** drag between loadout boxes and either set, back into the bag, or into the world.
  - **Left or right click on a filled box:** puts it back into the bag if there's room.

### Phases 3 + 4: Combat controls, two loadouts, the action bar

- **Controls:** left click is the Main Hand (tap light, hold heavy) and right click is the Off Hand: a shield blocks, a torch or second weapon swings, an empty hand punches.
- **Dual wielding:** with a melee weapon in each hand (not fists, torch or shield), left clicks within 0.6 s of the last swing ending alternate L, R, L. Each swing uses that hand's own weapon. The charge follows the button that started it.
- **Two-handed** (`Tool.twoHanded`), for the Bow and the Stone Pickaxe:
  - it fills a set's Main Hand and keeps the Off Hand empty;
  - equipping one moves the Off Hand item to the bag, and is refused when there's no room;
  - right click does nothing while one is drawn.
- **Set swap:** scroll up = A, down = B, Tab toggles. It's blocked mid-swing and takes a 0.3 s beat (`Loadout.swapTime`) with no attacks, blocks or skills. While carrying, scrolling picks the set that gets drawn.
- **Action bar** (`ActionBarUI`), left to right:
  - Skill 1 and Skill 2 (the drawn weapons' spells, with cooldowns);
  - Main Hand and Off Hand (the active set; dimmed while carrying; the Off Hand ghosts a two-handed Main Hand; drains during the swap beat; fully dimmed while that hand's swing recovers);
  - the R Quickslot;
  - the two sets small on the right, the active one bright, with the scroll hint beside them;
  - the "Q Drop" hint under the bar while carrying.

  The hand slots drag like loadout boxes.

### Phase 5: Picking up feels right

- **Aimed focus:** the usable interactable nearest the cursor (or stick) within reach wins, sticky by 0.15. Gear for an empty slot counts as 0.5 closer, and just-dropped items as 0.5 further.
- **E on gear acts on release:** a tap takes it into the bag; Hold E (0.35 s) wields it into a free slot of the active set (else in place of the Main Hand) or wears it, a bag included. What's replaced goes to the bag, else onto the ground at your feet. Anything else (stations, resources) acts on the press.
- **The prompt:** the item name in the Tiny5 pixel font (one font pixel = one game pixel), a grey "Hold E: Equip" line under it for gear, and a fill bar under the E icon while E is held.

### Phase 6: Consumable quickslot

- **R** (gamepad West), or a click on the slot, eats one of the assigned food from the bag, with a 0.5 s cooldown. It works while carrying.
- **Assigning:** drag a food stack from the bag onto the slot; the stack stays in the bag. The slot shows the count across the bag and dims at 0.

### Phase 7: Dropping, depositing and throwing (Q)

- **Tap Q:** deposits the carried item into a receiver zone you stand in (fire, furnace, build site), else drops it at your feet.
- **Hold Q:** the ghost shows where it will land (up to 3 units away). Release or left click throws it in an arc (`ThrownItem`), Y-sorted by the ground under it. It lands in a receiver if it falls in one, so a log thrown into the fire becomes fuel.
- **Q acts on the hand filled last.** Bag items keep their drag route.

### Resolved questions

- **Stowed torch:** goes dark and gives no warmth while carrying.
- **Full bag:** a Small item stays on the ground.
- **Bags:** swapped only, never taken off; the contents carry over.
- **Swap timing:** 0.3 s, and drawing after carrying is free.
- **Carry indicator:** the action bar's hand slots dim to show the stowed loadout.
- **Iron Stone:** Small.
- **Crafting with Big items:** the tool recipes moved to Rock (Small).

### Terrain groundwork

- **Ground Detail tilemap** (`Tools/Tiles/Set Up Ground Detail`, `GroundDetailSetup`):
  - a second tilemap on the scene's Grid in Chunk mode, sorted just above the ground (ground −1, detail 0, snow trail 1, splatters 2, shadows 3);
  - the **grass patch** tile is `Frame_0` of `big tile - grass.aseprite` (32 PPU, a 4×4-cell patch centred on the painted cell, no collider), in `ground-detail-palette`.
- **Grass tileset** (`Tools/Tiles/Build Grass Tileset`, `GrassTilesetBuilder`): `big tile.aseprite` cut into 32 px ground pieces (front corner, SE / SW edges with side faces, fill), with an Isometric Rule Tile that picks the piece from its neighbours, and `grass-palette`. It's still the brown dirt art, to be recoloured.
- **Isometric tileset:** the 11 sprites in the sheet's bottom row got Tile assets and joined `isometric-palette`.

# Fire Game — Overview

A snapshot of how the game plays **today**. For where it's heading, see:

- [InventoyManagementPlan.md](InventoyManagementPlan.md): inventory, hauling, HUD and terrain plan
- [Story.md](Story.md): premise, goal, dungeons and enemies

---

## The game in a paragraph

**Fire Game** is a top-down pixel-art survival game set in a frozen forest. The world is always below freezing (−2 °C at noon, −30 °C at night), so your warmth drains whenever you're away from a heat source. At zero warmth you start losing health and move slower. The campfire is your lifeline, and it burns through its fuel. You chop, mine and forage to keep it fed. You also climb a small crafting ladder: workbench, stone tools, furnace, then iron. Meanwhile skeletons rise out of the ground in a ring around the fire. The current win condition is to survive until noon.

---

## How it plays

### Combat

- **Two hands.** Each hand is its own weapon. Left mouse button uses the left hand and right mouse button uses the right. Whatever a hand holds decides what the button does: swing, eat or block with a shield. An empty hand falls back to fists.
- **Soft targeting.** A swing hits whatever is inside an aimed cone, up to a cap (usually 2 targets). The main target is highlighted gold and any secondary targets cyan before you swing. Secondary targets take half damage.
- **Heavy attacks.** Hold the button to charge a heavy attack. Swings also have a lunge, knockback, an impact freeze on axes and pickaxes, and a short hit-stop.
- **Bow.** Charging the bow increases how far the arrow flies.
- **Torch.** Hitting something with a torch sets it on fire, which does damage over time.
- **Whirlwind.** This axe skill is cast with key 1 or 2.
- **Skeletons.** They wander until something hits them, then chase that attacker forever. They spawn around the campfire, the spawn rate scales with how much fuel the fire has, and they vanish at 09:00.
- **Stats.** Strength, agility and stamina feed into attack power, crit chance and health, and armor reduces damage taken. So far the only stat item is the Iron Helmet, which is placed in the world rather than crafted.

### Gathering

- **Tools are required.** Trees take an axe and ore takes a pickaxe. Any other tool or your fists will "glance": the hit lands but does no damage and drops nothing.
- **Every damaging hit can knock loot loose.** Trees drop logs (50% chance per hit) and sap (15%). Ore drops stone. A felled tree or broken ore drops its main reward.
- **Logs break down further.** A log is a heavy, non-stacking item: one per hand. Chopping it gives 3 sticks.
- **Food is everywhere.** There are 44 berry bushes, and each regrows its berry 60 seconds after picking. Polar bears drop meat and leather, but there are none in the scene yet.
- **Gathering trains your weapon.** Chopping and mining earn weapon XP, so cutting wood levels your axe for fighting.

### Inventory and crafting

- **Your hands are your inventory.** Stacks are small: sticks 4, stones 8, logs 1. Whatever you carry is also what you fight with.
- **The Small Bag** costs 4 sticks at a workbench. It adds 6 slots and picks up items you walk over. You move things between bag and hands by clicking or dragging. Taking the bag off spills everything at your feet.
- **Crafting** runs from a HUD tab. Ingredients are taken from both hands and the bag.
  - **Buildings** (campfire, workbench, furnace) are placed as a ghost first. You then bring the materials to the site and deposit them with Q, the same way you feed the fire.
- **The crafting ladder:**
  - Sticks → torch and campfire
  - Pine wood → workbench
  - Workbench → stone axe and pickaxe, bow, bag
  - 10 stone → furnace
  - Iron ore → furnace → iron bar
  - 4 iron bars → iron sword
- **Stations.** The campfire cooks food and the furnace smelts ore. You feed both by depositing items into them.

### Progression

- **Player level.** Each kill gives 2 XP and a level costs 10 XP. Each level adds +1 to every attribute. Only enemies give XP.
- **Weapon level.** Every hit that deals damage gives 1 XP to that weapon type. Weapon levels give skill points for that weapon's tree. The axe tree has two passive nodes with no stats yet, plus Whirlwind.
- **Gear.** There are six slots: head, chest, legs, ring, amulet and bag. Only the helmet and the bag exist so far.

### Controls (keyboard and mouse)

| Input | Action |
|---|---|
| WASD | Move |
| Left / right click | Use the left / right hand |
| E | Interact / pick up |
| Q | Hold to aim a drop, release to drop; deposit into a fire or build site |
| Tab / scroll | Select hand |
| 1 / 2 | Left / right hand's skill |
| F | Menu (Bag, Player, Crafting, Skills tabs) |
| Page Up / Page Down | Zoom |
| Esc | Pause |

Gamepad is supported, with the right stick aiming.

---

## Friction and gaps

1. **Fire fuel and enemy spawns pull the wrong way.** The spawner's code comment says a dying fire makes the dark more dangerous. The code does the opposite: a full fire spawns fastest and an empty fire spawns nothing. The check that limits spawns to night is also commented out, so skeletons spawn all day.
2. **The loop has no reason to keep going.** It's "survive until noon" and then it's over. Nights don't escalate and nothing carries over between runs. Also, Camp starts at 10:00, so a noon win would fire about 25 seconds in. That's probably a test value.
3. **Skeletons drop nothing**, so fighting pays only in XP. Leather has no use, and the helmet can't be crafted.
4. **Combat XP barely matters yet.** A level costs only 10 XP (5 kills) and gives +1 to every attribute. Weapon XP grows from chopping wood, so you level your axe faster by farming trees than by fighting.
5. **Most of the work is hauling.** With one log per hand, every trip with full hands means walking back to the fire.
6. **Picking up is clunky.** E grabs the item nearest your feet rather than what you aim at, a full-handed pickup drops your weapon into the debris, and getting out of carrying to fight takes several steps.

Items 5 and 6 are what [InventoyManagementPlan.md](InventoyManagementPlan.md) addresses. Item 2 is what [Story.md](Story.md) explores.

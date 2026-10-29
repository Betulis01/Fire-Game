# Story & Goal — Design Ideas

Companion to [InventoyManagementPlan.md](InventoyManagementPlan.md). This document covers **why** the player does what they do: the premise, the long-term goal, dungeons and enemies.

Status: ideation. The recommended direction is chosen in principle; the open questions at the bottom are still undecided.

---

## The problem a story has to solve

The game already has strong mechanics with no reason behind them:

- Why is it always freezing?
- Why do skeletons rise out of the ground around your fire?
- Why does a bigger fire draw more of them?
- What are you working toward beyond surviving until noon?
- Why would there be dungeons in the middle of the wilderness?

A good premise answers all of these at once.

---

## Premises

### 1. The Last Fire

The winter isn't weather; it's an ending. The sun is fading, and your fire is one of the last.

- **The dead** froze in the long winter and are drawn to warmth. "A bigger fire draws more of them" becomes a story rule, not a bug.
- **The goal:** rekindle something big, a beacon or great hearth, that pushes the winter back.
- **Dungeons** are barrows and ruins of the people who tried before you. Deep in each lies an ember or hearthstone that you haul home to grow your fire. Hauling becomes the story itself.

### 2. The Spreading Frost

The cold has a source, something under the land, and it seeps out through wounds.

- **Dungeons** are "frost wells": caves, rifts and frozen hollows where the cold leaks out.
- **Clearing one thaws its region:** warmer days, new resources, a visibly changing map. Progress you can see.
- **The goal:** reach the frozen heart at the end.

### 3. The Living Flame

Your campfire is a spirit, weak and bound to this spot, and it talks to you.

- **It grows as you feed it,** and different fuels please it differently.
- **It asks for things,** which gives you quests and a narrator without needing NPCs.
- **Dungeons** hold its scattered fragments.
- "The fire is the lifegiver" becomes literal.

### Recommended: combine all three

- **The flame is alive** and one of the last.
- **The frost spreads from a source** in the land.
- **The dead are drawn to warmth.**
- **Each dungeon you clear** brings back a fragment that strengthens the flame and thaws a piece of the land.

This gives the game three layers of goals:

| Horizon | Goal |
|---|---|
| Long term | Thaw the frozen heart |
| Mid term | Clear one dungeon per region, bring its fragment home |
| Short term | The existing loop: survive the night, haul fuel, keep the fire alive |

---

## Dungeons

Each type has a reason to exist in the wilderness and a resource it pays out:

| Dungeon | Why it's there | What it gives |
|---|---|---|
| **Barrow / burial mound** | Graves of the people from before the winter. The skeletons come from here | embers, old gear, iron |
| **Collapsed mine** | They dug for iron before the cold; fits the furnace and blacksmithing | iron ore, tools |
| **Buried village** | A settlement under the snow: the dungeon is *down through the ice* | blueprints, lore, food stores |
| **Animal den** (bear cave, wolf hollow) | Natural caves | hides, meat, a boss animal |
| **Spider nest** | Deep caves where the frost-spiders breed | silk (rope, bags), venom |
| **Frost well** | Where the cold leaks out (premise 2) | the region-thawing objective |

---

## Enemies: three factions

Animals alone make the night feel empty. Each faction has **a different relationship to fire**:

| Faction | Relationship to fire | When and where | Examples |
|---|---|---|---|
| **Wildlife** | **Fears it.** A torch drives off a wolf pack | daytime, wilderness, dens | wolves (packs), polar bear, boar, elk (prey), frost-spiders |
| **The Frozen Dead** | **Drawn to it.** They come for your warmth | night, barrows, frost wells | skeletons, frozen archers, husks, a "cinder-eater" that goes for **the fire's fuel, not you** |
| **Orcs** (the old orc sprite) | **Want it.** They compete for fuel | camps in the world, raids on yours | raiders that steal from your woodpile, orc camps with their own fire |

What this does for play:

- **Fire is both a weapon and the prize.**
  - A torch scares wolves away but draws the dead in.
  - A big fire keeps you warm but attracts both the dead and the orcs.
- **Clearing an orc camp gives you its fire as a forward base.** These are the forward fires from Phase 8 of the inventory plan, won in the world rather than crafted.
- **Wildlife feeds the crafts.** Hides and carcasses supply leatherworking, and silk can supply bags and rope (see Ideas in the inventory plan).
- **The cinder-eater threatens the fire itself,** so you sometimes have to defend the fuel rather than yourself.

### Bosses

Each dungeon ends with one:

- **Barrow king:** the oldest of the frozen dead.
- **Spider broodmother:** at the bottom of the nest.
- **The great bear:** in the deepest den.
- **The frozen heart:** the final goal.

---

## How it ties into existing systems

- **The spawn rule already fits.** `EnemySpawner` scales spawns with the fire's fuel. Under this story, more warmth drawing more dead is intended. The night-only check (currently commented out) should come back: the dead rise in the dark.
- **Skeletons** become the first type of Frozen Dead. They need a source (barrows), and could drop embers or bone.
- **The polar bear** is the first piece of wildlife; it needs placing in the world and becomes a den boss later.
- **Hauling becomes the story.** Fragments, embers and hearthstones are carried home like logs, possibly as Big items.
- **A real win condition** replaces "survive to noon": thaw the frozen heart (or the run's equivalent, see question 2).

---

## Open questions

1. **Tone:** bleak and quiet (environmental storytelling, few words), or warmer (the flame talks, a bit of humour)?
2. **Run structure:** one long persistent game where you survive and thaw over time, or runs where each one gets you further?
3. **Dungeon format:** handmade spaces you go down into (a separate scene), or caves and ruins on the overworld map you walk straight into?
4. **The living flame's voice:** how much does it say, and how? Text, symbols, or only through the fire's light and behaviour?
5. **Fragments as Big items:** do they stow your weapons on the way home, so the trip back from a dungeon is itself the challenge?

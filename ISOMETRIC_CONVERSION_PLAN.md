# Orthographic → Isometric grid — build plan

Locked decisions (2026-07-22):
- **Movement**: screen-relative / diagonal. WASD is remapped to the isometric grid's NE/NW/SE/SW axes (classic ARPG-style diagonal movement), not literal up/down/left/right.
- **Facing**: 4 directions — NE, NW, SE, SW. NE and SE are the two drawn clips; NW is NE flipped, SW is SE flipped (`SpriteRenderer.flipX` — the same mechanism already used for the old east/west pair). Confirmed rule: `dir = v.y > 0 ? "ne" : "se"`, `flip = v.x < 0`.
- **Placement**: free (no grid-snapping added to `PlacementGhost`/`DropGhost`).
- **VFX rotation**: stays continuous (`Atan2`-driven), not swapped for direction-bucketed sprites.

## Why the list is shorter than the original audit

The original audit (see chat history / prior artifact) assumed a model where the whole scene gets sheared into isometric space, which would have forced aim, combat vectors, VFX rotation, and range indicators to all route through an iso-basis transform. That's **not** how Unity's `Grid.CellLayout.Isometric` actually works: it only reshapes how the *ground Tilemap's* cells map to world position. Everything else — player, enemies, physics, the camera — keeps living in normal, unsheared world space rendered by a plain orthographic camera. A straight line in world space is still a straight line on screen; a circle is still a circle; world `+Y` is still screen-up.

The locked decisions (free placement, rotating VFX) confirm this is the intended model — both only make sense if aim/placement/rotation math is *not* being remapped. So most of the original "breaks" findings turn out to need no change at all. What's actually left is narrow: the ground's cell layout, the facing bucket count, the player's input mapping, and the world bounds shape (because the ground's world-space footprint genuinely becomes a diamond once its cell layout changes).

Two systems worth a name because they're **more** correct than the original audit assumed, not less:
- **Y-sort** (`YSort.cs`) needs no change. Unity's isometric `CellToWorld` keeps world-Y monotonic with diagonal depth (`cellX + cellY`), so a plain world-Y sort already orders depth correctly against a diamond-laid-out ground.
- **Aim, combat vectors, popups, VFX rotation** — all computed in plain unsheared world space, so they need no change either. Confirm this visually in Task 5 rather than assuming, since it's the one place the reasoning above could be wrong once real art is in.

---

## Task 1 — Isometric grid setup — **done**
Switch the ground `Grid`/`Tilemap` from rectangular to isometric.

- `Grid.m_CellLayout`: `0` (Rectangle) → `2` (Isometric), `m_CellSize` → `{1, 0.5, 0}` — done by hand in the Unity Editor.
- `TilemapRenderer` sort settings (Chunk mode, dedicated "Ground" sorting layer) need no change — the ground renders as its own layer behind entities regardless of cell layout. Verified, not touched.
- Note from the Task 2 session: the scene file on disk still showed the old `Rectangle`/`{1,1,0}` values as of that investigation — worth a quick check that the scene was actually saved (Ctrl+S) in the Editor, since Task 4 (world bounds) depends on the real diamond footprint being there.
- Mostly Unity Editor / scene-asset work, not C#.

**Depends on:** nothing.

## Task 2 — 4-direction facing (`CardinalDir` rebuild) — **done**
Replaced the 3-way e(flip w)/n/s bucket with a 2-clip NE/SE resolver (NW/SW derived by flipping NE/SE respectively), in one shared function, and repointed every place that duplicated the logic instead of calling it.

Confirmed rule: `dir = v.y > 0f ? "ne" : "se"; flip = v.x < 0f;` — `flip` keeps its old meaning (west → `SpriteRenderer.flipX`), so nothing downstream that already just consumed `flip` needed to change.

- `Render/CardinalDir.cs` — the resolver itself, rewritten to the rule above.
- `Render/Visuals/EnemyAnimator.cs` — dropped its independent inline duplicate; both call sites now call `CardinalDir.Resolve` directly.
- `Render/Combat/SwordSwingEffectOrienter.cs` — its `Cardinal` property (unused elsewhere, kept for future consumers) now derives from `CardinalDir.Resolve` instead of its own tie-break. Its continuous rotation math is untouched (rotating VFX, locked decision).
- `Render/Visuals/HeldItemSorter.cs` — `IsFront`'s old leading-hand-by-side branch no longer applies (no more side-on facing exists); simplified to `front = CardinalDir.Resolve(f).dir == "se"`, uniform for both hands.
- `Render/Visuals/PlayerAnimator.cs` — no logic change; already delegated to `CardinalDir.Resolve`. Only header comments updated to the new state names.
- **Corrected out of scope** (both mis-cited in the original draft of this task — neither duplicates the bucket logic, both are continuous-rotation code untouched by this task): `Items/Tool.cs:85`, `Render/Combat/AxeSwingEffectOrienter.cs`.

Not yet done, deliberately out of scope for this task: the Animator Controllers (`Player`/`Enemy Animator Controller.controller`) still only have `s_/n_/e_` states (12 each). `Animator.Play("se_idle")`/`"ne_idle"` won't match anything until new states + art are added — expected, not a regression.

**Depends on:** nothing (built before Task 1's save was confirmed).

## Task 3 — Screen-relative player movement — **done, no-op (confirmed)**
Originally scoped as "remap WASD through an isometric basis so movement follows the diamond's edges." That scoping traced back to an ambiguous label ("screen-relative movement") that conflated two different things: whether input already feels coherent with what's on screen (true today regardless, since only the ground tiles' placement changed, not the character's coordinate space) versus the classic isometric diagonal-grid feel (which would need an actual change of basis, single keys following diamond edges, two-key combos giving true screen directions). Asked directly, with concrete previews of both — **the user chose to keep today's behavior unchanged.**

- No code change. `Entities/Player/PlayerController.cs:64` and `Render/Visuals/PlayerAnimator.cs:58` are the only two consumers of `UserInput.Instance.Move` in the project (confirmed by grep) — both already operate on raw, unremapped input and already agree with each other. Worth having checked: had a remap been chosen, both would have needed it applied through one shared utility to stay consistent (the same lesson as Task 2's duplicated `CardinalDir` logic).
- AI movement (`EnemyMover`, `ChaseBehavior`, `WanderBehavior`) — still unaffected, still untouched, as originally noted.

**Depends on:** Task 2 (done — moot now, no remap to keep consistent with its convention).

## Task 4 — Diamond-aware world bounds — **done**
`Mechanics/WorldBounds.cs` clamped the player/camera to a plain axis-aligned world-space rectangle. Rewritten to clamp in the Grid's *cell* space instead (a rectangle there, a diamond once rendered) — convert to continuous cell coordinates, clamp per-axis exactly like before, convert back. Same trick Unity's own isometric Tilemap uses; reimplemented as unrounded float math rather than calling `Grid.WorldToCell` so a smoothly-moving player/camera doesn't stair-step at the boundary.

- `min`/`max` renamed to `minCell`/`maxCell`, via `[FormerlySerializedAs]` so the scene's already-configured values (`(-13,-13)`/`(15,12)` on the "World Bounds" GameObject — numerically identical to the correct cell bounds, since world and cell coordinates coincided 1:1 under the old rectangular grid) carried over automatically instead of resetting.
- New `cellSize` field (default `1, 0.5`, matching the Grid) — a plain tunable field, not a runtime `Grid` lookup.
- The sprite/camera half-extents inset (`ClampPoint`'s and `ClampCamera`'s "don't let the sprite/viewport visually poke past the edge" behavior) needed its own geometry, not a naive port — worked out to one exact value applied to both cell axes: `cellInset = halfExtents.x/cellSize.x + halfExtents.y/cellSize.y`.
- All three public methods (unchanged signatures) now share one private `ClampInCellSpace` helper instead of duplicating the clamp-with-fallback logic three times.
- `EnemySpawner.cs` and `EnemyMover.cs` needed no changes — both already call `WorldBounds`'s public methods rather than touching `min`/`max` directly, so they're automatically diamond-aware now too. (`EnemySpawner` was confirmed to be a real consumer, sampling ring spawn points then clamping them — exactly the case this task's plan doc flagged as worth checking.)

**Depends on:** Task 1 (done).

## Task 5 — Polish & verification pass — **done**
Re-checked every item on the original checklist by reading the current code fresh (not trusting the prior "no change needed" reasoning blindly), plus a broader sweep of the whole `Assets/Scripts` tree for anything Tasks 1-4 might have missed.

- **Broad sweep: clean.** No other direction-bucketing logic, no other rectangle/position clamping, no other script reads `Grid`/`Tilemap` directly, no prefab has stale `WorldBounds` data — outside the files Tasks 2 and 4 already touched.
- **Checklist items: all confirmed fine, no change needed.** `AimIndicator`, `UserInput.AimDirection`/`AimPoint`, `Knockback`, `AttackRecoil`, `AttackLunge`, `Projectile`, both swing orienters, `DamagePopup`, `CombatTextEmitter`, `YSort.cs`, and the camera scripts all still work correctly — verified the camera is genuinely unrotated (`Camp.unity`, identity rotation) so "world space is unsheared" actually holds, not just assumed. `YSort` caveat for later (not a problem today): a prop spanning multiple diagonal cells would need more than a single-anchor Y-sort — nothing in the game does that yet.
- **Found and fixed instead: combat was completely non-functional.** `PlayerAnimator`/`EnemyAnimator` built attack state names (`ne_attack_r`, etc.) that don't exist in either Animator Controller yet (still only the old 12 `s_/n_/e_` states). Traced the actual consequence: since the clip can never finish, `WeaponUse`'s 3600-second charge failsafe became the *real* timeout — the player's first swing locked out every further attack for a full simulated hour, and never landed a hit or played VFX either (both are Animation-Event-gated). Enemies hit the same bug every ~3 seconds instead. Fixed in both `PlayerAnimator.PlayAttack` and `EnemyAnimator.PlayAttack`: guard with `Animator.HasState(0, Animator.StringToHash(state))` and skip arming the swing (with a console warning) instead of locking up. Discovered during implementation that `EnemyAttacker.Update()` re-arms every frame once `IsAttacking` no longer blocks it, so the warning is deduped via a `static HashSet<string>` (logs once per missing state, not once per enemy per frame). This makes the game safe to playtest again; it does not make combat visually or functionally complete — that still needs real `ne_/se_` states and art.
- **Deferred, needs visual judgment not code:** `Items/Hands.cs` drop-offset/scatter magnitudes are anisotropic against the new 1×0.5 cell (worth an eyeball pass once real art exists, can't be tuned blindly). `WorldBounds` min/max and camera `orthographic size` — already correct from Task 4, but worth a final visual pass once art defines how much of the diamond should actually be on-screen.
- **FYI, not touched:** `Assets/_Recovery/0 (1).unity` still has pre-Task-4 `WorldBounds` values; it's an auto-recovery snapshot not listed in `EditorBuildSettings`, never loaded, harmless.

**Depends on:** Tasks 1-4 (done).

---

All 5 tasks done. What's left is entirely art-side: redraw NE/SE (+ mirrored NW/SW) sprites and add the matching `ne_/se_` states + clips to both Animator Controllers. Everything logic-side is already wired to pick those up automatically once they exist — no further code changes anticipated for the conversion itself.

## Addendum — temporary stopgap: old clips reused under new state names
Rather than leave combat inert behind the `HasState` guard until real art lands, added the missing states directly to both `.controller` assets (hand-edited the YAML — states and their `Motion` clip are separate fields in Unity, so this was possible without the Editor), each one reusing an existing old clip:

- **Player** (`Player Animator Controller.controller`): added `se_idle/walk/attack_l/attack_r` (→ reuse the old `s_*` clips) and `ne_idle/walk/attack_l/attack_r` (→ reuse the old `n_*` clips). 8 new states, new fileIDs `5001000000000000001`-`...008`.
- **Enemy** (`Enemy Animator Controller.controller`): added `se_idle/walk/attack_r` (→ `s_*`) and `ne_idle/walk/attack_r` (→ `n_*`) — no `_attack_l`, since `EnemyAnimator` only ever builds an `_attack_r` name. 6 new states, new fileIDs `5002000000000000001`-`...006`.
- The old `e_*` states/clips are now unused by the new code paths but left in place, untouched.

This makes combat fully functional again (hits land, VFX plays, Animation Events fire — same clips that already had them) with old orthographic poses temporarily showing in the isometric world. The `HasState` guard from earlier in Task 5 is now dormant for these 14 states (kept, per your call — it's free insurance against the next missing state).

**When real art lands**: point each of these 14 states' `m_Motion` at the new clip instead of the reused old one — state names stay exactly as-is, only which clip they play changes. No C# changes needed.

## Addendum — ground-plane light squash, and two-light Z-axis fake
Follow-up from the collider-vs-ellipse question: colliders/hitboxes stay circular (unsheared world space, established earlier), but ground-plane *visual* effects like light pools are a legitimate isometric squash target, since a light spilling across the floor projects as an ellipse at an isometric angle.

- **`Render/Visuals/FireLight.cs`** — added `groundSquash` (default `(1, 0.5)`), multiplied into the existing scale-flicker so the light pool stays elliptical instead of circular. Applies automatically to all four `FireLight` users (`Campfire`, `Furnace`, `Sap Torch`, `Torch`) since it's a new field with a script-level default — no prefab edits needed for that part.
- **Z-axis light height** (a light should illuminate the ground but originate from somewhere above it, which 2D games fake rather than truly model): solved by giving `Torch` a second `Light2D` — `TorchLight`, ground-anchored, shadows off, gets the elliptical `groundSquash` treatment; `TorchLight (1)`, positioned above the torch's origin, shadows **on** (a real `ShadowCaster2D`-driven shadow), stays circular by default (`secondaryGroundSquash = (1,1)`) since it represents the flame/shadow source itself rather than a floor decal.
- `FireLight.cs` only ever drove one `Light2D` before this — extended with an optional `secondaryLight` + its own `secondaryMax*` fields (independent magnitude, shared flicker/boost timing with the primary so both stay in rhythm). `light2D` itself is now an explicit optional `[SerializeField]` rather than always auto-found, so a two-light prefab isn't at the mercy of which light `GetComponentInChildren` happens to grab first.
- `Campfire`/`Furnace`/`Sap Torch` are untouched and still single-light — the new fields default to unassigned, same auto-discover fallback as before.


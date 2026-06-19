---
name: diagnose
description: Run a read-only diagnostic on a named feature, bug, or system in the Fire-Game Unity project. Traces the full chain (scripts + prefabs + scene + physics/layers), finds bugs, and reports them by severity WITHOUT implementing fixes. Use when something is "faulty", "not working", or you want a system audited before touching it.
---

# Diagnose: `$ARGUMENTS`

Run a focused, **read-only** diagnostic of the feature/bug/system named above. The goal is to *find and explain* problems, not fix them.

## Hard rules
- **Do NOT implement.** No `Edit`/`Write` to scripts, prefabs, or scenes. Report only. If the user wants fixes, they'll ask in a follow-up.
- **Trace the whole chain**, not just the one file mentioned. A Unity bug usually spans code + prefab/scene data + project settings.
- **Verify against the actual files** (scripts, `.prefab`/`.unity` YAML, `ProjectSettings/`). Never diagnose from memory or assumption — open the data and confirm.
- Prefer the dedicated tools: `Grep`/`Glob` to locate, `Read` to confirm. Run independent reads in parallel.

## What to inspect (Unity-aware checklist)
Work outward from the named thing through every layer it touches:

1. **Scripts** — read the components involved and everything they call. Follow `GetComponent*`, events, and method calls across files until the chain is complete. Watch for:
   - `GetComponent<T>()` grabbing the *first* of several components (collider/rigidbody ambiguity).
   - `[RequireComponent]` / `[ExecuteInEditMode]` implications.
   - Code that mutates serialized state at runtime (`isTrigger`, `bodyType`, `transform.position`) and silently overrides the Inspector.
   - Lifecycle ordering (`Awake`/`Start`/`OnEnable`, `Update` vs `FixedUpdate`).

2. **Prefab / scene data** (`.prefab`, `.unity` YAML) — the running object often differs from the script's intent. Confirm:
   - Is the scene object actually the prefab instance you think? (Search the prefab GUID in the scene.)
   - Component values: `m_BodyType`, `m_IsTrigger`, collider offsets/radii, `m_Constraints`, serialized field references (`{fileID: ...}`).
   - Overrides on instances that mask the prefab.
   - Geometry: do the relevant colliders actually overlap in their local/world frames?

3. **Physics / layers** — `ProjectSettings/TagManager.asset` (layer names) and `Physics2DSettings.asset` (collision matrix, simulation mode). Check:
   - Object layers and whether those layers collide.
   - Dynamic vs Kinematic vs Static interactions (Kinematic ↔ Static never collide).
   - Trigger callback routing: 2D messages go to the **attached Rigidbody2D's** GameObject, not necessarily the collider's.

4. **Cross-references** — anything else with the same component/pattern that shares the latent bug.

## Output format
Be concise and concrete. Link findings to `file:line`.

1. **Chain traced** — one line showing the flow you followed (e.g. `ToolUser.Strike → Hitbox.Strike → OverlapCircle → GetComponentInParent<Hurtbox> → Health.TakeDamage`), plus the key facts you confirmed (layers, body types, geometry).
2. **Findings by severity** — 🔴 (breaks the feature) / 🟠 (latent / fragile) / 🟡 (cosmetic / confusing). Each finding: what's wrong, the evidence (`file:line` or YAML field), and *why* it produces the symptom.
3. **Root theme** — the underlying cause if several findings share one (e.g. "movement-blocking and combat-detection are tangled on one object").
4. **What's NOT the problem** — briefly note things you checked and ruled out, so the user doesn't re-investigate them.
5. End by offering to write a fix plan or implement — but only on request.

## Notes
- If the named thing is ambiguous, state the interpretation you're diagnosing and proceed with the most likely one; don't stall.
- If a fact can't be confirmed from files (e.g. a value only set in an unsaved editor session), say so explicitly and tell the user what to check in the Inspector.

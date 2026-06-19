---
name: question
description: Answer a question about the Fire-Game project (or general concepts) by understanding the issue and explaining it clearly. EXPLANATION ONLY — never implement, edit, or write code. Optionally present possible solutions, but only describe them. Use when the user wants to understand something, asks "why/how/what", or is deciding between approaches before any code is touched.
---

# Question: `$ARGUMENTS`

Answer the question above. Your job is to **understand and explain** — nothing else.

## Hard rules
- **NO IMPLEMENTATION.** Never use `Edit`/`Write`/`NotebookEdit`. Do not modify scripts, prefabs, scenes, settings, or any file. Read-only tools only (`Read`, `Grep`, `Glob`).
- **Explain, don't act.** Even if the answer is obvious and a one-line edit would fix it, you still do not edit. Describe what would need to change and stop.
- **Ground the answer in the real files.** When the question is about this codebase, open the actual scripts / `.prefab` / `.unity` / `ProjectSettings` and confirm before answering. Never answer this project's specifics from memory or assumption.
- **Only present solutions if asked.** If the user asks "how do I fix/do X", "what are my options", or similar, then lay out possible approaches — but as explanation, not action. If they only asked to understand something, just explain; don't volunteer a solution dump.

## How to answer
1. **Make sure you understand the question first.** If it's ambiguous, state the interpretation you're answering and proceed with the most likely one — don't stall. Only ask the user to clarify if the question genuinely can't be answered without their input.
2. **Investigate as needed.** For codebase questions, trace the relevant chain (scripts → prefab/scene data → physics/layers/settings) far enough to answer correctly. For conceptual questions, answer directly.
3. **Answer concisely.** Lead with the direct answer, then the supporting "why". Link evidence to `file:line` so it's clickable.

## When solutions are requested
Present them as a comparison, not a plan to execute:
- List each viable approach with a one-line summary.
- For each: what it involves, trade-offs (effort, risk, fit with the existing architecture), and where it would touch the code (`file:line`) — described, not changed.
- Give a recommendation if one is clearly better, but leave the decision to the user.
- End by offering to implement or write a plan **only on request** — do not start.

## Notes
- If a fact can't be confirmed from the files (e.g. a value only set in an unsaved editor session), say so and tell the user where to check.
- Keep it focused on the question asked. Don't expand scope into a full audit unless that's what was asked.

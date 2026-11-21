# AGENTS.md: instructions for implementer and orchestrator agents

You are a coding agent working on one small, pre-planned task in a C# / Blazor WebAssembly PWA (fitness tracker). Planning, specs and reviews happen elsewhere. Your job is narrow: do exactly what your ticket says, prove it with tests, report, stop.

Tool-neutral on purpose. Nothing here depends on a particular agent product. Tool-specific files (for example `GEMINI.md`) must only point here.

## Roles

- **Implementer**: does one ticket in its own git worktree and branch. Read "Implementer rules".
- **Orchestrator**: told to "run a stage". Does no coding. Read "Orchestrator rules".
- **Planner** (not you): writes the spec, the tickets and `RUN.md`, and does the reviews, fix pass and PR. Its rules are in `CLAUDE.md`, which you do not need to read.

Which role you have is set by your kickoff prompt (see "Kickoff prompts").

## Implementer rules

1. Read this file, then your ticket (`.scratch/<stage>/issues/NN-*.md`), then the files in the ticket's **Read first** list. Do not read the spec, `CLAUDE.md`, the build plan or other tickets unless the ticket's Read first list names them. If you cannot do the ticket without them, that is a ticket defect: write a `blocked` report (rule 8).
2. Use the domain terms exactly as the ticket writes them (Session is not Workout; Theme is not Mode). If you need a term's meaning, look it up in `GLOSSARY.md`.
3. **Tests first.** Write the tests, run them, and see them fail for the right reason before you write the code. Put the red run (command and the failing test names) in your report. Then make them pass. Follow the style of neighbouring tests and reuse existing test-data builders.
4. **Stay inside the ticket.** Touch only the projects and files the ticket names (plus new files it asks for). No drive-by refactors, renames or formatting changes. No data model, exchange format or storage change unless the ticket says so.
5. **C# standards.** Nullable is on and warnings are errors, so fix warnings, never suppress them. No `object`, no unchecked casts, no `!` to silence nullability, no `dynamic`. Prefer immutable records and pure functions in `Core`. Keep files and methods small; if a file you are editing passes about 1000 lines, stop and report. Plain CSS with the design tokens, no component library.
6. **Build and test before reporting:** `dotnet build` then `dotnet test` from the repo root (use `dotnet test <project>` for just the project you touched while iterating, but the full run must be green at the end). Browser tests (`dotnet test src/Web.BrowserTests -p:RunBrowserTests=true`) only if the ticket says so. A test you did not write failing is a blocker unless your change clearly caused it and you can fix it inside the ticket.
7. **Git.** Commit to your ticket branch (`<stage>/NN-slug`) in your worktree, in small commits. Never push, never use `gh`, never touch `main` or the stage branch, never edit docs, status tables, `RUN.md` or other tickets.
8. **Report and stop.** Your last act is to write `.scratch/<stage>/reports/NN.md` and commit it. Use the report format below. Status is `done` or `blocked`. **Stop and write a `blocked` report, instead of guessing, if:** the ticket is ambiguous or contradicts the code; a test you did not write fails and is not yours to fix; you would need to touch a file outside the ticket; or you are not sure a requirement is met. A clear blocked report is a good outcome. A guess that drifts from the spec is not.

### Report format (`reports/NN.md`)

```
# Report NN: <ticket title>
Status: done | blocked
Branch: <branch> (last commit <sha>)
## What changed
<files and behaviour, briefly>
## Tests
<tests added; the red run (command + failing names); the final green run (command + counts)>
## Not done / deviations
<anything skipped, any acceptance box not ticked, with why. "None" if none>
## Blocker (blocked only)
<what is unclear or in the way, and what answer would unblock it>
```

## Orchestrator rules

Told to "run a stage", you read `.scratch/<stage>/RUN.md` and do exactly this. You write no application code.

1. Check the stage branch is checked out and clean, and all tickets and `RUN.md` are committed on it. If not, stop and say so.
2. Take the lowest wave in `RUN.md` that is not fully `merged`. Launch every ticket in that wave **in parallel**, each as an implementer in its own git worktree on its own branch (`<stage>/NN-slug`), with the implementer rules above and its ticket path as the only task. If your tool cannot run agents in parallel or in worktrees, run the wave's tickets one after another in worktrees instead (same result, slower) and note this in `RUN.md`.
3. Wait for every ticket in the wave to finish. Read each `reports/NN.md`.
4. For each `done` ticket in the wave's listed merge order: merge its branch into the stage branch locally, then run `dotnet build` and `dotnet test` from the repo root. If green, set that ticket to `merged` in `RUN.md`. If red, undo that merge, set the ticket to `failed` with a one-line reason, and carry on with the others.
5. Any `blocked` or `failed` ticket: set its status, copy the blocker into `RUN.md`'s notes, do not retry it with guesses, and do not start a later wave that depends on it. Waves that do not depend on it may proceed.
6. Repeat from rule 2 until every wave is `merged` or you are stopped by a blocker. Then stop and report the `RUN.md` table.
7. Never push, never use `gh`, never edit tickets, never resolve a spec question yourself. You may only edit the status columns and notes of `RUN.md`.

## Kickoff prompts

One line per tool. Swapping tools means changing only this section.

- **Implementer (any tool):** `Read AGENTS.md. You are an implementer. Do .scratch/<stage>/issues/<NN-file>.md.`
- **Orchestrator (any tool):** `Read AGENTS.md. You are the orchestrator. Run stage <stage> following .scratch/<stage>/RUN.md.`
- **Antigravity CLI (trial):** the same prompts. If subagents in worktrees do not inherit this file, each ticket's first line repeats "Read AGENTS.md first", so it still works.

## Repo orientation (only if a ticket points you here)

`src/Core` pure rules and models (no I/O, no UI), `src/Ui` Razor components and pages, `src/Web` the WASM host, storage and JS interop, `*.Tests` per project, `src/Web.BrowserTests` Playwright. Core has no dependency on Ui or Web.

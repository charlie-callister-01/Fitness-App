# Build plan and workflow

## Workflow (every stage; also in `../CLAUDE.md`)

1. **`/to-spec`**: write the spec for the stage.
2. **`/to-tickets`**: break the spec into tickets. Each ticket must fit one agent in roughly **150-200k tokens** of context (reading, writing, tests and fixes). Split anything bigger.
3. **Implement** the tickets (test-first; each ticket includes its tests).
4. **`/code-review`**: normal review of the stage's changes.
5. **`/thermo-nuclear-code-quality-review`**: strict maintainability review (`../.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`). User-invoked only; if it can't be run as a skill, read the file and apply it. Its rules are TypeScript-flavoured; apply the same intent to C# (nullable abuse, `object`, casts, files over 1k lines, ad-hoc branching).
6. **Apply the fixes** from both reviews.
7. **`/pr`**: write the PR body with extra explanation of what changed and why, and any non-obvious decisions.

Don't skip or merge steps, and don't start the next stage until the current stage's PR is written. Issues live in GitHub Issues (`gh` CLI; see `agents/issue-tracker.md`). Labels: needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix.

## Two tracks

**Track A: no UI (can start immediately).** Data spec and core logic, storage, export/import, starter data.

**Track B: UX and design (owner-led).** Give [`design-brief.md`](design-brief.md) to the design agent; it lists requirements, constraints and exactly what to return (a `design-document.md` plus an `assets/` folder). When it comes back, put both under `docs/design/` and add them to this plan. The owner will produce a design document with Claude Design, covering screens, key flows (log a Workout, make up a missed Session, import a Plan, weekly review), a clickable prototype of the log screen to try on a phone, and the UI library/styling choice. It will be added to the build plan when ready. **No UI stage starts until Track B is finished.**

## Proposed stages (each gets its own `/to-spec`; boundaries are a starting proposal)

### Track A
- **A0: Scaffold and delivery.** The four-project solution, .NET LTS confirmed, PWA shell that installs and works offline, CI (build and test on PR), GitHub Pages deploy. First task: confirm the current .NET LTS and the Blazor + shared Razor class library setup.
- **A1: Data model and spec.** Entities and relationships (Plan, Phase, Session, Exercise, Workout, Set, Weigh-in, Pain check, Break, Badge, Guide, settings), permanent IDs, tags, canonical units, format version, JSON serialisation, validation. Written spec first.
- **A2: Core rules.** Log kinds and input parsing, PRs and Max sets, Suggested target with RPE, weeks/Streak/consistency, Missed/Partial/Skipped/Swapped handling, Badges, benchmark logic, unit conversion, week-rearrangement suggestions, Nudge rules.
- **A3: Storage.** The storage interface, an in-memory implementation, the IndexedDB implementation, persistent-storage request, load-into-memory model.
- **A4: Export, Import and prompt pack.** Export picker logic (JSON and Markdown), Import for Backup / Plan / merge / Guides with previews, automatic backup, Exercise matching, error messages, versioned format reading, prompt pack text.
- **A5: Starter data.** The starting Plan converted from `Config.md`, the 27 Guides, and the weight history, all delivered through Import.

### Track B-dependent (after the design document)
Likely stages, to be re-cut once the design exists: Workout log and home dashboard; Guide pages and Exercise library; Plan editor and week view; Progress, Benchmarks, Trophies and weight section; Weekly review and Nudges; Export/Import/Backup screens and Settings; install prompt, Wake Lock, polish.

## Inputs still needed from the owner
- `Fitness/Config.md` (current plan, kinds, badges).
- `Fitness/_scripts/fitness/view.js` and `view.css` (all logic to port; the reference for rules).
- The 27 files in `Fitness/Exercises/*.md` (starter Guides).
- Weight history from the old daily notes (`session::`/`weight` data from 1 Feb 2026) and `Plan/Tracking/Weight.md` / `Homepage.md` logic.
- `Fitness/How to use.md` (current user flow, out of date).
- The Claude Design document (Track B).

None of these block A0 to A4.

## First step in the next session
Run `/to-spec` for **A0 (scaffold and delivery)**, or A0 plus A1 if the owner prefers one larger spec, then `/to-tickets` with the 150-200k token sizing. Read `docs/README.md`, `CLAUDE.md` and `GLOSSARY.md` first.

## Open items to settle during specs
- ~~The exact .NET LTS version and Blazor template details.~~ Settled: .NET 10 LTS, standalone Blazor WebAssembly PWA template ([ADR 0004](adr/0004-dotnet-10-lts-pinned.md)).
- UI library and styling (waits for Track B).
- The precise JSON schema and version number scheme (A1).
- Whether `/to-spec` should treat A0 and A1 as one spec or two.

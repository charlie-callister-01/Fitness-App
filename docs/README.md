# Fitness App: documentation index

Start here in a new session. The grilling session (usage, then practical, then technical) is **complete**. Stages A0 (scaffold and delivery), A1 (data model and exchange format) and A2 (core rules) are merged. A3 (Storage) is implemented, reviewed and fixed (spec `specs/a3-storage.md`; draft PR #14). A4 (Export, Import and prompt pack) is implemented and reviewed (`specs/a4-export-import.md`; draft PR #15, defaults confirmed). B0 (design system and app shell) is merged. B1 (Workout log, core input) is merged (PR #20; `specs/b1-workout-log-core-input.md`), and its open items for the owner are in `build-plan.md`. B2 (Workout log, menus and finishing) is implemented on the `b2-implementation` branch (`specs/b2-workout-log-menus-and-finishing.md`; both reviews and the fix pass are done and the PR body is written; the PR is not yet opened), and its open items for the owner are in `build-plan.md`. B3 (Home) is merged (PR #23; `specs/b3-home.md`; manual phone checks are in `pwa-manual-checks.md`). B3 (Home) is merged (PR #23). The remaining B stages are grouped into Editors (B4 + B5), Data and settings (B9 + B10), Insight (B6 + B7 + B8) and Hardening (B11); Editors (B4 + B5) is implemented on the `b4-implementation` branch (`specs/editors-b4-b5.md`; reviews, fix pass and PR still owed); the next work is A4.5 PR 1. **A4.5 (Tracks) was re-specified on 22 Nov 2026** (explicit Tracks, format version 2; `specs/a4-5-tracks.md`, ADR 0007; two PRs, not yet implemented) and **A5 (Starter data) is repurposed** as an example and test dataset (`specs/a5-starter-data.md`). Order, one implementer at a time: A4.5 PR 1, then A5 or Data and settings, then A4.5 PR 2. See `build-plan.md`.

| Read this | For |
|---|---|
| [`../CLAUDE.md`](../CLAUDE.md) | Working rules, the grill order, and the **build workflow** every stage follows |
| [`../GLOSSARY.md`](../GLOSSARY.md) | The domain language. Use these terms exactly |
| [`product-brief.md`](product-brief.md) | What the app is, who it's for, how it should feel, features and rules |
| [`technical-brief.md`](technical-brief.md) | Stack, solution layout, storage, data exchange, testing, hosting |
| [`build-plan.md`](build-plan.md) | The two tracks, proposed stages, inputs still needed, the first step |
| [`data-spec.md`](data-spec.md) | The written data spec: entities, fields, units, ID rules, JSON exchange format and versioning (final for stage A1) |
| [`design-brief.md`](design-brief.md) | Self-contained brief to give the UI/UX design agent (requirements, constraints, and the design document and assets we want back) |
| [`design/design-document.md`](design/design-document.md) | The design document (v1.0, complete): Themes, tokens, components, screens, flows; assets in `design/assets/` (manifest in `design/assets/README.md`) |
| [`pwa-manual-checks.md`](pwa-manual-checks.md) | How the PWA caches and updates, and the manual phone checks (install, airplane mode, update, the storage checks, and the Theme, shell and Workout log checks) |
| [`grilling-log.md`](grilling-log.md) | The essence of every question and answer (Q1 to Q78) |
| [`adr/`](adr/) | Hard-to-reverse decisions: 0001 data foundation, 0002 stack, 0003 storage, 0004 .NET 10 LTS pinned, 0005 single writer, quarantine and two storage versions, 0006 two Themes (Keep and Clean), 0007 explicit Tracks and format version 2 |
| [`agents/`](agents/) | Skill config: GitHub issues, triage labels, domain docs layout |

Source material from the old system (not yet in this repo): the pre-plan at `C:\Users\calli\Downloads\Fitness App - Pre-plan.md`, and the Obsidian vault folder `Beta/99 Life Planning/Exercise/` (`Fitness/Config.md`, `Fitness/_scripts/fitness/view.js`, `view.css`, `Fitness/Exercises/*.md`, `Fitness/How to use.md`, `Homepage.md`, `Plan/Tracking/Weight.md`).

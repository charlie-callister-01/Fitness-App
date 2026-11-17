# Fitness App: documentation index

Start here in a new session. The grilling session (usage, then practical, then technical) is **complete**. Stages A0 (scaffold and delivery), A1 (data model and exchange format) and A2 (core rules) are merged. A3 (Storage) is implemented, reviewed and fixed (spec `specs/a3-storage.md`; draft PR #14). A4 (Export, Import and prompt pack) is specified (`specs/a4-export-import.md`); tickets are next. See `build-plan.md`.

| Read this | For |
|---|---|
| [`../CLAUDE.md`](../CLAUDE.md) | Working rules, the grill order, and the **build workflow** every stage follows |
| [`../GLOSSARY.md`](../GLOSSARY.md) | The domain language. Use these terms exactly |
| [`product-brief.md`](product-brief.md) | What the app is, who it's for, how it should feel, features and rules |
| [`technical-brief.md`](technical-brief.md) | Stack, solution layout, storage, data exchange, testing, hosting |
| [`build-plan.md`](build-plan.md) | The two tracks, proposed stages, inputs still needed, the first step |
| [`data-spec.md`](data-spec.md) | The written data spec: entities, fields, units, ID rules, JSON exchange format and versioning (final for stage A1) |
| [`design-brief.md`](design-brief.md) | Self-contained brief to give the UI/UX design agent (requirements, constraints, and the design document and assets we want back) |
| [`pwa-manual-checks.md`](pwa-manual-checks.md) | How the PWA caches and updates, and the manual phone checks (install, airplane mode, update, and the storage checks) |
| [`grilling-log.md`](grilling-log.md) | The essence of every question and answer (Q1 to Q78) |
| [`adr/`](adr/) | Hard-to-reverse decisions: 0001 data foundation, 0002 stack, 0003 storage, 0004 .NET 10 LTS pinned, 0005 single writer, quarantine and two storage versions |
| [`agents/`](agents/) | Skill config: GitHub issues, triage labels, domain docs layout |

Source material from the old system (not yet in this repo): the pre-plan at `C:\Users\calli\Downloads\Fitness App - Pre-plan.md`, and the Obsidian vault folder `Beta/99 Life Planning/Exercise/` (`Fitness/Config.md`, `Fitness/_scripts/fitness/view.js`, `view.css`, `Fitness/Exercises/*.md`, `Fitness/How to use.md`, `Homepage.md`, `Plan/Tracking/Weight.md`).

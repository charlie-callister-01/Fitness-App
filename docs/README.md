# Fitness App: documentation index

Start here in a new session via `/start-session`. Current state lives in [`STATUS.md`](STATUS.md) and [`HANDOFF.md`](HANDOFF.md); stage order and notes are in [`build-plan.md`](build-plan.md).

| Read this | For |
|---|---|
| [`../AGENTS.md`](../AGENTS.md) | Working rules, the grill order, and the **build workflow** every stage follows |
| [`../GLOSSARY.md`](../GLOSSARY.md) | The domain language. Use these terms exactly |
| [`product-brief.md`](product-brief.md) | What the app is, who it's for, how it should feel, features and rules |
| [`technical-brief.md`](technical-brief.md) | Stack, solution layout, storage, data exchange, testing, hosting |
| [`build-plan.md`](build-plan.md) | The two tracks, proposed stages, inputs still needed, the first step |
| [`data-spec.md`](data-spec.md) | The written data spec: entities, fields, units, ID rules, JSON exchange format and versioning (formatVersion 2, updated in stage A4.5 for explicit Tracks) |
| [`design-brief.md`](design-brief.md) | Self-contained brief to give the UI/UX design agent (requirements, constraints, and the design document and assets we want back) |
| [`design/design-document.md`](design/design-document.md) | The design document (v1.0, complete): Themes, tokens, components, screens, flows; assets in `design/assets/` (manifest in `design/assets/README.md`) |
| [`pwa-manual-checks.md`](pwa-manual-checks.md) | How the PWA caches and updates, and the manual phone checks (install, airplane mode, update, the storage checks, and the Theme, shell and Workout log checks) |
| [`grilling-log.md`](grilling-log.md) | The essence of every question and answer (Q1 to Q78) |
| [`adr/`](adr/) | Hard-to-reverse decisions: 0001 data foundation, 0002 stack, 0003 storage, 0004 .NET 10 LTS pinned, 0005 single writer, quarantine and two storage versions, 0006 two Themes (Keep and Clean), 0007 explicit Tracks and format version 2 |
| [`agents/`](agents/) | Skill config: local markdown tracker, triage labels, domain docs, workflow settings, GitHub platform |

Source material from the old system (not yet in this repo): the pre-plan at `C:\Users\calli\Downloads\Fitness App - Pre-plan.md`, and the Obsidian vault folder `Beta/99 Life Planning/Exercise/` (`Fitness/Config.md`, `Fitness/_scripts/fitness/view.js`, `view.css`, `Fitness/Exercises/*.md`, `Fitness/How to use.md`, `Homepage.md`, `Plan/Tracking/Weight.md`).

# Handoff: write the A3 (Storage) spec

Paste this file's contents (or point a fresh chat at it) to start the A3 `/to-spec`. File lives at `.scratch/a3-storage/HANDOFF.md`.

**Task: run `/mattpocock-skills:to-spec` for stage A3 (Storage) of the Fitness App.** Stop after the spec is written and pushed. Do not write tickets or code, and wait for the owner before `/to-tickets`.

## Orient first
- Work in the repo root and read `CLAUDE.md`, `docs/README.md` and `GLOSSARY.md` in that order. Then read `docs/technical-brief.md` ("Storage"), `docs/adr/0003-local-first-indexeddb-storage-behind-an-interface.md`, `docs/adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md` and `docs/data-spec.md`.
- Use `docs/specs/a1-data-model-and-spec.md` and `docs/specs/a2-core-rules.md` as the format and style to copy: front matter `labels: ready-for-agent`, `stage: A3`, then the to-spec template sections.
- `main` is at or after `ec2efa5` (PR #12). It has A0 (scaffold and delivery), A1 (data model and exchange format) and A2 (core rules), all merged, with 539 Core and 9 Ui tests green.
- Start from fresh `origin/main` and create a new worktree and branch, for example `worktree-a3-spec`. Don't reuse `worktree-a1-spec` or `worktree-a2-spec`, which are merged.
- `gh` is at `C:\Program Files\GitHub CLI\gh.exe` and is not on the bash PATH. Use PowerShell and it works. Specs and tickets have so far been local files under `docs/specs/` and `.scratch/<stage>/issues/`, matching A1 and A2.

## What A3 is
- The build plan says: "The storage interface, an in-memory implementation, the IndexedDB implementation, persistent-storage request, load-into-memory model."
- ADR 0003 and the technical brief fix the shape:
  - IndexedDB is used as a document store behind a storage interface that lives in `Core`.
  - The whole dataset is loaded into memory at startup and all queries run in `Core` (so A2's rules apply to in-memory data).
  - Each change saves only the one record that changed.
  - An in-memory implementation is used by tests.
  - The app requests persistent storage on first run.
  - A later MAUI build implements the same interface with SQLite or files.
- The Core project has no dependencies and "today" comes from an injected clock. Storage and the clock are interfaces in `Core`.

## Facts about the code that the spec must respect
- The A1 model is `ExchangeDocument`, with records for Exercises, Settings, Plans (Phases and Sessions), Weigh-ins, Breaks, Badges, Guides, Workouts (Sets and a Pain check) and Week schedules. Every record has a permanent string ID, except Guides, which are keyed by `exerciseId`, and Settings and Week schedules, which are singletons or keyed by week.
- A1 already provides a validated JSON exchange format (`ExchangeFormat.Read`, `Write` and `Validate`), and the A2 rules are pure functions over this model.
- The JS bridge for IndexedDB lives in the Blazor Web host, not in `Core`. `Ui` is a Razor class library with no rules.
- The storage interface needs to cover load-all, save and delete per record kind, and Settings. It also needs an atomic whole-dataset replace, which A4 will use for restoring a Backup, and a way to record the last-Backup timestamp. A2's Backup Nudge currently takes that timestamp as an input, so A3 or A4 must own its storage.

## Decisions the to-spec needs to make or confirm (don't interview at length; propose defaults and flag them)
- The shape of the interface: per-kind repositories versus one generic document store with a kind and an ID. Also the unit of "save one record changed" for nested records (a Workout holds its Sets and Pain check; a Plan holds Phases and Sessions).
- Store layout and schema versioning: how IndexedDB stores map to record kinds, how the store schema upgrades when the app updates, and how this relates to the exchange `formatVersion`.
- Concurrency and failure behaviour: quota exceeded, storage cleared by the browser, a blocked upgrade, and two tabs open. A change should not silently be lost. Say what the user sees.
- Persistent-storage request: when it is asked, and what is exposed if it is denied.
- Startup load and first-run state: empty database, and how an invalid or corrupt record is handled (the A1 validator exists for this).
- How the in-memory model exposes "today's" snapshot to the A2 rules (the rules take data as arguments, not a store).
- Seam for tests: the rule is "prefer the highest seam, ideally one". The proposal is a contract-test suite run against both implementations (in-memory, and IndexedDB via bUnit or a Playwright browser run). Say plainly what can and cannot be tested without a browser.
- Out of scope: Export, Import, Backup file handling and Exercise matching (A4), starter data (A5), any UI, sync or accounts.

## Housekeeping to flag, not necessarily fix
- `docs/build-plan.md` and `docs/README.md` have a stale status. They say A1 is next; A1 and A2 are done. Update them as part of the A3 spec commit or note it for the owner.
- The A2 spec lists nine assumptions to check against the old Obsidian `view.js`, which is not yet in the repo.

## Workflow rules (from CLAUDE.md)
- Use the glossary's terms exactly.
- Don't skip steps and don't start A4 until A3's PR is written.
- The next step after the spec is `/to-tickets`, with each ticket sized to about 150-200k tokens.
- The review step must use `/mattpocock-skills:code-review`, not the built-in one.

## Deliverable
A pushed branch with `docs/specs/a3-storage.md`, a short summary of the proposed seam and the key defaults, and the open questions that need the owner's answer before tickets.

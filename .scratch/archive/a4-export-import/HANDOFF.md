# Handoff: write the A4 (Export, Import and prompt pack) spec

Point a fresh chat at this file to start the A4 `/mattpocock-skills:to-spec`. File lives at `.scratch/a4-export-import/HANDOFF.md`.

**Task: run `/mattpocock-skills:to-spec` for stage A4 of the Fitness App.** Stop after the spec is written and pushed. Don't write tickets or code; wait for the owner before `/to-tickets`.

## State of the repo
- A0 to A2 are merged. **A3 (Storage) is a draft PR: https://github.com/charlie-callister-01/Fitness-App/pull/14** (branch `a3-storage`, last commit `0766576`). It is implemented, reviewed (`mattpocock-skills:code-review` and the thermo-nuclear review) and fixed. 648 Core, 32 Web, 9 Ui tests and 37 opt-in browser tests (headless Chromium) pass locally.
- **Before starting A4:** check whether PR #14 has merged. If not, base the A4 work on `a3-storage` (A4 builds on its API). Also check the new `browser-tests` CI job: it had never run on GitHub when this was written. If it fails there, fix that first (it is A3's problem, not A4's).
- Work in a new worktree and branch (for example `worktree-a4-spec`). Don't reuse old ones. `gh` is at `C:\Program Files\GitHub CLI\gh.exe` and works from PowerShell, not bash. Specs and tickets are local files: `docs/specs/<stage>.md` and `.scratch/<stage>/issues/NN-*.md`, as in A1 to A3.

## Read first, in order
`CLAUDE.md`, `docs/README.md`, `GLOSSARY.md`, `docs/product-brief.md` (Export and Import sections), `docs/technical-brief.md`, `docs/data-spec.md`, `docs/build-plan.md` (A4 entry and "Known sharp edges from A3"), `docs/adr/0001` and `0005`, then `docs/specs/a3-storage.md` and `docs/specs/a2-core-rules.md` for format and style. Copy their layout: front matter `labels: ready-for-agent`, `stage: A4`, then the to-spec template sections.

## What A4 is
Build plan: "Export picker logic (JSON and Markdown), Import for Backup / Plan / merge / Guides with previews, automatic backup, Exercise matching, error messages, versioned format reading, prompt pack text." Product brief sections on Export/Import hold the user-facing rules (picker with date range default last 12 weeks and tick boxes for Workouts, Plan, Weigh-ins, Pain checks, Guides, Exercises; paste box first, file picker second; plain-language preview before anything changes; automatic backup before applying; field-level error messages naming the exact field; matching Exercises by ID, then exact name, then suggested near-matches for confirmation; never silent merges). No UI in this stage: the logic and its tests only (screens are Track B).

## What A3 gives A4 (all in `Core`, namespace `Core.Storage`)
- `AppData`: the in-memory model. `Snapshot` is an immutable `DataSnapshot` (`ToDocument()` gives an `ExchangeDocument` for Export/Backup). `CommitAsync(Change...)` validates (strict, A1 validator), saves, then publishes; returns `CommitResult` (`Committed`, `Rejected(issues)`, `StoreFailed`, `Blocked`). `Changed` event.
- **Restore a Backup** = `AppData.ReplaceAllAsync(ExchangeDocument)`: validates, atomic replace of everything, works from any startup state including `CannotRead`, sets `Initialised` and the format version. It does not stamp the Backup time: call `RecordBackupAsync(now)` afterwards (separate, non-atomic write; A4 may want an atomic variant). `LastBackupAt` is exposed; A2's Backup Nudge takes it as an input.
- **Automatic backup before an Import** is A4's job: produce the Backup document from `Snapshot.ToDocument()` and hand it to the delivery path before applying. A3 does not write files.
- Merge/Import of partial documents (Plan only, Guides only, changes): build `Change` puts/deletes and commit them in one batch so they are atomic. Imported records need IDs; `Change.Put<T>` is generic and a non-record argument fails at run time.
- Commits are blocked for records set aside at startup (quarantine, code `record_set_aside`) and when startup was `CannotRead`/`StorageFailed`. Only replace-all (restore) replaces quarantined records.
- A1 exchange format: `ExchangeFormat.Read/Write/Validate`, versioned reader, field-level `FormatIssue` (path, code, message). A2 added optional Week schedules without a version bump.
- Time comes from the injected `IClock`; `AppData` never reads a clock.

## Decisions A4 will probably need to make (propose defaults, flag them, don't interview at length)
- How Import previews are computed (a pure function from current snapshot + incoming document to a plain-language change list) and the exact "merge" semantics per Import kind (Backup restore vs Plan as a new Plan vs merge of small changes vs Guides). Conflicts and ID collisions.
- Exercise matching algorithm (ID, exact name, near-match suggestions) and the result type the UI will confirm against.
- Export slicing (date range, kinds) and the Markdown rendering for chats; what the `readMe` block says; which health data is off by default.
- Prompt pack text (analyse my training, write me a Plan, write Guides) embedding the format spec and the current Exercise library.
- Whether the restore + `RecordBackupAsync` should become one atomic call (small A3 API addition) and who stamps the Backup time for an Export that is a full Backup.
- Delivery (download, copy, share) is a UI/host concern: say what Core returns (text and a suggested file name) and leave the rest out of scope.
- Seams for tests: the highest seam, ideally one (A1 tested through the exchange-format reader, A2 through the rules surface, A3 through the `IDataStore` contract).

## Known sharp edges from A3 that touch A4 (also in `docs/build-plan.md`)
- If the only active Plan is quarantined, startup is `CannotRead` (`no_active_plan`) and commits are blocked: restore is the only way out, so restore must work from that state (it does).
- A migration that quarantines any record fails the whole migration (deliberate).
- No real layout upgrade has been exercised (layout version is still 1).
- The A2 spec's nine assumptions still need checking against the old Obsidian `view.js`, which is not in the repo yet.

## Housekeeping to remember
- Update `docs/build-plan.md` and `docs/README.md` status as part of the A4 spec commit (they will say A3 is "implemented, PR pending" until PR #14 merges).
- Use the glossary's terms exactly (Export, Backup, Import, Prompt pack, Session vs Workout).
- Workflow after the spec: `/to-tickets` (each ticket about 150-200k tokens), implement, `/mattpocock-skills:code-review` (not the built-in), the thermo-nuclear review (`.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`, apply to C# and JS), apply fixes, then `/mattpocock-skills:pr`. Don't start A5 until A4's PR is written.

## Deliverable
A pushed branch with `docs/specs/a4-export-import.md`, a short summary of the proposed seam and key defaults, and the open questions that need the owner's answer before tickets.

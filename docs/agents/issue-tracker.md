# Issue tracker: Local Markdown

Stage specs live in `docs/specs/<stage>.md`, and implementation tickets live as markdown files in `.scratch/<stage>/issues/`. Markdown is the source of truth because the skills read it.

## Conventions

- One working folder per stage: `.scratch/<stage>/` (e.g. `.scratch/a4-5-tracks/`), with the stage spec at `docs/specs/<stage>.md` and a row in `docs/build-plan.md`.
- Implementation tickets are one file each at `.scratch/<stage>/issues/<NN>-<slug>.md`, numbered from `01`, never a single combined file. Size each ticket so one agent can finish it within roughly 150-200k tokens of context.
- Triage state is a `Status:` line near the top of each ticket holding **exactly one** label from `triage-labels.md` (or `resolved`). Any detail goes on its own `Notes:` line underneath, never inside the Status line.
- Comments and history append to the bottom of the file under `## Comments`.
- Durable outcomes (decisions, status) are copied into `docs/adr/`, `docs/STATUS.md` and `docs/LOG.md`; `.scratch/` is working space.
- When a stage is merged, move `.scratch/<stage>/` to `.scratch/archive/` (move, never delete).

## When a skill says "publish to the issue tracker"

Create the stage spec in `docs/specs/<stage>.md` (or ticket files under `.scratch/<stage>/issues/`) and update `docs/build-plan.md` / `docs/STATUS.md`.

## When a skill says "fetch the relevant ticket"

Read the file at the referenced path (normally `.scratch/<stage>/issues/<NN>-<slug>.md`). Archived work is under `.scratch/archive/`.

## Wayfinding operations

Used by `/wayfinder` for foggy efforts. Its tickets are **decision tickets** (questions to decide), never **work tickets** (slices of a spec to build): they live apart. The map is `.scratch/map-<effort>/map.md`; each decision ticket is `.scratch/map-<effort>/decisions/NN-<slug>.md` with a `Type:` line (`research`/`prototype`/`grilling`/`task`), a `Status:` line (`open`/`claimed`/`resolved`) and a `Blocked by: NN, NN` line. Frontier = open, unblocked, unclaimed files, first number wins. Claim by setting `Status: claimed` before work; resolve by appending an `## Answer` section, setting `Status: resolved`, and adding a pointer to the map's Decisions-so-far.

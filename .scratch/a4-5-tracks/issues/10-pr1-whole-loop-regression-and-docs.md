# 10: PR 1 whole-loop test, regression sweep and docs

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md`, the whole-loop test in `src/Core.Tests` (search for the A4 loop that exports then restores a dataset), `docs/build-plan.md`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1, "Further Notes".

**What to build:** prove PR 1 as a whole, then close it out.
- Extend the whole-loop test: a two-Track dataset (a weekly main Track with Phases, a daily mobility Track with its own Phases, Workouts on both, a Break, a Badge `streak:<id>`, a Week schedule) is Exported, Restored into a fresh `AppData`, and every rule result (Streaks, daily run, week counts, Phases, Home view data, Badge progress, Nudges) is identical before and after. Add the same loop starting from a v1 Backup and from a stored v1 dataset (ticket 03).
- Regression sweep: run the whole suite and write any test that needed more than a mechanical change under Notes; confirm the data-spec JSON blocks are all parsed by the conformance test.
- Docs: `docs/data-spec.md` matches the code; `docs/build-plan.md` status for A4.5 (PR 1 delivered, tests counts, the open items below) and `docs/README.md`; the `.scratch/a4-5-tracks/HANDOFF.md` state section; `docs/specs/editors-b4-b5.md` gets a one-line pointer that the Plan, Week and Home behaviour is Track-aware from A4.5.
- Then the stage steps from `CLAUDE.md` for PR 1: `/mattpocock-skills:code-review` against the commit before ticket 01, the thermo-nuclear review (applied as C#), the fix pass, and `/mattpocock-skills:pr` (commit the body as `.scratch/a4-5-tracks/pr-body-1.md`).

**Blocked by:** 03, 04, 05, 06, 07, 08, 09.

**Status:** ready-for-agent

- [ ] The extended whole-loop test passes for the v2, v1 Backup and stored v1 starts.
- [ ] `dotnet build` 0 warnings; `dotnet test` all green (state the new Core, Ui and Web counts in the PR body).
- [ ] No test was loosened; every changed expectation is listed with the reason.
- [ ] Docs, handoff and PR body written; both reviews and their fixes done.

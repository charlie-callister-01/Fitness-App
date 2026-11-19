# 06: Whole-loop test, regression sweep and docs

**What to build:** Proof that the whole stage is consistent and nothing older broke. A multi-Track dataset (weekly and daily Tracks, Track Phases, Track Badges, Workouts on each) is exported as a Backup, the store wiped, restored, and the snapshot equals the original. The docs describe Tracks in the project's own words. Spec: "Testing Decisions", "Further Notes".

**Blocked by:** 04, 05.

**Status:** ready-for-agent

- [ ] The whole-loop Backup round-trip test runs on a multi-Track dataset and the restored snapshot equals the original.
- [ ] The full suite (Core, Ui, Web) passes, with every pre-existing A1 to A4 test unchanged; any that needed changing are listed in the PR body as findings.
- [ ] `GLOSSARY.md` gains Track, main track, cadence and daily Streak, with terms used exactly as in the spec.
- [ ] `docs/data-spec.md`, `docs/technical-brief.md` (if it names per-Plan rules), `docs/build-plan.md` (A4.5 done, A5 unblocked, B3/B5/B6/B7 notes about per-Track results) and `docs/README.md` are current.
- [ ] A short ADR records the decision: tracks inside one active Plan (not several active Plans), per-Track Phases optional, `formatVersion` staying 1.
- [ ] A handoff for A5 (`.scratch/a5-starter-data/HANDOFF.md`) is updated to say A4.5 is merged.

# Handoff: Insight (B6 + B7 + B8)

Read `AGENTS.md`, `docs/README.md` and `GLOSSARY.md`, then `docs/specs/insight-b6-b8.md` and the ticket you are working on. Conventions and gotchas: `.scratch/archive/editors/HANDOFF.md` (or `.scratch/a4-5-tracks/HANDOFF.md` on the A4.5 branch).

## State
Spec and ten tickets written; nothing implemented. Branch `feature/insight` from `main`, in its own worktree. One implementer at a time. Independent of A4.5: unqualified views mean the main Track and take plain Plan, Workouts, Breaks, Weigh-ins and Exercises inputs so per-Track variants can be added later.

## Order and blocking
1. 01 Progress tab with Exercise charts (none)
2. 02 Chart date range (01)
3. 03 Benchmarks tab (01)
4. 04 Calendar tab (01)
5. 05 Weight screen (01)
6. 06 Trophies: Badges tab and detail (03)
7. 07 Badge editor (06)
8. 08 Bests tab and UnlockToast (06)
9. 09 Weekly review (05)
10. 10 Navigation, Nudge copy, regression and docs, then reviews and the PR body (02, 04, 07, 08, 09)

Commit after each ticket as `Insight: ticket NN <title>` with a green build and tests.

## Decisions already made (owner)
- Two test seams: Core.Tests for view builders, bUnit page tests via `PageFixture`. No new Playwright seam.
- Main Track only, Track-ready. No Track picker in the Badge editor.
- Weekly review's export button is hidden until B9.
- Estimated-1RM formula is settled in ticket 01 (Epley by default).

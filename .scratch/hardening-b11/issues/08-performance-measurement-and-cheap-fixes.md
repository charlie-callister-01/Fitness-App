# 08: Performance measurement and cheap fixes

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Performance).

**What to build:** Measure the deployed Pages build after tickets 03–07 have landed: Lighthouse mobile (target ≥ 90) and a warm-cache cold start to interactive Home on the owner's phone (target < 3 s). If a target is missed, try in order: confirm IL trimming is effective; stop loading all globalisation data, or use invariant globalisation where no culture-specific behaviour is needed; make sure compressed (Brotli) assets are served. No AOT and no lazy-loaded assemblies; if still short, record the gap and propose a follow-up spec. Pages deploys `main`, so this ticket runs after the Hardening PR merges, with any fixes in a small follow-up PR.

**Blocked by:** 03, 04, 05, 06, 07

**Status:** ready-for-human

Notes: needs the owner's phone and a deployed build.

- [ ] Lighthouse mobile score and warm-start time recorded in `docs/pwa-manual-checks.md` with date and build identifier.
- [ ] If a target was missed, each cheap setting tried is recorded with before/after figures.
- [ ] Any build-setting change keeps `dotnet build` at 0 warnings and all tests green, and the published app still works offline.

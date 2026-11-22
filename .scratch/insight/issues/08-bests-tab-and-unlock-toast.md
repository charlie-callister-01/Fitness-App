# 08: Bests tab and UnlockToast

Spec: `docs/specs/insight-b6-b8.md` (S-13 Bests, C-23). Stories 50-53.

**Status:** resolved

**Blocked by:** 06 Trophies: Badges tab and detail.

**What to build:** The Bests tab lists each Exercise's personal best with its date, plus a "Best of the week" row (the strongest PR in the current Monday-to-Sunday week, or none). The finish summary's static "Badge unlocked" line becomes the real UnlockToast: shown on the finish summary only (never during logging), auto-hides after 4 s, tap opens the Badge, a fade and 8 px rise over 200 ms, instant under reduced motion, no sound or haptics. Newly unlocked Badges still come from the existing before-and-after comparison.

New pure Core Bests view built on the existing Best and PR rules: per Exercise best and date, and the Best of the week.

- [x] Core view has Core.Tests: bests per log kind, lower-is-better, no PR this week, tie-break for Best of the week, Exercises with no Best are omitted
- [x] Bests tab bUnit tests via `PageFixture`: populated and empty
- [x] UnlockToast bUnit tests: appears on finish with a newly unlocked Badge, none when nothing unlocked, auto-hide after 4 s, tap opens the Badge, reduced motion has no animation
- [x] Existing finish-summary tests updated, not loosened
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 08 Bests tab and UnlockToast`

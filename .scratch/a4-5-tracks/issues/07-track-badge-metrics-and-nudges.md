# 07: Track Badge metrics and Nudges per Track (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Badges", "Nudges"), `docs/data-spec.md` (Badge), `src/Core/Rules/Badges/*`, `src/Core/Rules/Nudges/NudgeRules.cs`, `src/Core/Rules/Home/HomeNudges.cs`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:**
- **Badge metrics.** Unqualified `streak`, `bestweek` and `sessions` keep their meaning and measure the **main Track** (`sessions` therefore counts the main Track's counted Workouts). New qualified metrics, case-insensitive, Track ID after a colon: `streak:<trackId>` (best run: weeks on a weekly Track, days on a daily one), `days:<trackId>` (daily: total done days; weekly: total counted Workouts of that Track), `bestweek:<trackId>` (weekly only; a daily Track is "not measurable"). An unknown Track ID, or an empty ID after the colon, is "not measurable" (the Badge is still stored). `BadgeHistory` carries what the rules need per Track. `prs`, `bodyweight` and Exercise metrics are unchanged.
- **Nudges.** "One Session short of this week's target" and "Session X was missed" are computed per weekly Track (each from that Track's own week count and Missed Sessions) and name the Track in the message when the Plan has more than one weekly Track. No daily-Track Nudge. Dismissal keys must stay unique per Track (check `NudgeDismissals` in Ui and whatever key `Nudge` carries; add the Track ID to the key if two Tracks could clash).
- The Home Nudge cap (at most three, Home only) is unchanged.

**Blocked by:** 04, 06.

**Status:** resolved
**Notes:** Implemented qualified Badge metrics (`streak:<id>`, `days:<id>`, `bestweek:<id>`) and per-weekly-Track Nudges with Track-scoped dismissal keys. Note: `HomeNudges.Select` caps at 2 (`b3-home.md`), left unchanged.

- [x] Tests: each qualified metric on a weekly Track and a daily Track; `bestweek:<id>` on a daily Track is not measurable; an unknown and an empty Track ID are not measurable; case-insensitive Track ID; unqualified metrics on a two-Track Plan equal the main Track's values (and not the other Track's); a Badge that names a Track keeps working after the Tracks are reordered.
- [x] Nudge tests: two weekly Tracks, one short, one on target gives one Nudge naming the short Track; a one-Track Plan's Nudge wording is unchanged; two Missed Sessions on different Tracks give two Nudges with distinct keys.
- [x] Existing Badge and Nudge tests pass unchanged for one-Track Plans.
- [x] `dotnet build` 0 warnings; all tests pass.

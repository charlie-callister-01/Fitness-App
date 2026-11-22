# 15: Guide and Library Track context, manual checks and docs (PR 2)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Screens (PR 2)"), `src/Ui/Pages/GuidePage.razor`, `Exercises.razor`, `src/Core/Rules/Guides/GuideHistoryView.cs`, `Rules/Library/LibraryViewBuilder.cs`, `docs/pwa-manual-checks.md`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 2.

**What to build:**
- **Guide header** "where it sits in the Plan" names the Track(s) of the Sessions that hold the Exercise when the Plan has more than one Track (for example "A: Lower + core (Main)", "Morning mobility (Morning mobility, daily)").
- **Library** Session groups name the Track in the group heading when the Plan has more than one Track; the "Not in a Session" group is unchanged.
- **Manual phone checks** in `docs/pwa-manual-checks.md`: a section "Tracks manual checks (A4.5 PR 2)" covering the Track selector at 360 px in both Themes and Modes, Add Track weekly and daily, Move up and Move down, per-Track Phases and Sessions, the Week "Every day" row, Home daily cards and extra week lines, the Plans headline, a v1 Backup restored then edited, and the read-only state.
- **Docs:** `docs/build-plan.md` and `docs/README.md` status (A4.5 PR 2 delivered; counts; open items), `docs/specs/a4-5-tracks.md` Further Notes (what changed from the spec, if anything), `.scratch/a4-5-tracks/HANDOFF.md` state.
- Then the stage steps from `CLAUDE.md` for PR 2: `/mattpocock-skills:code-review` against the commit before ticket 11, the thermo-nuclear review (as C#), the fix pass, `/mattpocock-skills:pr` (commit the body as `.scratch/a4-5-tracks/pr-body-2.md`).

**Blocked by:** 14.

**Status:** in-progress

- [x] bUnit tests: Guide header and Library headings name Tracks with two Tracks and are unchanged with one.
- [x] The manual-checks section is written and unticked.
- [x] `dotnet build` 0 warnings; all tests green; counts recorded.
- [ ] Docs, handoff and PR body written; both reviews and their fixes done.

## Comments
- **Guide & Library Track context (`GuideHistoryView.cs`, `GuidePage.razor.cs`):**
  - Added `SessionLabel` to `GuidePlacement` in `GuideHistoryView`: returns `session.Name` when the Plan has one Track, `$"{session.Name} ({track.Name})"` for a weekly Track when `plan.Tracks.Count > 1`, and `$"{session.Name} ({track.Name}, daily)"` for a daily Track when `plan.Tracks.Count > 1`.
  - Updated `GuidePage.HeaderLine` and `GuidePage.PlacementText` to render `SessionLabel` across all placements, keeping single-Track output unchanged while qualifying multi-Track placements (e.g. `"A: Lower + core (Main)"`, `"Morning mobility (Morning mobility, daily)"`).
  - Verified `LibraryViewBuilder` and `Exercises.razor` group headings (`"{Track.Name} · {Session.Name}"` when `plan.Tracks.Count > 1`, `"{Session.Name}"` when `plan.Tracks.Count == 1`, `"Not in a Session"` unchanged).
- **Tests (`GuideHistoryViewTests.cs`, `GuidePageTests.cs`, `ExerciseLibraryTests.cs`):**
  - Added Core unit test `Placements_name_each_Sessions_Track_when_the_Plan_has_more_than_one_Track` in `GuideHistoryViewTests`.
  - Added bUnit test `The_header_and_place_name_the_Track_when_the_Plan_has_more_than_one_Track` in `GuidePageTests`.
  - Added bUnit test `Session_group_headings_name_the_Track_when_the_Plan_has_more_than_one_Track` in `ExerciseLibraryTests`.
- **Manual checks & docs:**
  - Added unticked section `## Tracks manual checks (A4.5 PR 2)` (11 items) in `docs/pwa-manual-checks.md`.
  - Updated `docs/build-plan.md`, `docs/README.md`, `docs/specs/a4-5-tracks.md` (`## Further Notes`), and `.scratch/a4-5-tracks/HANDOFF.md` with PR 2 completion and test counts (**1,675 Core, 754 Ui, 39 Web tests green; 0 build warnings**).

## Summary

Brings explicit **Tracks** (`weekly` and `daily`) to the screens (`docs/specs/a4-5-tracks.md`, PR 2, tickets 11–15): the Plan editor's Track selector, split Settings, and per-Track Phases and Sessions tabs; the Week view's `"Every day"` row and extra weekly Track rows; Home's daily Track cards and extra weekly Track week lines; the Plans list headline and Track count; and multi-Track context on the Guide and Exercise library pages.

```diff
 <PlanEditor> (/plans/{PlanId})
   PlanEditState (Draft, SelectedTrackId, PlanSettingsForm + per-Track TrackSettingsForm)
+  Track selector (chips: Main hint + Track names, + Add Track sheet, Move up / Move down)
   <PlanSettingsTab>
-    Single Plan settings list
+    Plan settings (Start date, Goal, Goal date, Weight increment)
+    Track settings for SelectedTrack (Name; Weekly: target, Streak min, Custom workouts on Main; Daily: day-done %)
   <PlanPhasesTab>
-    Main Track Phases only
+    SelectedTrack.Phases (Plan-wide week dates, Track-scoped override Sessions, "No Phases for this Track")
   <PlanSessionsTab>
-    All Plan Sessions
+    SelectedTrack Sessions (Daily: "Every day", no day picker; Weekly: Mon-Sun; Mobility type; Benchmark on Main only)
+  Cross-Track problem banner ("Fix the problem on the Phases (Mobility) tab to save.")

 <Week> (/week)
+  WeekRows.BuildDaily -> "Every day" rows above the week grid (today's state, Start / Continue / View summary, never Missed, no Move)
   WeekRows.Build      -> all weekly Tracks' Sessions on their days (shows Track name when >1 weekly Track)

 <Home> (/)
   <TodayCard /> (Main track)
+  DailyTrackCard[] (Track name, daily Streak, Session, NotStarted/InProgress/Done/OnBreak, Start / Continue)
   <WeekStrip /> & <WeekLine /> (Main track)
+  ExtraWeekLine[] (extra weekly Tracks: Done of Scheduled, target, Streak, Missed, tappable Session buttons)

 <GuidePage> (/exercises/{ExerciseId}/guide) & <Exercises> (/exercises)
+  Multi-Track Session labels ("A: Lower + core (Main)", "Morning mobility (Morning mobility, daily)", "{Track} · {Session}")
```

- **Plan editor Track selector & split Settings (ticket 11):** Track selector row above the tabs with a `"Main"` hint on `tracks[0]`, `+ Add Track` (`FormSheet` for name and Cadence), and `Move up` / `Move down` (surfacing plain refusals from `TrackEditRules`, such as moving a Daily Track first). `PlanSettingsTab` splits into Plan settings and Track settings for the selected Track; `PlanSettingsForm` preserves typed fields across tab and Track switches.
- **Per-Track Phases & Sessions tabs (ticket 12):** `PlanPhasesTab` and `PlanSessionsTab` edit the selected Track (`@key="State.SelectedTrackId"`). Daily Track Sessions show `"Every day"` with no day picker; Benchmark exercise toggles and benchmark intervals appear on the Main track only. `PlanEditState` validates Settings, Phases, and Session rows across every Track, naming non-selected Tracks in `ProblemTab` (`"Phases (Mobility)"`).
- **Week view daily row & extra weekly Tracks (ticket 13):** `WeekRows.BuildDaily` returns `"Every day"` rows above the week grid with today's state (`Not started`, `In progress`, `Done`) and `Start` / `Continue` / `View summary` actions—never a `Missed` chip and never movable. `WeekRows.Build` and `WeekEditRules.SuggestDraft` cover all weekly Tracks and show the Track name when a Plan has more than one weekly Track.
- **Plans list & Home Tracks (ticket 14):** `Plans` shows `"{n} Tracks"` when `n > 1` and `PlanEditor` shows `PlanProgress.ActiveLabel` on the active Plan header. `Home` renders a compact card per Daily Track (`_view.DailyTracks`) and a week line per extra weekly Track (`_view.ExtraWeekLines`) with `GuardedSave` and read-only guards.
- **Guide, Exercise library, manual checks & docs (ticket 15):** `GuideHistoryView` and `GuidePage` qualify Session names with their Track (and `, daily` for Daily Tracks) when the Plan has more than one Track, and resolve the current Phase across the Exercise's placed Tracks. Added `## Tracks manual checks (A4.5 PR 2)` in `docs/pwa-manual-checks.md`.

## Evidence

- **Before (on `main` before PR 2):** Core 1,670, Ui 715, Web 39 tests passing (2,424 total).
- **After (A4.5 PR 2 tickets 11–15 + review fixes):** `dotnet build` **0 warnings, 0 errors**; **Core 1,675 (+5), Ui 754 (+39), Web 39 tests passing (2,468 total, +44 new tests)**.

```text
Single-Track invariance:
  All existing PlanEditorTests, WeekTests, HomeTests, PlansTests, GuidePageTests, and ExerciseLibraryTests
  pass unchanged when plan.Tracks.Count == 1.

Multi-Track screen coverage (Ui.Tests & Core.Tests):
  PlanEditorTests: Track selector order & Main hint, Add Track (Weekly & Daily), Move up/down & daily-first refusal,
                   split Plan/Track settings persistence, per-Track Phases & Sessions, cross-Track ProblemTab naming
  WeekRowsTests & WeekTests: "Every day" rows above week grid, never Missed, no Move, excluded from suggested draft,
                             Start / Continue / View summary, extra weekly Track day labels
  HomeTests & PlansTests: Daily Track cards (NotStarted, InProgress, Done, OnBreak), extra weekly Track week lines,
                          Track-qualified Nudges, Plans "2 Tracks" summary & active Plan header label
  GuideHistoryViewTests, GuidePageTests & ExerciseLibraryTests: multi-Track Session labels & group headings
```

## Merge Danger

**Door:** Two-way

Pure UI and Core view-builder additions over the v2 Track model and storage format already landed in A4.5 PR 1 (PR #27). No schema, storage layout, or record-format version changes. Single-Track Plans render and behave identically to before.

**Blast Radius:** Low

Scoped to `PlanEditor` (`PlanEditState`, `PlanSettingsForm`, `PlanSettingsTab`, `PlanPhasesTab`, `PlanSessionsTab`), `Week` (`WeekRows`, `WeekEditRules`), `Home`, `Plans`, and `GuidePage` (`GuideHistoryView`).

## Summary

B3 replaces the Home placeholder with the real Home page, and adds the Custom workout draft page it links to. Home reads one Core value, `HomeView`; the page only renders it and starts things.

```text
Home (src/Ui/Pages/Home.razor)
  HomeViewBuilder.Build(snapshot, now, today, hour, dismissed, lastBackup) -> HomeView   (Core, pure)
  <TodayCard>            Start | Continue | Done today | Rest day | On a Break | no Plan | Plan finished | not started
  alternatives           Do a different Session | Build a Custom workout | Log a past Workout
  <WeekStrip> <WeekLine>
  <OrbRow>               Streak, Phase, Race
  <NudgeCard>*           max 3, dismissible for the week
  <RecentPrsPanel> <WeightPanel>
  sheets                 DifferentSession, LogPastWorkout, LogWeight
/custom, /custom/makeup/{SessionId}   Custom workout draft page
```

```text
src/Core/Rules/Home/   TodayCard, week strip and line, orbs, Nudges, Recent PRs, Weight panel, HomeViewBuilder
src/Core/Rules/Starting/   Custom workout creation (rows from picked Exercises, Make up from a Missed session)
src/Ui/Components/     TodayCard, WeekStrip, WeekLine, OrbRow, NudgeCard, RecentPrsPanel, WeightPanel + 3 sheets
src/Ui/Pages/          Home, CustomWorkoutDraft
src/Ui/Preferences/    NudgeDismissals (week-keyed, stored through IDevicePreferences)
src/Ui/Saving/         ReadOnlyWording (one read-only message for Log, Home and Custom workout)
```

Non-obvious decisions:
- **Week-keyed Nudge dismissals.** The key is `yyyy-MM-dd|Kind|subject`, so a dismissed Nudge returns next week. Stored in device preferences, not in the exchange format.
- **The strip counts finished Workouts only.** An in-progress Workout does not turn a tile Done.
- **No alternatives on some cards.** "Do a different Session" and friends are hidden on Break, in-progress, no Plan, Plan finished and not-started.
- **Done today means dated today.** A Logged afterwards Workout for an earlier day does not make the card Done today.
- **Read-only.** The two sheet-opening alternatives are disabled with the reason shown; Log weight still opens its sheet, which explains itself.
- **Done summary** opens the Workout the card describes (the card carries its id).
- **Phase orb caption** reads "week 5 of 8".
- **Desktop.** Home's column widens to 960 px from 900 px (`:has(.fa-home)` rule and a `--column-wide` token). Other pages keep the 560 px cap. Week line wording ("This week 1 of 5 · target 5") is unchanged: "of N" is Sessions scheduled this week.
- **Workflow trial.** Tickets 01 to 05 were implemented by the Antigravity implementer; it hit its usage limit, so 06 to 14 were done by the planner. The trial files were added then reverted inside this branch (net zero in the diff); the workflow files are not part of this change.

## Evidence

- **Before:** Home was a placeholder; no `HomeViewBuilder`, no Custom workout page.
  **After:**
  ```text
  dotnet build   0 warnings
  Core.Tests     1168 passed
  Ui.Tests        459 passed
  Web.Tests        39 passed
  ```
- Examples of behaviour now pinned by tests:
  ```text
  Start tap twice            -> one Workout, log/{id} opens
  read-only window           -> Start, Do a different Session, Log a past Workout disabled, reason shown
  Done today + View summary  -> opens log/{the finished Workout's id}
  Log a past Workout, future day -> "That day hasn't happened yet.", nothing saved
  dismiss Nudge              -> gone; stays gone for the week; returns next week
  ```
- Reviewed with `/mattpocock-skills:code-review` (Standards and Spec) and the thermo-nuclear review; fixes applied, follow-ups in `docs/specs/b3-review-fixes.md` are built.
- Not run: browser tests. Manual phone checks are in `docs/pwa-manual-checks.md` (B3 section, 11 checks); the 900 px two-column layout is a manual check.

## Merge Danger

**Door:** two-way

Additive Core and Ui code plus one page replacement. No data model, exchange format or storage change. Nudge dismissals are new device-preference keys (`nudge-dismissed:*`) that can be ignored if the PR is reverted.

**Blast Radius:** Home and the new Custom workout routes

- Merge `docs-b2-status` (PR 22) first; it touches `docs/README.md` and `docs/build-plan.md`, which this PR also edits.
- The shell gains one global CSS rule (`.column:has(.fa-home)`), active only at 900 px and wider and only on Home.
- Not yet checked on a real phone.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

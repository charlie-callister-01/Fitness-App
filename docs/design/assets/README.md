# Design assets

Every file in this folder, what it is and where it's used. All files are original work for this project unless noted. The one exception is the font Jersey 15 (SIL Open Font License 1.1; see `fonts/OFL-Jersey15.txt`). No Jagex, HYROX or other third-party art, icons or fonts are included.

Section and component numbers (S-xx, C-xx, F-xx) refer to `../design-document.md`.

## Core

| File | What | Used by | Size |
|---|---|---|---|
| `tokens.json` | All design tokens: colour and shadow per Theme × Mode, fonts, type, spacing, radius, sizes, borders, opacity, per-Theme display settings | §4; build tooling | 11 KB |
| `tokens.css` | The tokens as CSS custom properties scoped by `data-theme` / `data-mode` | every component; §4 | 7 KB |
| `components.css` | Plain CSS for components C-01 to C-12 (`fa-` classes) and the self-hosted font | §5 | 12 KB |
| `prototype-log.html` | Self-contained clickable Workout log (no build step, works offline, all four Theme/Mode sets) | S-02, §8 | 106 KB |

## Fonts

| File | What | Used by | Size |
|---|---|---|---|
| `fonts/OFL-Jersey15.txt` | SIL Open Font License 1.1 for Jersey 15 | §4.4; components.css | 4 KB |
| `fonts/jersey-15-latin-400-normal.woff2` | Jersey 15 regular, Latin subset (Keep display and label face) | §4.4; components.css | 16 KB |

## Icons (24 × 24, currentColor)

| File | What | Used by | Size |
|---|---|---|---|
| `icons/icon-add-exercise.svg` | Add an extra Exercise | §4.11 | 8 KB |
| `icons/icon-back.svg` | Back / close a pushed page | §4.11 | 8 KB |
| `icons/icon-backup.svg` | Backup (chest) | §4.11 | 8 KB |
| `icons/icon-badge.svg` | Badge (rosette) | §4.11 | 8 KB |
| `icons/icon-benchmark.svg` | Benchmark exercise (banner) | §4.11 | 8 KB |
| `icons/icon-break.svg` | Break (banked campfire) | §4.11 | 8 KB |
| `icons/icon-calendar.svg` | Calendar / date | §4.11 | 8 KB |
| `icons/icon-check.svg` | Done / confirmed Set | §4.11 | 8 KB |
| `icons/icon-close.svg` | Close / dismiss | §4.11 | 8 KB |
| `icons/icon-copy.svg` | Copy to clipboard | §4.11 | 8 KB |
| `icons/icon-delete.svg` | Delete | §4.11 | 8 KB |
| `icons/icon-download.svg` | Download file | §4.11 | 8 KB |
| `icons/icon-edit.svg` | Edit | §4.11 | 8 KB |
| `icons/icon-export.svg` | Export | §4.11 | 8 KB |
| `icons/icon-filter.svg` | Filter by tag | §4.11 | 8 KB |
| `icons/icon-guide.svg` | Guide (open book) | §4.11 | 8 KB |
| `icons/icon-home.svg` | Tab: Home (keep) | §4.11 | 8 KB |
| `icons/icon-import.svg` | Import | §4.11 | 8 KB |
| `icons/icon-info.svg` | Info / why | §4.11 | 8 KB |
| `icons/icon-install.svg` | Install to home screen | §4.11 | 8 KB |
| `icons/icon-lock.svg` | Locked (Badge) | §4.11 | 8 KB |
| `icons/icon-logged-afterwards.svg` | Logged afterwards (back-dated) | §4.11 | 8 KB |
| `icons/icon-max-set.svg` | Max set (axe) | §4.11 | 8 KB |
| `icons/icon-menu-more.svg` | Exercise menu / more actions | §4.11 | 8 KB |
| `icons/icon-minus.svg` | Decrease | §4.11 | 8 KB |
| `icons/icon-missed.svg` | Missed session (dashed ring) | §4.11 | 8 KB |
| `icons/icon-more.svg` | Tab: More | §4.11 | 8 KB |
| `icons/icon-note.svg` | My notes | §4.11 | 8 KB |
| `icons/icon-nudge.svg` | Nudge (speech scroll) | §4.11 | 8 KB |
| `icons/icon-offline.svg` | Offline (needs a connection) | §4.11 | 8 KB |
| `icons/icon-pain.svg` | Pain (cross) | §4.11 | 8 KB |
| `icons/icon-partial.svg` | Partial workout (half disc) | §4.11 | 8 KB |
| `icons/icon-phase.svg` | Phase (castle tower) | §4.11 | 8 KB |
| `icons/icon-plus.svg` | Add | §4.11 | 8 KB |
| `icons/icon-pr.svg` | PR (shield with peak) | §4.11 | 8 KB |
| `icons/icon-progress.svg` | Tab: Progress | §4.11 | 8 KB |
| `icons/icon-race.svg` | Race countdown (flag) | §4.11 | 8 KB |
| `icons/icon-reorder.svg` | Drag to reorder | §4.11 | 8 KB |
| `icons/icon-search.svg` | Search | §4.11 | 8 KB |
| `icons/icon-settings.svg` | Settings (cog) | §4.11 | 8 KB |
| `icons/icon-share.svg` | Share sheet | §4.11 | 8 KB |
| `icons/icon-skip.svg` | Skip an Exercise (Skipped state) | §4.11 | 8 KB |
| `icons/icon-storage.svg` | Persistent storage | §4.11 | 8 KB |
| `icons/icon-streak.svg` | Streak (torch flame) | §4.11 | 8 KB |
| `icons/icon-swap.svg` | Swap an Exercise (Swapped state) | §4.11 | 8 KB |
| `icons/icon-timer.svg` | Rest timer (hourglass) | §4.11 | 8 KB |
| `icons/icon-trophies.svg` | Tab: Trophies | §4.11 | 8 KB |
| `icons/icon-undo.svg` | Undo (draft item) | §4.11 | 8 KB |
| `icons/icon-video.svg` | Video link | §4.11 | 8 KB |
| `icons/icon-warning.svg` | Format warning | §4.11 | 8 KB |
| `icons/icon-week.svg` | Tab: Week | §4.11 | 8 KB |
| `icons/icon-weight.svg` | Weigh-in (scales) | §4.11 | 8 KB |

## Badge art

| File | What | Used by | Size |
|---|---|---|---|
| `badges/badge-back-after-break-locked.svg` | Back after a Break Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-back-after-break.svg` | Back after a Break Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-best-week-locked.svg` | Best week Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-best-week.svg` | Best week Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-carry-1km-locked.svg` | Carry 1 km Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-carry-1km.svg` | Carry 1 km Badge, unlocked | C-10, S-13 | 8 KB |
| `badges/badge-first-benchmark-locked.svg` | First benchmark test Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-first-benchmark.svg` | First benchmark test Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-first-max-set-locked.svg` | First Max set Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-first-max-set.svg` | First Max set Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-frame-template.svg` | Blank Badge frame for custom Badges (glyph area and colour rules in a comment) | C-22, S-13 | 9 KB |
| `badges/badge-phase-complete-locked.svg` | Phase complete Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-phase-complete.svg` | Phase complete Badge, unlocked | C-10, S-13 | 8 KB |
| `badges/badge-prs-10-locked.svg` | 10 PRs Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-prs-10.svg` | 10 PRs Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-prs-25-locked.svg` | 25 PRs Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-prs-25.svg` | 25 PRs Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-prs-50-locked.svg` | 50 PRs Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-prs-50.svg` | 50 PRs Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-run-50km-locked.svg` | Run 50 km Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-run-50km.svg` | Run 50 km Badge, unlocked | C-10, S-13 | 8 KB |
| `badges/badge-streak-12-locked.svg` | 12-week Streak Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-streak-12.svg` | 12-week Streak Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-streak-2-locked.svg` | 2-week Streak Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-streak-2.svg` | 2-week Streak Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-streak-4-locked.svg` | 4-week Streak Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-streak-4.svg` | 4-week Streak Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-streak-8-locked.svg` | 8-week Streak Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-streak-8.svg` | 8-week Streak Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-weight-goal-locked.svg` | Weight goal Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-weight-goal.svg` | Weight goal Badge, unlocked | C-10, S-13 | 8 KB |
| `badges/badge-workouts-1-locked.svg` | First Workout Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-1.svg` | First Workout Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-10-locked.svg` | 10 Workouts Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-10.svg` | 10 Workouts Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-100-locked.svg` | 100 Workouts Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-100.svg` | 100 Workouts Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-25-locked.svg` | 25 Workouts Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-25.svg` | 25 Workouts Badge, unlocked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-50-locked.svg` | 50 Workouts Badge, locked | C-10, S-13 | 9 KB |
| `badges/badge-workouts-50.svg` | 50 Workouts Badge, unlocked | C-10, S-13 | 9 KB |

## Celebration and state graphics

| File | What | Used by | Size |
|---|---|---|---|
| `graphics/pr-marker.svg` | PR shield for the Set row state cell | C-03 | 8 KB |
| `graphics/race-countdown.svg` | Race countdown card | S-01 desktop, C-11 | 9 KB |
| `graphics/stone-texture.svg` | Keep stone grain tile (optional) | §4.3 | 8 KB |
| `graphics/streak-mark-break.svg` | Streak torch banked during a Break | C-11, S-01 Break | 8 KB |
| `graphics/streak-mark.svg` | Streak torch | C-11 | 8 KB |
| `graphics/tapestry-border.svg` | Keep zigzag band tile | C-09, C-23 | 8 KB |
| `graphics/unlock-toast.svg` | Badge unlock toast (static; motion note inside) | C-23 | 9 KB |

## Chart samples

| File | What | Used by | Size |
|---|---|---|---|
| `charts/chart-heatmap.svg` | Calendar heatmap with Phase strip and every day state | C-18, S-07 | 17 KB |
| `charts/chart-line.svg` | Line chart sample: weekly best, PR points, best point, Phase bands, gap | C-17, S-07 | 12 KB |
| `charts/chart-progress-bar.svg` | Progress bar samples | C-19, S-07, S-13 | 9 KB |
| `charts/chart-weight.svg` | Weight chart sample: weigh-ins, 7-entry rolling average, goal line | C-17, S-08 | 12 KB |

## Illustrations / empty states

| File | What | Used by | Size |
|---|---|---|---|
| `illustrations/empty-first-run.svg` | First run (gatehouse) | C-21 | 8 KB |
| `illustrations/empty-no-history.svg` | No history yet (empty slot grid) | C-21 | 9 KB |
| `illustrations/empty-no-plan.svg` | No Plan / no Guide (blank scroll) | C-21 | 8 KB |
| `illustrations/empty-offline.svg` | Offline (resting pigeon) | C-21 | 8 KB |

## App icon and install assets

| File | What | Used by | Size |
|---|---|---|---|
| `app-icon/app-icon-maskable.svg` | Maskable master (80% safe zone) | PWA manifest, §11 | 9 KB |
| `app-icon/app-icon-monochrome.svg` | Monochrome mask | PWA manifest, §11 | 8 KB |
| `app-icon/app-icon.svg` | Master icon (any) | PWA manifest, §11 | 9 KB |
| `app-icon/apple-touch-icon-180.png` | iOS home screen icon | PWA manifest, §11 | 12 KB |
| `app-icon/favicon-16.png` | PNG export: fav16 | PWA manifest, §11 | 6 KB |
| `app-icon/favicon-32.png` | PNG export: fav32 | PWA manifest, §11 | 6 KB |
| `app-icon/favicon.svg` | Favicon (SVG) | PWA manifest, §11 | 8 KB |
| `app-icon/icon-any-192.png` | PNG export: any 192 | PWA manifest, §11 | 13 KB |
| `app-icon/icon-any-512.png` | PNG export: any 512 | PWA manifest, §11 | 24 KB |
| `app-icon/icon-maskable-192.png` | PNG export: maskable 192 | PWA manifest, §11 | 12 KB |
| `app-icon/icon-maskable-512.png` | PNG export: maskable 512 | PWA manifest, §11 | 23 KB |
| `app-icon/icon-monochrome-512.png` | PNG export: monochrome 512 | PWA manifest, §11 | 11 KB |
| `app-icon/splash.json` | Splash background and theme colours | PWA manifest, §11 | 1 KB |

## Flow diagrams

| File | What | Used by | Size |
|---|---|---|---|
| `flows/F-01-log-workout.svg` | F-01 Log a Workout: 10 steps | F-01 (§7) | 20 KB |
| `flows/F-02-make-up-missed.svg` | F-02 Make up a Missed session: 5 steps | F-02 (§7) | 14 KB |
| `flows/F-03-different-session.svg` | F-03 Do a different Session today: 5 steps | F-03 (§7) | 13 KB |
| `flows/F-04-edit-plan.svg` | F-04 Edit the Plan: 6 steps | F-04 (§7) | 15 KB |
| `flows/F-05-import-plan.svg` | F-05 Import a Plan an LLM wrote: 7 steps | F-05 (§7) | 16 KB |
| `flows/F-06-export-llm.svg` | F-06 Export for an LLM: 5 steps | F-06 (§7) | 14 KB |
| `flows/F-07-weekly-review.svg` | F-07 Weekly review (Sunday): 5 steps | F-07 (§7) | 13 KB |
| `flows/F-08-break-rearrange.svg` | F-08 Break and re-arrange the week: 5 steps | F-08 (§7) | 14 KB |
| `flows/F-09-edit-guide.svg` | F-09 Edit a Guide and add notes: 5 steps | F-09 (§7) | 14 KB |
| `flows/F-10-restore-backup.svg` | F-10 Restore a Backup after storage was cleared: 5 steps | F-10 (§7) | 13 KB |

## Screen mockups (PNG, 2x; 390 × 844 unless desktop/360)

| File | What | Used by | Size |
|---|---|---|---|
| `mockups/S-01-home-break-dark.png` | home break dark | S-01 (§6) | 100 KB |
| `mockups/S-01-home-clean-dark.png` | home clean dark | S-01 (§6) | 64 KB |
| `mockups/S-01-home-clean-light.png` | home clean light | S-01 (§6) | 63 KB |
| `mockups/S-01-home-dark.png` | home dark | S-01 (§6) | 109 KB |
| `mockups/S-01-home-desktop.png` | home desktop | S-01 (§6) | 219 KB |
| `mockups/S-01-home-done-dark.png` | home done dark | S-01 (§6) | 100 KB |
| `mockups/S-01-home-inprogress-dark.png` | home inprogress dark | S-01 (§6) | 109 KB |
| `mockups/S-01-home-light.png` | home light | S-01 (§6) | 50 KB |
| `mockups/S-01-home-planfinished-dark.png` | home planfinished dark | S-01 (§6) | 104 KB |
| `mockups/S-01-home-restday-dark.png` | home restday dark | S-01 (§6) | 105 KB |
| `mockups/S-02-log-360-dark.png` | log 360 dark | S-02 (§6) | 68 KB |
| `mockups/S-02-log-afterwards-dark.png` | log afterwards dark | S-02 (§6) | 106 KB |
| `mockups/S-02-log-bodyweight-dark.png` | log bodyweight dark | S-02 (§6) | 117 KB |
| `mockups/S-02-log-carry-dark.png` | log carry dark | S-02 (§6) | 114 KB |
| `mockups/S-02-log-clean-dark.png` | log clean dark | S-02 (§6) | 42 KB |
| `mockups/S-02-log-clean-light.png` | log clean light | S-02 (§6) | 41 KB |
| `mockups/S-02-log-dark.png` | log dark | S-02 (§6) | 116 KB |
| `mockups/S-02-log-desktop.png` | log desktop | S-02 (§6) | 186 KB |
| `mockups/S-02-log-distance-time-dark.png` | log distance time dark | S-02 (§6) | 115 KB |
| `mockups/S-02-log-error-dark.png` | log error dark | S-02 (§6) | 109 KB |
| `mockups/S-02-log-exercise-done-dark.png` | log exercise done dark | S-02 (§6) | 91 KB |
| `mockups/S-02-log-exercise-menu-dark.png` | log exercise menu dark | S-02 (§6) | 40 KB |
| `mockups/S-02-log-finish-partial-dark.png` | log finish partial dark | S-02 (§6) | 44 KB |
| `mockups/S-02-log-light.png` | log light | S-02 (§6) | 33 KB |
| `mockups/S-02-log-pain-check-dark.png` | log pain check dark | S-02 (§6) | 40 KB |
| `mockups/S-02-log-pain-check-yes-dark.png` | log pain check yes dark | S-02 (§6) | 32 KB |
| `mockups/S-02-log-pr-dark.png` | log pr dark | S-02 (§6) | 100 KB |
| `mockups/S-02-log-rest-timer-dark.png` | log rest timer dark | S-02 (§6) | 108 KB |
| `mockups/S-02-log-resumed-dark.png` | log resumed dark | S-02 (§6) | 106 KB |
| `mockups/S-02-log-seconds-dark.png` | log seconds dark | S-02 (§6) | 106 KB |
| `mockups/S-02-log-skip-sheet-dark.png` | log skip sheet dark | S-02 (§6) | 47 KB |
| `mockups/S-02-log-skipped-dark.png` | log skipped dark | S-02 (§6) | 81 KB |
| `mockups/S-02-log-summary-dark.png` | log summary dark | S-02 (§6) | 76 KB |
| `mockups/S-02-log-swap-picker-dark.png` | log swap picker dark | S-02 (§6) | 34 KB |
| `mockups/S-02-log-swapped-dark.png` | log swapped dark | S-02 (§6) | 116 KB |
| `mockups/S-02-log-time-dark.png` | log time dark | S-02 (§6) | 92 KB |
| `mockups/S-02-log-workout-menu-dark.png` | log workout menu dark | S-02 (§6) | 35 KB |
| `mockups/S-03-guide-dark.png` | guide dark | S-03 (§6) | 137 KB |
| `mockups/S-03-guide-edit-dark.png` | guide edit dark | S-03 (§6) | 98 KB |
| `mockups/S-03-guide-light.png` | guide light | S-03 (§6) | 50 KB |
| `mockups/S-03-guide-noguide-dark.png` | guide noguide dark | S-03 (§6) | 82 KB |
| `mockups/S-04-library-add-dark.png` | library add dark | S-04 (§6) | 115 KB |
| `mockups/S-04-library-dark.png` | library dark | S-04 (§6) | 94 KB |
| `mockups/S-04-library-light.png` | library light | S-04 (§6) | 32 KB |
| `mockups/S-05-plan-editor-dark.png` | plan editor dark | S-05 (§6) | 104 KB |
| `mockups/S-05-plan-editor-desktop.png` | plan editor desktop | S-05 (§6) | 225 KB |
| `mockups/S-05-plan-editor-light.png` | plan editor light | S-05 (§6) | 42 KB |
| `mockups/S-05-plan-editor-phases-dark.png` | plan editor phases dark | S-05 (§6) | 91 KB |
| `mockups/S-05-plans-dark.png` | plans dark | S-05 (§6) | 92 KB |
| `mockups/S-05-plans-light.png` | plans light | S-05 (§6) | 30 KB |
| `mockups/S-06-week-break-dark.png` | week break dark | S-06 (§6) | 80 KB |
| `mockups/S-06-week-dark.png` | week dark | S-06 (§6) | 71 KB |
| `mockups/S-06-week-draft-dark.png` | week draft dark | S-06 (§6) | 82 KB |
| `mockups/S-06-week-light.png` | week light | S-06 (§6) | 30 KB |
| `mockups/S-07-benchmarks-dark.png` | benchmarks dark | S-07 (§6) | 94 KB |
| `mockups/S-07-calendar-dark.png` | calendar dark | S-07 (§6) | 86 KB |
| `mockups/S-07-calendar-light.png` | calendar light | S-07 (§6) | 27 KB |
| `mockups/S-07-progress-dark.png` | progress dark | S-07 (§6) | 109 KB |
| `mockups/S-07-progress-desktop.png` | progress desktop | S-07 (§6) | 145 KB |
| `mockups/S-07-progress-light.png` | progress light | S-07 (§6) | 37 KB |
| `mockups/S-08-weight-dark.png` | weight dark | S-08 (§6) | 81 KB |
| `mockups/S-08-weight-light.png` | weight light | S-08 (§6) | 31 KB |
| `mockups/S-08-weight-log-dark.png` | weight log dark | S-08 (§6) | 33 KB |
| `mockups/S-09-weekly-review-dark.png` | weekly review dark | S-09 (§6) | 101 KB |
| `mockups/S-09-weekly-review-light.png` | weekly review light | S-09 (§6) | 36 KB |
| `mockups/S-10-export-dark.png` | export dark | S-10 (§6) | 120 KB |
| `mockups/S-10-export-light.png` | export light | S-10 (§6) | 39 KB |
| `mockups/S-10-import-dark.png` | import dark | S-10 (§6) | 83 KB |
| `mockups/S-10-import-error-dark.png` | import error dark | S-10 (§6) | 86 KB |
| `mockups/S-10-import-guides-dark.png` | import guides dark | S-10 (§6) | 100 KB |
| `mockups/S-10-import-preview-dark.png` | import preview dark | S-10 (§6) | 89 KB |
| `mockups/S-10-import-preview-light.png` | import preview light | S-10 (§6) | 36 KB |
| `mockups/S-10-prompt-pack-dark.png` | prompt pack dark | S-10 (§6) | 85 KB |
| `mockups/S-11-settings-dark.png` | settings dark | S-11 (§6) | 119 KB |
| `mockups/S-11-settings-light.png` | settings light | S-11 (§6) | 41 KB |
| `mockups/S-12-first-run-dark.png` | first run dark | S-12 (§6) | 124 KB |
| `mockups/S-12-first-run-install-dark.png` | first run install dark | S-12 (§6) | 93 KB |
| `mockups/S-12-first-run-light.png` | first run light | S-12 (§6) | 21 KB |
| `mockups/S-12-offline-dark.png` | offline dark | S-12 (§6) | 91 KB |
| `mockups/S-13-badge-detail-dark.png` | badge detail dark | S-13 (§6) | 106 KB |
| `mockups/S-13-badge-editor-dark.png` | badge editor dark | S-13 (§6) | 117 KB |
| `mockups/S-13-badges-dark.png` | badges dark | S-13 (§6) | 85 KB |
| `mockups/S-13-badges-empty-dark.png` | badges empty dark | S-13 (§6) | 84 KB |
| `mockups/S-13-badges-light.png` | badges light | S-13 (§6) | 33 KB |
| `mockups/S-13-bests-dark.png` | bests dark | S-13 (§6) | 93 KB |

Totals: 222 files, 8.3 MB in all. What ships in the app (everything except mockups, flows and the prototype) is 1098 KB, and most of that is provenance metadata the export tool adds to each SVG; strip it in the build.

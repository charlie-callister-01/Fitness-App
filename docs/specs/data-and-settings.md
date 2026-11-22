---
labels: ready-for-agent
stage: Data and settings (B9 + B10)
---

# Data and settings (B9 + B10)

Parent docs: [`../design/design-document.md`](../design/design-document.md) (C-15, C-16, S-10, S-11, S-12, F-05, F-06, F-10), [`a4-export-import.md`](a4-export-import.md), [`../adr/0005-single-writer-quarantine-and-two-storage-versions.md`](../adr/0005-single-writer-quarantine-and-two-storage-versions.md), [`../adr/0006-two-themes-keep-and-clean.md`](../adr/0006-two-themes-keep-and-clean.md), [`../adr/0007-explicit-tracks-and-format-version-2.md`](../adr/0007-explicit-tracks-and-format-version-2.md), [`../../GLOSSARY.md`](../../GLOSSARY.md).

## Problem Statement

All of the Export, Import, Prompt pack, Settings, and storage-health logic exists in Core, and the app shell already has routes for Export and Import (`/backup`) and Settings (`/settings`), but both pages are still placeholders and there is no First run experience on Home (`/`). Until these screens are built:

- The owner cannot copy an Export to an LLM, download a Backup, restore a Backup after storage is cleared, or import a Plan or Guides written by an LLM.
- The owner cannot change units, weight increment, rest timer default, goal weight, Streak rules, Theme, or Mode from Settings, nor see storage health, persistence status, or last Backup date.
- A brand-new install (or cleared browser storage) lands on an empty Home screen with no guided path to import a Plan, restore a Backup, start with an empty Plan, install the PWA, or request persistent storage.
- Entry points on Weekly review ("Export for the LLM"), the Plans list ("Import a Plan"), and an empty Guide ("Import Guides") have nowhere to link.

## Solution

Deliver the three remaining data, configuration, and onboarding surfaces over the existing Core rules, `AppData`, `GuardedSave`, `PageContext`, `ThemeService`, `IStorageEnvironment`, and `IDevicePreferences`:

1. **Export, Import, Backup, and Prompt pack (S-10 at `/backup`):** Three tabs (**Export**, **Import**, **Prompts**) with query-string deep links from Weekly review, First run, Plans, and Guide. Export supports date range, include toggles (with Health data off by default for LLM exports), Markdown/JSON format, Copy/File/Share delivery, and one-tap **Export everything (Backup)**. Prompt pack presents the three copyable prompts (Analyse my training, Write me a Plan, Write Guides). Import puts a large paste box first, file picker second, runs `ImportInput.Inspect` and `ImportPreviewer.Preview`, renders the `ImportPreview` (C-16) diff list with per-Exercise match decisions and per-Guide keep/replace choices, blocks safely with field-level errors on bad input, downloads an automatic pre-import Backup before applying, and commits atomically via `ImportApplier`.
2. **Settings (S-11 at `/settings`):** Instant-save controls (via `GuardedSave`, disabled via `ReadOnlyWell` when read-only) for Look (Theme Keep/Clean, Mode System/Dark/Light, with live primary-button preview), Units and training (weight unit, distance unit, weight increment, default rest timer, optional goal weight), Streak rules (minimum Workouts per week, whether Custom workouts count), and Data (last backed up date + Back up shortcut, persistent storage status + request button, storage usage/quota, and PWA install status).
3. **First run and install (S-12 on `/`):** When storage has no Plans and no history (Workouts or Weigh-ins), Home shows the Welcome state ("Welcome to the Keep", local-only note, **Import my Plan**, **Restore a Backup**, and **Start with an empty Plan** which creates a minimal active Plan and opens it in the Plan editor). Once a Plan or Backup is in place, Home shows a dismissible **Two quick things** card (install to home screen, persistent storage request with plain explanation, and Backup reminder) until dismissed in device preferences.

## User Stories

### Export and Backup (S-10)
1. As the owner, I want Export, Import, and Prompt pack organised into three tabs on `/backup` (Export, Import, Prompts), so that each task fits cleanly on a 360 px phone screen.
2. As the owner, I want Export to default to the last 12 weeks, with Workouts, Plan, and Exercises ticked on, Guides off, and Health data (Weigh-ins and Pain checks) off, so that a quick LLM export is safe by default.
3. As the owner, I want to pick any custom start and end date (with inline validation if start is after end) and toggle between Markdown and JSON, so that I can tailor any Export.
4. As the owner, I want to see a one-line summary of what the Export contains (counts and date range) before copying or downloading, so that I can verify what is leaving the app.
5. As the owner, I want to deliver an Export by copying to the clipboard, downloading a file with the suggested filename, or using the device Share sheet when the browser supports it, so that moving data to an LLM or cloud drive takes one tap.
6. As the owner, I want a dedicated **Export everything (Backup)** action showing my last-backed-up date that exports all data as JSON, downloads it, and records the Backup timestamp (resetting the Backup Nudge), so that backing up is unmistakable.
7. As the owner, I want tapping "Export for the LLM" on Weekly review to open Export prefilled to the past 7 days, Markdown format, Workouts and Plan on, and Health data off, so that Sunday review exports take one tap.

### Prompt pack (S-10)
8. As the owner, I want three ready-made prompt cards (**Analyse my training**, **Write me a Plan**, **Write Guides**) with short plain-language explanations and a **Copy prompt** button each, so that I can start an LLM chat without remembering the format.
9. As the owner, I want the **Write Guides** prompt card to let me include Exercises without a Guide by default (or pick specific Exercises), so that the LLM writes only the Guides I need.
10. As the owner, I want clear confirmation when a prompt is copied to the clipboard, so that I know it is ready to paste.

### Import and Preview (S-10, C-16)
11. As the owner, I want Import to show a large paste box first and a **Check** button, with a file picker underneath and a note that an automatic Backup is made before applying, so that pasting from an LLM chat on a phone is effortless.
12. As the owner, I want extra text or markdown fences around pasted JSON to be stripped cleanly with a short notice saying extra text was ignored, so that I never have to trim an LLM reply by hand.
13. As the owner, I want the app to suggest the detected Import kind (Backup, Plan, Merge, or Guides) and let me switch it before or during preview, so that a wrong guess costs nothing.
14. As the owner, I want invalid or incompatible files refused with nothing changed and a concise banner listing the exact field-level errors (or "update the app" for a newer format version), so that I can fix the file or ask the LLM to.
15. As the owner, I want `ImportPreview` (C-16) to show a plain-language summary and colour-coded diff rows (add, change old → new, needs a decision), plus any warnings (such as duplicate-date Workout warnings) and ignored sections, so that I know exactly what will change.
16. As the owner, I want near-match Exercises ("Did you mean Goblet squat?") to require my explicit choice (**Yes** to match or **Keep as new**) and unknown Exercises to be clearly shown as **Add**, with the preview updating live as I decide, so that history is never merged into the wrong Exercise.
17. As the owner, I want Guide imports to let me choose **Keep mine** or **Replace** for each existing Guide (while automatically adding new Guides) and never overwrite my personal notes, so that my own notes stay safe.
18. As the owner, I want Backup restores to state clearly how much current data will be replaced before I tap **Apply**, so that I never overwrite newer training by accident.
19. As the owner, I want tapping **Apply** to first download an automatic pre-import Backup JSON file and abort with nothing changed if that download fails, so that every Import has a safety net.
20. As the owner, I want **Apply** to disable cleanly when the app is read-only (via `ReadOnlyWell`), re-check if data changed since the preview was built, and show a clear confirmation after committing, so that concurrent windows or stale previews cannot corrupt data.
21. As the owner, I want deep links from **Import a Plan** (Plans list), **Import Guides** (no-Guide state), and **Restore a Backup** / **Import my Plan** (First run) to open `/backup` on the Import tab with the expected kind pre-selected, so that every entry point flows smoothly.

### Settings (S-11)
22. As the owner, I want a **Look** section with Theme (Keep / Clean), Mode (System / Dark / Light), and a live preview of a primary button that updates immediately when changed, so that I can see the look as I pick it.
23. As the owner, I want a **Units and training** section to set display weight unit (kg / lb), display distance unit (km / miles), weight increment, default rest timer seconds, and optional goal weight (converted to/from canonical kg in the displayed unit), so that the whole app matches my gym's plates and preferences.
24. As the owner, I want a **Streak rules** section to set the minimum counted Workouts per week and whether Custom workouts count toward the Streak, so that my Streak reflects my rules.
25. As the owner, I want every setting change to save immediately via `GuardedSave` (and be disabled with `ReadOnlyWell` when read-only), so that there is no extra Save button to forget.
26. As the owner, I want a **Data** section showing my last-backed-up date with a **Back up** button, persistent storage status with a button to request persistence if not granted, storage usage/quota if available, and PWA install status with an install trigger when available, so that I can check data safety in one place.

### First run and install (S-12)
27. As the owner, when the app opens with empty storage (no Plans and no Workouts or Weigh-ins), I want Home (`/`) to show the Welcome screen ("Welcome to the Keep", local-only data note) with **Import my Plan**, **Restore a Backup**, and **Start with an empty Plan**, so that I know how to begin.
28. As the owner, I want **Start with an empty Plan** to create a minimal active Plan (with one weekly Track) and open it directly in the Plan editor, so that I can start adding Phases and Sessions immediately.
29. As the owner, once I have a Plan or restored Backup, I want a dismissible **Two quick things** card on Home prompting me to install to home screen (if not standalone), grant persistent storage (if not granted, with a plain explanation), and remember Backups, so that browser eviction is prevented early without blocking me from training.

## Implementation Decisions

- **Browser capabilities seam (`IBrowserCapabilities`):** Introduce one narrow interface in the UI layer covering only browser-owned operations: clipboard write, file download, Web Share availability and invocation, standalone PWA detection, and `beforeinstallprompt` availability/prompting. Implement it in the Web host over JS interop and with a recording fake in UI tests.
- **Pre-import automatic Backup sink:** Implement `IBackupSink` over the browser capabilities download action so `ImportApplier.ApplyAsync` triggers a JSON file download of the pre-import Backup and aborts without mutating storage if the download call fails.
- **Export, Import, and Prompt pack page (`/backup`):** Replace the stub with a tabbed view (**Export**, **Import**, **Prompts**) accepting query parameters for tab selection, Export preset (such as the 7-day LLM preset from Weekly review), and default Import kind (Plan, Backup, Merge, Guides). All Export, Inspect, Preview, Apply, and Prompt generation call `Core.Exchange` directly; Backup recording and Settings updates run through `AppData` and `GuardedSave`.
- **Deep-link wiring on existing pages:** Wire the existing Weekly review "Export for the LLM" action, the Plans list "Import a Plan" action, and the empty Guide "Import Guides" action to `/backup` with the corresponding query parameters.
- **Settings page (`/settings`):** Replace the stub with sections for Look (Theme, Mode, live primary-button preview), Units and training (weight unit, distance unit, increment, rest timer default, goal weight in the active display unit), Streak rules (minimum weekly Workouts, count Custom workouts), and Data (last backed up + Back up action, persistent storage status + request action, storage estimate, install status + install action). Every write uses `GuardedSave` and respects `PageContext.CanWrite` via `ReadOnlyWell`, except Theme/Mode which follow the existing `ThemeService` pattern.
- **First run on Home (`/`):** Extract a Core helper (or extend `HomeViewBuilder`) to detect the true empty state (no Plans, no Workouts, no Weigh-ins) and to build the minimal active empty Plan (one weekly Track). Home renders the Welcome view in that empty state; once a Plan or history exists, Home renders the dismissible **Two quick things** card when relevant (not standalone or persistence not granted, and not dismissed in `IDevicePreferences`).
- **Offline notice:** Keep S-12's offline note minimal and only where a connection is genuinely needed (e.g. external demo video links or Web Share if offline), since all Export, Import, Settings, and Backup download/paste flows work offline.

## Testing Decisions

- **Good tests assert external behaviour, not implementation details:** Tests render the real Razor pages through bUnit (`PageFixture` over real `AppData`, `InMemoryDataStore`, `InMemoryStorageEnvironment`, `Core.Exchange`, `ThemeService`, `IDevicePreferences` fake, `FixedClock`, and a recording `IBrowserCapabilities` fake) and interact via user clicks, inputs, and navigation, asserting rendered text, disabled states, downloaded/copied payloads, and committed `DataSnapshot` state.
- **Modules tested:**
  - `Ui.Tests`: Export tab (defaults, date-range validation, LLM preset query param, copy/download/share visibility and payloads, Export everything Backup + Backup date update), Prompts tab (all three prompt cards, copy action, Guide exercise selection), Import tab (paste + extra-text notice, file upload, invalid input field errors, kind switching, near-match Exercise decisions, Guide keep/replace decisions, Backup restore confirmation, automatic Backup failure aborts Apply, stale preview handling, read-only disabling), Settings page (Theme/Mode live preview, unit/increment/rest/goal/Streak instant saves, read-only disabling, persistence request and Back up button), and Home First run (Welcome state actions, Start with an empty Plan creating an active Plan and navigating to the Plan editor, Two quick things card visibility and dismissal).
  - `Core.Tests`: Any new pure Core helper (e.g. minimal empty Plan factory / First-run state predicate and Settings validation/conversion helpers).
  - `Web.Tests` / `Web.BrowserTests`: Minimal contract/wiring verification for the new JS interop capabilities if needed, plus manual phone checks added to `docs/pwa-manual-checks.md`.
- **Prior art:** Follow `PageFixture` and the existing page test suites in `Ui.Tests` (such as `HomeTests`, `PlansTests`, and `GuidePageTests`).

## Out of Scope

- Cloud sync, user accounts, or remote Backup storage.
- Editing or deleting existing Exercises/Plans/Sessions/Breaks (covered separately if needed).
- Push notifications (Nudges remain in-app only).
- Accessibility/contrast/responsive hardening across all screens (covered in Hardening B11).

## Further Notes

- Visual checks at 360 px across both Themes (Keep, Clean) and both Modes (Dark, Light) against the `S-10-*`, `S-11-*`, and `S-12-*` mockups should be added to `docs/pwa-manual-checks.md` alongside PWA install and persistent-storage checks on a real phone.
- Remember the Editors open item: Weekly review, Plans list, and no-Guide state have had placeholder/disabled or missing Import/Export links waiting on B9; wiring them here completes those flows.

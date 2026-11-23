# 01: Browser capabilities seam and Settings page (S-11)

**What to build:** Introduce a narrow browser-capabilities seam for clipboard copy, file download, Web Share availability/invocation, standalone PWA detection, and install prompting (with JS interop in the Web host and a recording fake in UI tests), and replace the Settings placeholder (`/settings`) with the full **Settings** page: Look (Theme, Mode, and a live preview of a primary button), Units and training (display weight unit, display distance unit, weight increment, default rest timer, and optional goal weight converted in the active display unit), Streak rules (minimum Workouts per week and whether Custom workouts count), and Data (last backed up date with a Back up shortcut, persistent storage status with a request button, storage usage/quota, and PWA install status with an install trigger). Every setting change saves immediately via `GuardedSave` and disables writes via `ReadOnlyWell` when read-only.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Browser-capabilities seam supports clipboard copy, file download, share availability/invocation, standalone detection, and install prompt availability/trigger, wired in the Web host and faked in the UI test fixture.
- [ ] Settings Look section lets the owner switch Theme (Keep / Clean) and Mode (System / Dark / Light) with a live primary-button preview updating immediately.
- [ ] Settings Units and training section lets the owner edit display weight unit, display distance unit, weight increment, default rest timer seconds, and optional goal weight (displayed and edited in the active weight unit, stored in canonical kg), validating inputs cleanly.
- [ ] Settings Streak rules section lets the owner edit minimum counted Workouts per week and whether Custom workouts count toward the Streak.
- [ ] Settings Data section shows the last-backed-up date with a **Back up** action (downloading a full Backup JSON and updating the Backup date), persistent storage status with a request button when not granted, storage usage/quota when available, and install status with an install button when available.
- [ ] All Settings writes commit immediately via `GuardedSave` and are disabled with `ReadOnlyWell` shown when the app is read-only.
- [ ] UI tests cover Settings rendering, live Theme/Mode preview, unit-aware goal weight conversion, instant saves, Back up action, persistence request, and read-only behaviour with 0 build warnings.

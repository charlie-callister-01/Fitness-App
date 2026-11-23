# 05: First run welcome and Two quick things on Home (S-12)

**What to build:** Add pure Core helpers to detect the true empty state (no Plans, no Workouts, no Weigh-ins) and to construct a minimal active empty Plan (one weekly Track). On Home (`/`), when storage is in that empty state, render the **S-12 Welcome** view ("Welcome to the Keep", local-only data note, **Import my Plan** linking to `/backup?tab=import&kind=plan`, **Restore a Backup** linking to `/backup?tab=import&kind=backup`, and **Start with an empty Plan** which creates the minimal active Plan and navigates to its Plan editor). Once a Plan or Backup is in place, render the dismissible **Two quick things** card on Home when not yet dismissed in `IDevicePreferences` and either not installed as standalone or persistent storage is not yet granted (offering install trigger, persistent storage request with a plain explanation, and Backup reminder). Add B9+B10 visual and device checks to `docs/pwa-manual-checks.md`.

**Blocked by:** 01: Browser capabilities seam and Settings page (S-11), 04: Import tab, Preview (C-16), and deep links (S-10 Import, F-05, F-10)

**Status:** ready-for-agent

- [ ] Core helper detects true empty state (no Plans, no Workouts, no Weigh-ins) and creates a valid minimal active Plan with one weekly Track.
- [ ] Home renders the **Welcome to the Keep** view in the empty state with **Import my Plan**, **Restore a Backup**, and **Start with an empty Plan**.
- [ ] Tapping **Start with an empty Plan** saves the minimal active Plan via `GuardedSave` and navigates to `/plans/{id}` (disabled with `ReadOnlyWell` when read-only).
- [ ] Once a Plan or history exists, Home renders the dismissible **Two quick things** card when not dismissed in `IDevicePreferences` and install or persistent storage is still actionable, hiding permanently once dismissed.
- [ ] `docs/pwa-manual-checks.md` includes B9+B10 visual checks (360 px, Keep/Clean, Dark/Light) and real-phone install/persistence checks.
- [ ] Core tests and UI tests cover empty-state detection, minimal Plan creation, Welcome actions, Start with an empty Plan navigation, and Two quick things visibility/dismissal with 0 build warnings.

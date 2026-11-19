# 09: Logged afterwards UI, error states, manual checks and docs

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Logged afterwards stories 74 to 79, states 80 to 86). Design: §8.6, mockup `S-02-log-afterwards-dark.png`.

**What to build:** the Logged afterwards item in the Workout menu opens a native date input to re-date the open Workout (Core re-date rule): a future date is refused with a plain message, there is no limit on how far back, and the same item undoes it. The banner reads "Logged afterwards · Sun 15 Nov. No timers."; the rest timer, elapsed time and Wake Lock indicator are hidden; no duration is recorded; PR checks use history as of that date; the summary shows "—" for duration. Then close the stage: sweep every B2 action for read-only and failed-save behaviour (disabled with the plain message, banner shown, screen unchanged), reduced-motion for sheets and toasts, and 360 px and all four Theme and Mode looks. Add a "Workout log manual checks (B2)" section to `docs/pwa-manual-checks.md` (rest timer, Wake Lock, each sheet, Pain check, summary, Logged afterwards, kill the tab mid-finish, offline), and update `docs/README.md`, `docs/build-plan.md` status and open items, and the stage handoff.

**Blocked by:** 02, 07, 08.

**Status:** ready-for-agent

- [ ] Re-date and undo work and survive reload; future date refused; banner text derives from the Workout's date
- [ ] No timer, elapsed time or Wake Lock indicator on a Logged afterwards Workout; summary duration is "—"; PR judged against history up to that date
- [ ] Every B2 action is disabled with the plain message when read-only and leaves the screen unchanged with the banner on a failed save (one test per action family)
- [ ] Reduced motion respected by sheets and toasts
- [ ] Manual checks section added; README, build plan (status, remaining open items, the now-closed B1 kg/lb item) and handoff updated
- [ ] bUnit page tests written first (red run in the report); full suite green: Core, Ui and Web

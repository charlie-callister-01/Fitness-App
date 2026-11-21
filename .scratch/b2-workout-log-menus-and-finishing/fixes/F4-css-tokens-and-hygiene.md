# F4: CSS tokens and small hygiene

Source: thermo review item 7 and Spec finding 4. Rule: `.scratch/b2-workout-log-menus-and-finishing/HANDOFF.md` (CSS and tokens rules: "A raw hex or px in component CSS is a defect"; new tokens go in `src/Ui/wwwroot/tokens.css` AND `docs/design/assets/tokens.css`, plus `tokens.json` and the design document tables, in the same change).

**What to build:**
1. Replace the raw `max-height: 90dvh` in `Sheet.razor.css` with a token (for example a sheet max-height token) defined in both `tokens.css` copies, `tokens.json` and the design document token tables.
2. Replace the two raw `text-underline-offset: 3px` (in `GuideContent.razor.css` and `WorkoutLog.razor.css`) with a token or an existing one (check for an existing offset token such as `--outline-offset` first and do not misuse an unrelated one). Check every CSS file changed in stage B2 (`git diff c2625d4...HEAD --name-only -- '*.css'`) for any other raw `px`, `dvh`, `vh` or hex and fix those too. Leave pre-existing, unchanged lines alone unless they are clearly B2's.
3. Confirm `--state-skipped` and `--state-swapped` exist in the docs copy `docs/design/assets/tokens.css` and `tokens.json` as well as the app copy; add if missing.
4. **Wake Lock reacquire:** read `src/Web/wwwroot/js/wake-lock.js` and confirm it reacquires the lock on `visibilitychange` when the page becomes visible again while the log still wants it, and releases on release; fix it if not (it is a browser-only path, so keep the change small and add a Web-level check only if one exists for this file; otherwise list it in the report for the manual phone checks, which already include it).
5. `LoggedAfterwardsFlow.BannerText` (display formatting) moves out of the flow class next to the other log view formatting (a small edit; if F2 is already changing that flow, only move the function and update its callers).

**Blocked by:** None (can start immediately). Touches different files from F1; merge conflicts are not expected apart from item 5, which is a one-function move.

**Status:** ready-for-agent

- [ ] No raw `px`, `dvh`, `vh` or hex remains in CSS added or changed by stage B2
- [ ] New tokens are present in both `tokens.css` copies, `tokens.json` and the design document tables
- [ ] `wake-lock.js` reacquire on visibility return verified (or fixed) and reported
- [ ] Banner text lives outside the flow class; its test still passes
- [ ] Full suite green; no behaviour change

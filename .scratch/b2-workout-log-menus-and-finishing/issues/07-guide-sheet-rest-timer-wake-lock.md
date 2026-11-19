# 07: Guide sheet, rest timer and Wake Lock

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (View Guide stories 32 to 36, rest timer and Wake Lock 43 to 50). Design: §8.2, §8.4, mockup `S-02-log-rest-timer-dark.png`.

**What to build:** (a) View Guide opens a sheet with the Guide content that exists today for the Exercise (read from the snapshot) or a plain no-Guide message, with the video link opening in a new tab, and **Back to the Set** closing it with typed text, the open row and the pager exactly as they were; built so B4 can swap its Guide content in without changing the sheet. (b) The rest timer: when a duration other than Off is chosen, Confirm starts it, the ConfirmBar shows "Rest 1:12" counting down, another Confirm restarts it, reaching zero stops and clears it quietly; it is computed from a start timestamp and the clock, not a tick count, and is cancelled when the page is disposed. (c) Wake Lock behind a small interface (acquire, release, supported) with a browser implementation in the Web project following the existing JS interop pattern, acquired when the log opens a Workout, reacquired on return to the tab, released on leaving, failures swallowed silently, and a no-op fake for tests and unsupported browsers. Add the new JS file to the service worker's offline cache list.

**Blocked by:** 04, 06 (page conflicts; do after Swap and Add).

**Status:** ready-for-agent

- [ ] Guide sheet shows content or the no-Guide state; Back to the Set preserves unsaved typed text and state
- [ ] Timer starts only when not Off, shows the countdown in the ConfirmBar, restarts on the next Confirm, clears at zero, and uses the fake clock in tests
- [ ] Wake Lock acquire, reacquire and release are exercised through the fake; an unsupported browser works silently
- [ ] The offline-cache coverage test stays green with the new JS file
- [ ] Manual phone checks for the timer and Wake Lock drafted for the docs ticket (list them in the report)
- [ ] bUnit page tests written first (red run in the report); all existing tests stay green; no browser test

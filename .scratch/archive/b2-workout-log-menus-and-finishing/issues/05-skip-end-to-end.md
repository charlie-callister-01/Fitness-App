# 05: Skip end to end

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Skip stories 6 to 12). Design: §8.4, mockups `S-02-log-skip-sheet-dark.png`, `S-02-log-skipped-dark.png`.

**What to build:** the owner taps Skip in the Exercise menu, a sheet offers optional reason chips (Short on time, Pain, No equipment, Other) and a plain **Skip** button, and the Exercise becomes skipped: a "—" chip in the pager, a Skipped state with its reason, its Sets hidden, and the log moves to the next Exercise that is not complete. A skipped Exercise can be un-skipped from its menu. Skipping an Exercise that already has confirmed Sets first asks and says the Sets will be removed. The view builder and pager learn skipped state; a skipped Exercise counts as handled for the primary button's Next/Finish labels.

**Blocked by:** 01, 04.

**Status:** ready-for-agent

- [ ] Skip with and without a reason saves a skipped entry through the Workout writer; the pager shows "—" and the state chip with reason
- [ ] After skipping, the page lands on the next incomplete Exercise (or the Finish action when none remain)
- [ ] Un-skip restores the Exercise as not yet done; skip with Sets asks first and Cancel changes nothing
- [ ] Choosing Pain as a reason does not create a Pain check
- [ ] Failed save shows the shared banner and leaves the screen unchanged; read-only disables Skip
- [ ] Survives reload (kill and reopen shows the skipped state)
- [ ] bUnit page tests written first (red run in the report); all existing tests stay green

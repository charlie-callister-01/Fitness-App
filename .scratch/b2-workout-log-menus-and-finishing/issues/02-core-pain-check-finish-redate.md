# 02: Core: Pain check, Finish and Partial, re-date, past-dated creation

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Core: pure Workout edits, Logged afterwards creation; decisions 1 to 4 and 7 in Further Notes).

**What to build:** the pure Core rules behind finishing a Workout. Pain check edits: answered none, reported (one or more body areas, a severity, optional Exercise and note, the optional side written as a leading phrase in the note), and cleared back to unanswered. Finish: stamps the end time; on a Partial finish it first records every planned row with no entry (or only an empty Done entry, dropping its RPE) as a skipped entry with no reason, and drops added Exercises that have no Sets; a Workout with no confirmed Set cannot be finished. Re-date as Logged afterwards: sets the flag and a past date, clears the start time so no duration is stored; undo restores today's date and the flag false; a future date is refused. A rule that creates a past-dated Workout (for a Session or as a Custom workout) with the flag set and no timers, saved through the existing atomic commit path.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Reported Pain check requires at least one area and a severity; "none" carries neither; clearing leaves the Workout unanswered (distinct from none)
- [ ] Partial finish writes explicit skipped entries (no reason) for unrecorded and empty-Done rows, leaves swapped and fully done rows alone, drops empty added Exercises, and the outcomes rule then reports the Workout as Partial
- [ ] Finish on a Workout with no confirmed Set returns a refusal, not a changed Workout
- [ ] Finish is idempotent in effect: an already finished Workout keeps its original end time
- [ ] Re-date: past and today allowed, future refused, no limit on how far back; undo restores
- [ ] Past-dated creation yields flag true, no start or end time, a copy of the Session's rows as in force that week (reusing the B1 start rule), or an empty plan for a Custom workout
- [ ] Core tests written first (show the red run in the report), including the RPE-only entry at Finish; all existing tests stay green
- [ ] No data model, exchange format or storage change

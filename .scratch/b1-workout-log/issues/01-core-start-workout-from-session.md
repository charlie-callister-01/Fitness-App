# 01: Core: start a Workout from a Session

**What to build:** A pure Core rule that creates an in-progress Workout for a Session on a date: fresh ID from the existing ID generator, the date, the Session ID, `StartedAt` = now, `EndedAt` null, `LoggedAfterwards` false, no entries, and `Planned` copied from the Session's Exercise rows as in force that week (a Phase's override for that Session when the Phase covers the week, otherwise the Plan default), reusing the existing Phase and Week rules. Plus the thinnest query over the existing Week schedule rules answering "which Session does Start offer" (due today, else next undone this week) and "is a Workout in progress" (the one with no `EndedAt`). Spec: `docs/specs/b1-workout-log-core-input.md` ("In progress means no end time", "starting a Workout from a Session").

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Creating a Workout copies the Phase-override rows when a Phase covers the week, else the Plan rows; later Plan edits do not change it
- [x] The Workout has `EndedAt` null and no entries; a helper identifies the in-progress Workout (at most one)
- [x] The "next Session to start" query reuses existing scheduling rules and adds no new scheduling logic; none when there is no active Plan or no Sessions
- [x] Core unit tests in the style of the existing Rules tests, reusing `PlanFixture`, `WorkoutBuilder`, `HistoryBuilder`

using Core.Model;
using Core.Rules.Home;
using Core.Rules.Schedule;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Home;

public class HomeTodayRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 10, 30, 0, TimeSpan.Zero);

    private static Session Lower => PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower",
        exercises: [PlanFixture.Row("squat", 3, 5), PlanFixture.Row("lunge", 3, 10)]);

    private static Session Upper => PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper",
        exercises: [PlanFixture.Row("press", 3, 5)]);

    private static Session Cardio => PlanFixture.Session("cardio", DayOfWeek.Friday, SessionType.Cardio, name: "Cardio",
        exercises: [PlanFixture.Row("run", 1, null)]);

    private static Plan MakePlan(
        DateOnly? start = null,
        IReadOnlyList<Phase>? phases = null,
        IReadOnlyList<Session>? sessions = null,
        bool active = true) =>
        PlanFixture.Make(
            start: start ?? Mon,
            phases: phases,
            sessions: sessions ?? [Lower, Upper, Cardio]) with { IsActive = active };

    private static Workout MakeInProgress(
        DateOnly date,
        string sessionId = "lower",
        DateTimeOffset? startedAt = null,
        bool hasStartTime = true,
        bool loggedAfterwards = false,
        IReadOnlyList<WorkoutEntry>? entries = null) =>
        new(
            "w-in-progress",
            date,
            sessionId,
            loggedAfterwards,
            hasStartTime ? (startedAt ?? Now.AddMinutes(-30)) : null,
            null,
            [PlanFixture.Row("squat", 3, 5), PlanFixture.Row("lunge", 3, 10)],
            entries ?? [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [new WorkoutSet("s1", 100, 5, null, null, null, false)])],
            null);

    private static Workout MakeFinished(
        DateOnly date,
        string sessionId = "lower",
        DateTimeOffset? startedAt = null,
        DateTimeOffset? endedAt = null,
        IReadOnlyList<WorkoutEntry>? entries = null,
        IReadOnlyList<SessionExercise>? planned = null) =>
        new(
            $"w-fin-{date:yyyyMMdd}-{sessionId}",
            date,
            sessionId,
            false,
            startedAt ?? new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
            endedAt ?? new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero),
            planned ?? [PlanFixture.Row("squat", 3, 5)],
            entries ?? [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [new WorkoutSet("s1", 100, 5, null, null, null, false)])],
            null);

    // =========================================================================
    // 1. Each of the 8 kinds is produced by a test, in precedence order
    // =========================================================================

    [Fact]
    public void Compute_gives_NoPlan_when_no_active_plan_exists()
    {
        var inactive = MakePlan(active: false);
        var snapshot = DataSnapshot.Empty with { Plans = [inactive] };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.NoPlan, card.Kind);
    }

    [Fact]
    public void Compute_gives_Break_when_today_is_a_break_day()
    {
        var plan = MakePlan();
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Breaks = [new Break("b1", Mon, Mon.AddDays(2), "Rest")]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.Break, card.Kind);
    }

    [Fact]
    public void Compute_gives_InProgress_with_counts_and_elapsed()
    {
        var plan = MakePlan();
        var started = Now.AddMinutes(-45);
        var inProgress = MakeInProgress(Mon, startedAt: started);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [inProgress]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.InProgress, card.Kind);
        Assert.Same(inProgress, card.InProgressWorkout);
        Assert.Same(inProgress, card.Workout);
        Assert.Equal(1, card.DoneCount); // squat is Done
        Assert.Equal(2, card.PlannedCount); // squat + lunge
        Assert.Equal(TimeSpan.FromMinutes(45), card.Elapsed);
    }

    [Fact]
    public void Compute_gives_DoneToday_with_summary_metrics()
    {
        var plan = MakePlan();
        var start = new DateTimeOffset(Mon.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
        var end = new DateTimeOffset(Mon.ToDateTime(new TimeOnly(8, 50)), TimeSpan.Zero);
        var finished = MakeFinished(Mon, startedAt: start, endedAt: end);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [finished]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.DoneToday, card.Kind);
        Assert.NotNull(card.DoneSummary);
        Assert.Equal(TimeSpan.FromMinutes(50), card.Duration);
        Assert.Equal(1, card.SetCount);
        Assert.False(card.IsPartial);
    }

    [Fact]
    public void Compute_gives_PlanFinished_when_past_last_phase()
    {
        var phase = new Phase("ph1", "Phase 1", 1, 4, [], []);
        var plan = MakePlan(phases: [phase]);
        // Week 1 starts Mon (Nov 16). Week 4 ends Dec 13. Week 5 is Dec 14.
        var today = Mon.AddDays(28);
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var card = HomeTodayRules.Compute(snapshot, Now, today);

        Assert.Equal(TodayKind.PlanFinished, card.Kind);
    }

    [Fact]
    public void Compute_gives_NotStarted_when_before_plan_start_date()
    {
        var plan = MakePlan(start: Mon);
        var today = Mon.AddDays(-3);
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var card = HomeTodayRules.Compute(snapshot, Now, today);

        Assert.Equal(TodayKind.NotStarted, card.Kind);
        Assert.Equal(Mon, card.PlanStartDate);
    }

    [Fact]
    public void Compute_gives_Start_for_session_scheduled_today()
    {
        var plan = MakePlan();
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.Start, card.Kind);
        Assert.NotNull(card.Session);
        Assert.Equal("lower", card.Session.Id);
    }

    [Fact]
    public void Compute_gives_RestDay_when_no_session_is_scheduled_today()
    {
        var plan = MakePlan();
        var tuesday = Mon.AddDays(1);
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var card = HomeTodayRules.Compute(snapshot, Now, tuesday);

        Assert.Equal(TodayKind.RestDay, card.Kind);
        Assert.NotNull(card.NextSession);
        Assert.Equal("upper", card.NextSession.Id);
        Assert.Equal(Mon.AddDays(2), card.NextDate);
    }

    // =========================================================================
    // 2. Conflict tests
    // =========================================================================

    [Fact]
    public void Conflict_Break_plus_InProgress_gives_Break()
    {
        var plan = MakePlan();
        var inProgress = MakeInProgress(Mon);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Breaks = [new Break("b1", Mon, Mon, "Break day")],
            Workouts = [inProgress]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.Break, card.Kind);
    }

    [Fact]
    public void Conflict_InProgress_plus_finished_today_gives_InProgress()
    {
        var plan = MakePlan();
        var finished = MakeFinished(Mon, "cardio",
            endedAt: new DateTimeOffset(Mon.ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero));
        var inProgress = MakeInProgress(Mon, "lower", startedAt: Now.AddMinutes(-20));
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [finished, inProgress]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.InProgress, card.Kind);
        Assert.Same(inProgress, card.InProgressWorkout);
    }

    [Fact]
    public void Conflict_finished_today_plus_another_session_due_gives_DoneToday()
    {
        // Monday: finished Lower, but another session also due or moved to today
        var plan = MakePlan();
        var finished = MakeFinished(Mon, "lower");
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [finished],
            WeekSchedules = [new WeekSchedule(Mon, [new SessionMove("upper", Mon)])]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.DoneToday, card.Kind);
    }

    [Fact]
    public void Conflict_PlanFinished_plus_InProgress_gives_InProgress()
    {
        var phase = new Phase("ph1", "Phase 1", 1, 4, [], []);
        var plan = MakePlan(phases: [phase]);
        var today = Mon.AddDays(28); // after week 4
        var inProgress = MakeInProgress(today);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [inProgress]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, today);

        Assert.Equal(TodayKind.InProgress, card.Kind);
    }

    [Fact]
    public void Conflict_NoPlan_overrides_InProgress_and_Break()
    {
        var inactive = MakePlan(active: false);
        var inProgress = MakeInProgress(Mon);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [inactive],
            Breaks = [new Break("b1", Mon, Mon, "Break")],
            Workouts = [inProgress]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.NoPlan, card.Kind);
    }

    // =========================================================================
    // 3. Moved sessions & Done sessions
    // =========================================================================

    [Fact]
    public void Session_moved_to_today_gives_Start()
    {
        var plan = MakePlan();
        // Move Upper (normally Wed) to Tuesday
        var tuesday = Mon.AddDays(1);
        var schedule = new WeekSchedule(Mon, [new SessionMove("upper", tuesday)]);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            WeekSchedules = [schedule]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, tuesday);

        Assert.Equal(TodayKind.Start, card.Kind);
        Assert.NotNull(card.Session);
        Assert.Equal("upper", card.Session.Id);
    }

    [Fact]
    public void Session_already_done_this_week_is_not_Start()
    {
        var plan = MakePlan();
        // Lower is scheduled Monday. But Lower was already logged earlier this week (e.g. earlier today).
        var done = MakeFinished(Mon, "lower");
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [done]
        };

        // DoneToday will trigger on Monday. But let's check a day where Lower is not finished today,
        // e.g. Lower was moved or logged on Monday, and on Wednesday (Upper day) Upper was already done yesterday!
        var wednesday = Mon.AddDays(2);
        var upperDoneTuesday = MakeFinished(Mon.AddDays(1), "upper");
        var snapshotWed = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [upperDoneTuesday]
        };

        // On Wednesday, Upper is scheduled today, but already done this week -> RestDay instead of Start!
        var card = HomeTodayRules.Compute(snapshotWed, Now, wednesday);

        Assert.Equal(TodayKind.RestDay, card.Kind);
        Assert.NotNull(card.NextSession);
        Assert.Equal("cardio", card.NextSession.Id);
    }

    [Fact]
    public void When_two_sessions_due_today_first_in_layout_order_is_chosen()
    {
        var plan = MakePlan();
        // Move Upper (Wed) to Monday, so both Lower and Upper are on Monday
        var schedule = new WeekSchedule(Mon, [new SessionMove("upper", Mon)]);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            WeekSchedules = [schedule]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.Start, card.Kind);
        Assert.NotNull(card.Session);
        Assert.Equal("lower", card.Session.Id); // Lower comes first in plan order on Monday
    }

    // =========================================================================
    // 4. RestDay next session resolution
    // =========================================================================

    [Fact]
    public void RestDay_finds_next_session_later_this_week()
    {
        var plan = MakePlan();
        var tuesday = Mon.AddDays(1);
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var card = HomeTodayRules.Compute(snapshot, Now, tuesday);

        Assert.Equal(TodayKind.RestDay, card.Kind);
        Assert.NotNull(card.NextSession);
        Assert.Equal("upper", card.NextSession.Id);
        Assert.Equal(Mon.AddDays(2), card.NextDate);
    }

    [Fact]
    public void RestDay_finds_first_session_of_next_week_when_none_left_this_week()
    {
        var plan = MakePlan();
        var saturday = Mon.AddDays(5);
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var card = HomeTodayRules.Compute(snapshot, Now, saturday);

        Assert.Equal(TodayKind.RestDay, card.Kind);
        Assert.NotNull(card.NextSession);
        Assert.Equal("lower", card.NextSession.Id);
        Assert.Equal(Mon.AddDays(7), card.NextDate); // Next Monday
    }

    [Fact]
    public void RestDay_has_no_next_session_when_plan_has_no_sessions()
    {
        var emptyPlan = PlanFixture.Make(start: Mon, sessions: []);
        var snapshot = DataSnapshot.Empty with { Plans = [emptyPlan] };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.RestDay, card.Kind);
        Assert.Null(card.NextSession);
        Assert.Null(card.NextDate);
    }

    // =========================================================================
    // 5. InProgress: Past-dated in-progress workout has null Elapsed
    // =========================================================================

    [Fact]
    public void Past_dated_in_progress_workout_gives_InProgress_with_null_Elapsed()
    {
        var plan = MakePlan();
        var pastWorkout = MakeInProgress(Mon.AddDays(-2), hasStartTime: false, loggedAfterwards: true);
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [pastWorkout]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.InProgress, card.Kind);
        Assert.Null(card.Elapsed);
        Assert.Same(pastWorkout, card.InProgressWorkout);
    }

    // =========================================================================
    // 6. Multiple finished workouts today: picks latest EndedAt
    // =========================================================================

    [Fact]
    public void Multiple_finished_workouts_today_selects_latest_by_EndedAt()
    {
        var plan = MakePlan();
        var earlier = MakeFinished(Mon, "lower",
            startedAt: new DateTimeOffset(Mon.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
            endedAt: new DateTimeOffset(Mon.ToDateTime(new TimeOnly(8, 45)), TimeSpan.Zero));
        var later = MakeFinished(Mon, "cardio",
            startedAt: new DateTimeOffset(Mon.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
            endedAt: new DateTimeOffset(Mon.ToDateTime(new TimeOnly(12, 30)), TimeSpan.Zero));
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [earlier, later]
        };

        var card = HomeTodayRules.Compute(snapshot, Now, Mon);

        Assert.Equal(TodayKind.DoneToday, card.Kind);
        Assert.Equal(TimeSpan.FromMinutes(30), card.Duration); // from later workout
    }

    // =========================================================================
    // 7. UndoneSessions
    // =========================================================================

    [Fact]
    public void UndoneSessions_returns_undone_and_missed_sessions_in_date_order()
    {
        var plan = MakePlan();
        // Today is Thursday. Monday's Lower is missed (no workout). Wednesday's Upper is missed (no workout).
        // Friday's Cardio is upcoming.
        var thursday = Mon.AddDays(3);
        var snapshot = DataSnapshot.Empty with { Plans = [plan] };

        var undone = HomeTodayRules.UndoneSessions(snapshot, thursday);

        Assert.Equal(3, undone.Count);
        Assert.Equal("lower", undone[0].SessionId);
        Assert.Equal(Mon, undone[0].Date);
        Assert.Equal("upper", undone[1].SessionId);
        Assert.Equal(Mon.AddDays(2), undone[1].Date);
        Assert.Equal("cardio", undone[2].SessionId);
        Assert.Equal(Mon.AddDays(4), undone[2].Date);
    }

    [Fact]
    public void UndoneSessions_excludes_sessions_with_a_workout_this_week()
    {
        var plan = MakePlan();
        var wednesday = Mon.AddDays(2);
        // Lower was completed on Monday
        var lowerDone = MakeFinished(Mon, "lower");
        var snapshot = DataSnapshot.Empty with
        {
            Plans = [plan],
            Workouts = [lowerDone]
        };

        var undone = HomeTodayRules.UndoneSessions(snapshot, wednesday);

        Assert.Equal(2, undone.Count);
        Assert.Equal("upper", undone[0].SessionId);
        Assert.Equal("cardio", undone[1].SessionId);
    }

    [Fact]
    public void UndoneSessions_returns_empty_when_no_active_plan()
    {
        var inactive = MakePlan(active: false);
        var snapshot = DataSnapshot.Empty with { Plans = [inactive] };

        var undone = HomeTodayRules.UndoneSessions(snapshot, Mon);

        Assert.Empty(undone);
    }
}

using Core.Model;
using Core.Rules.Starting;
using Core.Rules.Tracks;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using Core.Time;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Starting;

public class WorkoutStartTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly FixedClock Clock = new(Now);

    private static SessionExercise Row(string id, int sets = 3, int? reps = 8) => PlanFixture.Row(id, sets, reps);

    // Plan starts on Mon (week 1). Lower on Mon, Upper on Wed, Run on Fri.
    private static Plan Plan(IReadOnlyList<Phase>? phases = null, bool active = true) =>
        PlanFixture.Make(start: Mon, phases: phases, sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [Row("squat"), Row("lunge", 2, 10)]),
            PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises: [Row("press")]),
            PlanFixture.Session("run", DayOfWeek.Friday, SessionType.Cardio, name: "Run", exercises: [Row("run")]),
        ]) with { IsActive = active };

    private static Session Lower(Plan plan) => plan.Sessions[0];

    private static Workout Start(Plan plan, DateOnly date) =>
        WorkoutStart.Create(plan, Lower(plan), date, new SequentialIdGenerator("w"), Clock);

    // ---- Create

    [Fact]
    public void Create_makes_an_in_progress_workout_with_a_fresh_id_and_no_entries()
    {
        var workout = Start(Plan(), Mon);

        Assert.Equal("w-1", workout.Id);
        Assert.Equal(Mon, workout.Date);
        Assert.Equal("lower", workout.SessionId);
        Assert.Equal(Now, workout.StartedAt);
        Assert.Null(workout.EndedAt);
        Assert.False(workout.LoggedAfterwards);
        Assert.Empty(workout.Entries);
        Assert.Null(workout.PainCheck);
    }

    [Fact]
    public void Create_copies_the_plan_rows_when_no_phase_covers_the_week()
    {
        var plan = Plan();

        Assert.Equal(plan.Sessions[0].Exercises, Start(plan, Mon).Planned);
    }

    [Fact]
    public void Create_copies_the_phase_override_rows_when_a_phase_covers_the_week()
    {
        SessionExercise[] overrideRows = [Row("front-squat", 5, 5)];
        var phase = new Phase("ph", "Build", 1, 4, [], [new SessionOverride("lower", overrideRows)]);

        var workout = Start(Plan([phase]), Mon);

        Assert.Equal(overrideRows, workout.Planned);
    }

    [Fact]
    public void Create_uses_the_plan_rows_for_a_session_the_phase_does_not_override()
    {
        var phase = new Phase("ph", "Build", 1, 4, [], [new SessionOverride("upper", [Row("dip")])]);
        var plan = Plan([phase]);

        Assert.Equal(plan.Sessions[0].Exercises, Start(plan, Mon).Planned);
    }

    [Fact]
    public void Create_uses_the_plan_rows_when_the_phase_has_ended()
    {
        var phase = new Phase("ph", "Build", 1, 4, [], [new SessionOverride("lower", [Row("front-squat")])]);

        Assert.Equal(Plan().Sessions[0].Exercises, Start(Plan([phase]), Mon.AddDays(7 * 4)).Planned);
    }

    [Fact]
    public void Later_plan_edits_do_not_change_a_created_workout()
    {
        var plan = Plan();
        var workout = Start(plan, Mon);
        var before = workout.Planned.ToList();

        var edited = plan with { Sessions = [Lower(plan) with { Exercises = [Row("deadlift")] }] };

        Assert.Equal(before, workout.Planned);
        Assert.NotEqual(edited.Sessions[0].Exercises, workout.Planned);
    }

    // ---- InProgress

    [Fact]
    public void InProgress_finds_the_workout_with_no_end_time()
    {
        var open = Start(Plan(), Mon);
        var done = Logged(Mon.AddDays(-7), "lower") with { EndedAt = Now };

        Assert.Same(open, WorkoutStart.InProgress([done, open]));
    }

    [Fact]
    public void InProgress_is_null_when_every_workout_has_ended()
    {
        var done = Logged(Mon, "lower") with { EndedAt = Now };

        Assert.Null(WorkoutStart.InProgress([done]));
        Assert.Null(WorkoutStart.InProgress([]));
    }

    // ---- NextToStart

    private static string? NextId(Plan? plan, DateOnly today, params Workout[] workouts) =>
        WorkoutStart.NextToStart(plan, plan is null ? null : TrackRules.Main(plan), [], [], workouts, today)?.SessionId;

    [Fact]
    public void The_session_due_today_is_offered()
    {
        Assert.Equal("upper", NextId(Plan(), Mon.AddDays(2)));
    }

    [Fact]
    public void With_nothing_due_today_the_next_session_later_this_week_is_offered()
    {
        Assert.Equal("upper", NextId(Plan(), Mon.AddDays(1)));
    }

    [Fact]
    public void A_session_already_logged_this_week_is_skipped()
    {
        Assert.Equal("upper", NextId(Plan(), Mon, Logged(Mon, "lower")));
    }

    [Fact]
    public void A_workout_logged_in_another_week_does_not_count()
    {
        Assert.Equal("lower", NextId(Plan(), Mon, Logged(Mon.AddDays(-7), "lower")));
    }

    [Fact]
    public void When_only_earlier_sessions_remain_the_earliest_is_offered()
    {
        // Saturday: Run (Fri) done; Lower and Upper are missed.
        Assert.Equal("lower", NextId(Plan(), Mon.AddDays(5), Logged(Mon.AddDays(4), "run")));
    }

    [Fact]
    public void Nothing_is_offered_once_every_session_is_done_this_week()
    {
        var done = new[] { Logged(Mon, "lower"), Logged(Mon.AddDays(2), "upper"), Logged(Mon.AddDays(4), "run") };

        Assert.Null(NextId(Plan(), Mon.AddDays(4), done));
    }

    [Fact]
    public void Nothing_is_offered_without_an_active_plan_or_without_sessions()
    {
        Assert.Null(NextId(null, Mon));
        Assert.Null(NextId(Plan(active: false), Mon));
        Assert.Null(NextId(PlanFixture.Make(start: Mon, sessions: []), Mon));
    }

    [Fact]
    public void Nothing_is_offered_before_the_plan_starts()
    {
        Assert.Null(NextId(Plan(), Mon.AddDays(-7)));
    }

    // ---- Multi-Track

    [Fact]
    public void Create_and_CreatePastDated_resolve_the_Sessions_own_Track_Phase_override()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(2, 1, false),
            [new Phase("p1", "Main", 1, 4, [], [new SessionOverride("lower", [Row("deadlift", 5, 5)])])]);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(),
            [new Phase("m1", "Habit", 1, 4, [], [new SessionOverride("stretch", [Row("hip-90-90", 2, 10)])])]);
        var lower = PlanFixture.Session("lower", DayOfWeek.Monday, exercises: [Row("squat")], trackId: "main");
        var stretch = PlanFixture.Session("stretch", DefaultDay.Daily, SessionType.Mobility, exercises: [Row("stretch")], trackId: "mob");
        var plan = PlanFixture.Make(start: Mon, tracks: [main, mobility], sessions: [lower, stretch]);

        var live = WorkoutStart.Create(plan, stretch, Mon, new SequentialIdGenerator("w"), Clock);
        var past = WorkoutStart.CreatePastDated(plan, stretch, Mon, new SequentialIdGenerator("w"));

        Assert.Equal(["hip-90-90"], live.Planned.Select(r => r.ExerciseId));
        Assert.Equal(["hip-90-90"], past.Planned.Select(r => r.ExerciseId));
    }

    [Fact]
    public void NextToStart_works_per_weekly_Track_and_returns_null_for_a_daily_Track()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(2, 1, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(1, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(), []);
        var plan = PlanFixture.Make(start: Mon, tracks: [main, cardio, mobility], sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, trackId: "main"),
            PlanFixture.Session("run", DayOfWeek.Wednesday, SessionType.Cardio, trackId: "cardio"),
            PlanFixture.Session("stretch", DefaultDay.Daily, SessionType.Mobility, trackId: "mob"),
        ]);

        Assert.Equal("lower", WorkoutStart.NextToStart(plan, main, [], [], [], Mon)?.SessionId);
        Assert.Equal("run", WorkoutStart.NextToStart(plan, cardio, [], [], [], Mon)?.SessionId);
        Assert.Null(WorkoutStart.NextToStart(plan, mobility, [], [], [], Mon));
    }
}

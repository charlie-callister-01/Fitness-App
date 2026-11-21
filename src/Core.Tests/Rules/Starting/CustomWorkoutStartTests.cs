using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Schedule;
using Core.Rules.Starting;
using Core.Storage;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using Core.Tests.Storage;
using Core.Time;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Starting;

public class CustomWorkoutStartTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly FixedClock Clock = new(Now);

    private static SessionExercise Row(string id, int sets = 3, int? reps = 8, int? repsMax = null, string? cue = null) =>
        new(id, sets, reps, repsMax, cue);

    private static Plan Plan(IReadOnlyList<Phase>? phases = null, bool active = true) =>
        PlanFixture.Make(start: Mon, phases: phases, sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises:
            [
                Row("squat", 4, 6, 8, "Chest up"),
                Row("lunge", 2, 10),
            ]),
            PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises:
            [
                Row("bench", 3, 8),
                Row("row", 3, 10),
            ]),
        ]) with { IsActive = active };

    private static async Task<(AppData Data, WorkoutStarter Starter)> OpenAsync(IDataStore? store = null)
    {
        var data = await AppData.OpenAsync(store ?? new InMemoryDataStore());
        await data.CommitAsync([
            Change.Put(new Exercise("squat", "Squat", LogKind.Load, null, false)),
            Change.Put(new Exercise("lunge", "Lunge", LogKind.Load, null, false)),
            Change.Put(new Exercise("bench", "Bench", LogKind.Load, null, false)),
            Change.Put(new Exercise("row", "Row", LogKind.Load, null, false)),
            Change.Put(new Exercise("curl", "Curl", LogKind.Load, null, false)),
            Change.Put(Plan()),
        ]);
        return (data, new WorkoutStarter(data, new SequentialIdGenerator("w"), Clock));
    }

    // ---- Create

    [Fact]
    public void Create_makes_an_in_progress_custom_workout_with_no_session_and_planned_rows()
    {
        var plan = Plan();
        IReadOnlyList<SessionExercise> rows = [Row("squat", 3, 5), Row("curl", 3, 10)];

        var workout = CustomWorkoutStart.Create(plan, rows, Mon, new SequentialIdGenerator("w"), Clock);

        Assert.Equal("w-1", workout.Id);
        Assert.Equal(Mon, workout.Date);
        Assert.Null(workout.SessionId);
        Assert.False(workout.LoggedAfterwards);
        Assert.Equal(Now, workout.StartedAt);
        Assert.Null(workout.EndedAt);
        Assert.Empty(workout.Entries);
        Assert.Null(workout.PainCheck);
        Assert.Equal(rows, workout.Planned);
    }

    // ---- RowsForExercises

    [Fact]
    public void RowsForExercises_keeps_order_and_drops_duplicates()
    {
        var plan = Plan();
        string[] input = ["bench", "squat", "bench", "curl", "squat"];

        var rows = CustomWorkoutStart.RowsForExercises(plan, input, Mon);

        Assert.Equal(3, rows.Count);
        Assert.Equal("bench", rows[0].ExerciseId);
        Assert.Equal("squat", rows[1].ExerciseId);
        Assert.Equal("curl", rows[2].ExerciseId);
    }

    [Fact]
    public void RowsForExercises_takes_plan_row_including_cue_for_planned_exercise()
    {
        var plan = Plan();

        var rows = CustomWorkoutStart.RowsForExercises(plan, ["squat"], Mon);

        var row = Assert.Single(rows);
        Assert.Equal("squat", row.ExerciseId);
        Assert.Equal(4, row.Sets);
        Assert.Equal(6, row.Reps);
        Assert.Equal(8, row.RepsMax);
        Assert.Equal("Chest up", row.Cue);
    }

    [Fact]
    public void RowsForExercises_gives_unplanned_exercise_3_sets_and_no_reps()
    {
        var plan = Plan();

        var rows = CustomWorkoutStart.RowsForExercises(plan, ["curl"], Mon);

        var row = Assert.Single(rows);
        Assert.Equal("curl", row.ExerciseId);
        Assert.Equal(3, row.Sets);
        Assert.Null(row.Reps);
        Assert.Null(row.RepsMax);
        Assert.Null(row.Cue);
    }

    [Fact]
    public void RowsForExercises_uses_current_phase_override_when_present()
    {
        SessionExercise[] overrideRows = [Row("squat", 5, 5, cue: "Phase cue")];
        var phase = new Phase("ph", "Strength", 1, 4, [], [new SessionOverride("lower", overrideRows)]);
        var plan = Plan([phase]);

        var rows = CustomWorkoutStart.RowsForExercises(plan, ["squat"], Mon);

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Sets);
        Assert.Equal(5, row.Reps);
        Assert.Equal("Phase cue", row.Cue);
    }

    [Fact]
    public void RowsForExercises_uses_first_session_match_when_exercise_in_multiple_sessions()
    {
        var plan = PlanFixture.Make(start: Mon, sessions:
        [
            PlanFixture.Session("s1", DayOfWeek.Monday, exercises: [Row("pullup", 3, 5)]),
            PlanFixture.Session("s2", DayOfWeek.Wednesday, exercises: [Row("pullup", 4, 10)]),
        ]);

        var rows = CustomWorkoutStart.RowsForExercises(plan, ["pullup"], Mon);

        var row = Assert.Single(rows);
        Assert.Equal(3, row.Sets);
        Assert.Equal(5, row.Reps);
    }

    // ---- RowsForMakeUp

    [Fact]
    public void RowsForMakeUp_matches_session_rows_as_in_force_that_week()
    {
        var plan = Plan();
        var session = plan.Sessions[0];

        var rows = CustomWorkoutStart.RowsForMakeUp(plan, session, Mon);

        Assert.Equal(session.Exercises, rows);
    }

    [Fact]
    public void RowsForMakeUp_uses_phase_override_when_present()
    {
        SessionExercise[] overrideRows = [Row("front-squat", 5, 5)];
        var phase = new Phase("ph", "Build", 1, 4, [], [new SessionOverride("lower", overrideRows)]);
        var plan = Plan([phase]);
        var session = plan.Sessions[0];

        var rows = CustomWorkoutStart.RowsForMakeUp(plan, session, Mon);

        Assert.Equal(overrideRows, rows);
    }

    // ---- StartCustomAsync

    [Fact]
    public async Task StartCustomAsync_saves_a_new_workout()
    {
        var (data, starter) = await OpenAsync();
        IReadOnlyList<SessionExercise> rows = [Row("squat", 3, 5)];

        var outcome = await starter.StartCustomAsync(Plan(), rows, Mon);

        Assert.IsType<CommitResult.Committed>(outcome.Commit);
        var saved = Assert.Single(data.Snapshot.Workouts);
        Assert.Equal(saved, outcome.Workout);
        Assert.Null(saved.SessionId);
        Assert.False(saved.LoggedAfterwards);
        Assert.Equal(Now, saved.StartedAt);
        Assert.Null(saved.EndedAt);
        Assert.Equal(rows, saved.Planned);
    }

    [Fact]
    public async Task StartCustomAsync_with_workout_already_in_progress_continues_that_one_and_saves_nothing()
    {
        var (data, starter) = await OpenAsync();
        var first = await starter.StartAsync(Plan(), Plan().Sessions[0], Mon);

        var outcome = await starter.StartCustomAsync(Plan(), [Row("bench", 3, 5)], Mon);

        Assert.Equal(first.Workout, outcome.Workout);
        Assert.Null(outcome.Commit);
        Assert.Single(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task StartCustomAsync_with_empty_rows_saves_nothing()
    {
        var (data, starter) = await OpenAsync();

        var outcome = await starter.StartCustomAsync(Plan(), [], Mon);

        Assert.Null(outcome.Workout);
        Assert.Null(outcome.Commit);
        Assert.Empty(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task StartCustomAsync_failed_save_opens_nothing_and_returns_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var (data, starter) = await OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var outcome = await starter.StartCustomAsync(Plan(), [Row("squat", 3, 5)], Mon);

        Assert.Null(outcome.Workout);
        Assert.IsType<CommitResult.StoreFailed>(outcome.Commit);
        Assert.Empty(data.Snapshot.Workouts);
    }

    // ---- Missed session remains missed

    [Fact]
    public void Custom_workout_for_missed_session_leaves_session_missed()
    {
        var plan = Plan();
        var session = plan.Sessions[0]; // Lower on Monday
        var wednesday = Mon.AddDays(2);

        // Make up the missed session with a custom workout on Wednesday
        var makeUpRows = CustomWorkoutStart.RowsForMakeUp(plan, session, wednesday);
        var customWorkout = CustomWorkoutStart.Create(plan, makeUpRows, wednesday, new SequentialIdGenerator("w"), Clock);

        var missed = WeekScheduleRules.MissedInWeek(plan, [], [], [customWorkout], Mon, wednesday);

        var missedSession = Assert.Single(missed);
        Assert.Equal("lower", missedSession.SessionId);
    }
}

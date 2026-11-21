using Core.Model;
using Core.Rules.Starting;
using Core.Storage;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using Core.Time;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Starting;

public class PastDatedStartTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = D(2026, 11, 18);

    private static Plan Plan() => PlanFixture.Make(start: Mon, sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat"), PlanFixture.Row("lunge", 2, 10)]),
    ]);

    private static async Task<(AppData Data, WorkoutStarter Starter)> OpenAsync()
    {
        var data = await AppData.OpenAsync(new InMemoryDataStore());
        await data.CommitAsync(
        [
            Change.Put(new Exercise("squat", "Squat", LogKind.Load, null, false)),
            Change.Put(new Exercise("lunge", "Lunge", LogKind.Load, null, false)),
            Change.Put(Plan()),
        ]);
        return (data, new WorkoutStarter(data, new SequentialIdGenerator("w"), new FixedClock(Now)));
    }

    [Fact]
    public void A_past_dated_Session_workout_has_the_flag_no_timers_and_the_rows_of_that_week()
    {
        var plan = Plan();

        var workout = WorkoutStart.CreatePastDated(plan, plan.Sessions[0], Mon, new SequentialIdGenerator("w"));

        Assert.True(workout.LoggedAfterwards);
        Assert.Equal(Mon, workout.Date);
        Assert.Equal("lower", workout.SessionId);
        Assert.Null(workout.StartedAt);
        Assert.Null(workout.EndedAt);
        Assert.Equal(["squat", "lunge"], workout.Planned.Select(r => r.ExerciseId));
        Assert.Empty(workout.Entries);
    }

    [Fact]
    public void A_past_dated_Custom_workout_has_an_empty_plan_and_no_Session()
    {
        var workout = WorkoutStart.CreatePastDated(null, null, Mon, new SequentialIdGenerator("w"));

        Assert.True(workout.LoggedAfterwards);
        Assert.Null(workout.SessionId);
        Assert.Empty(workout.Planned);
        Assert.Null(workout.StartedAt);
    }

    [Fact]
    public async Task Starting_logged_afterwards_saves_the_workout()
    {
        var (data, starter) = await OpenAsync();
        var plan = Plan();

        var outcome = await starter.StartLoggedAfterwardsAsync(plan, plan.Sessions[0], Mon, Today);

        Assert.IsType<CommitResult.Committed>(outcome.Commit);
        var saved = Assert.Single(data.Snapshot.Workouts);
        Assert.Equal(saved, outcome.Workout);
        Assert.True(saved.LoggedAfterwards);
    }

    [Fact]
    public async Task Starting_logged_afterwards_on_a_future_date_saves_nothing()
    {
        var (data, starter) = await OpenAsync();

        var outcome = await starter.StartLoggedAfterwardsAsync(null, null, Today.AddDays(1), Today);

        Assert.Null(outcome.Workout);
        Assert.Null(outcome.Commit);
        Assert.Empty(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task Starting_logged_afterwards_with_a_workout_in_progress_continues_it()
    {
        var (data, starter) = await OpenAsync();
        var first = await starter.StartLoggedAfterwardsAsync(null, null, Mon, Today);

        var second = await starter.StartLoggedAfterwardsAsync(null, null, Mon.AddDays(1), Today);

        Assert.Equal(first.Workout, second.Workout);
        Assert.Single(data.Snapshot.Workouts);
    }
}

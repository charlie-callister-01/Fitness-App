using Core.Model;
using Core.Rules.Starting;
using Core.Storage;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using Core.Tests.Storage;
using Core.Time;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Starting;

public class WorkoutStarterTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    private static Plan Plan() => PlanFixture.Make(start: Mon, sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [PlanFixture.Row("squat")]),
    ]);

    private static async Task<(AppData Data, WorkoutStarter Starter)> OpenAsync(IDataStore? store = null)
    {
        var data = await AppData.OpenAsync(store ?? new InMemoryDataStore());
        await data.CommitAsync([Change.Put(new Exercise("squat", "Squat", LogKind.Load, null, false)), Change.Put(Plan())]);
        return (data, new WorkoutStarter(data, new SequentialIdGenerator("w"), new FixedClock(Now)));
    }

    [Fact]
    public async Task Starting_saves_an_in_progress_workout_and_says_to_open_it()
    {
        var (data, starter) = await OpenAsync();

        var outcome = await starter.StartAsync(Plan(), Plan().Sessions[0], Mon);

        Assert.IsType<CommitResult.Committed>(outcome.Commit);
        var saved = Assert.Single(data.Snapshot.Workouts);
        Assert.Equal(saved, outcome.Workout);
        Assert.Null(saved.EndedAt);
        Assert.Equal("lower", saved.SessionId);
    }

    [Fact]
    public async Task With_a_workout_already_in_progress_it_continues_that_one_and_saves_nothing()
    {
        var (data, starter) = await OpenAsync();
        var first = await starter.StartAsync(Plan(), Plan().Sessions[0], Mon);

        var second = await starter.StartAsync(Plan(), Plan().Sessions[0], Mon);

        Assert.Equal(first.Workout, second.Workout);
        Assert.Null(second.Commit);
        Assert.Single(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task A_failed_save_opens_nothing_and_hands_back_the_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var (data, starter) = await OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var outcome = await starter.StartAsync(Plan(), Plan().Sessions[0], Mon);

        Assert.Null(outcome.Workout);
        Assert.IsType<CommitResult.StoreFailed>(outcome.Commit);
        Assert.Empty(data.Snapshot.Workouts);
    }
}

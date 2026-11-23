using Core.Model;
using Core.Storage;
using Ui.Accessibility;
using Ui.Log;
using Ui.Preferences;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Log;

/// <summary>The log's writes without a page: each action commits through the one writer and moves the pager where the owner expects.</summary>
public sealed class LogActionsTests
{
    private sealed record Rig(LogActions Actions, LogEditState State, AppData Data);

    private static async Task<Rig> OpenAsync(params WorkoutEntry[] entries)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now", entries));
        var clock = new TestClock(Now);
        var notice = new SaveFailureNotice();
        var state = new LogEditState();
        var session = new LogSession(
            data, clock, new FakeTicker(), new RecordingWakeLock(), new RestTimerSetting(new FakeDevicePreferences()),
            new FinishFlow(data, notice, clock, new CountingIds()), new LoggedAfterwardsFlow(data, notice), new Announcer(), () => { });
        var actions = new LogActions(data, notice, new CountingIds(), state, new LogViewBuilder(data, state), session, new Announcer()) { WorkoutId = "w-now" };
        return new Rig(actions, state, data);
    }

    private static Workout Workout(Rig rig) => rig.Data.Snapshot.Workouts.Single();

    [Fact]
    public async Task Skip_records_the_reason_and_moves_to_the_next_Exercise_not_yet_handled()
    {
        var rig = await OpenAsync();

        await rig.Actions.ApplyAsync(new ExerciseAction.Skip(Squat, "rack taken"));

        var entry = Assert.Single(Workout(rig).Entries);
        Assert.Equal((Squat, EntryStatus.Skipped, "rack taken"), (entry.ExerciseId, entry.Status, entry.SkipReason));
        Assert.Equal(1, rig.State.Current);
    }

    [Fact]
    public async Task Add_goes_to_the_new_Exercise_and_Remove_takes_it_out_again()
    {
        var rig = await OpenAsync();

        await rig.Actions.ApplyAsync(new ExerciseAction.Add(Squat, "plank"));
        Assert.Equal(1, rig.State.Current);
        Assert.Contains(Workout(rig).Entries, e => e.ExerciseId == "plank");

        await rig.Actions.ApplyAsync(new ExerciseAction.Remove("plank"));
        Assert.Equal(0, rig.State.Current);
        Assert.DoesNotContain(Workout(rig).Entries, e => e.ExerciseId == "plank");
    }

    [Fact]
    public async Task Swap_stores_the_replacement_and_Undo_swap_puts_the_planned_Exercise_back()
    {
        var rig = await OpenAsync();

        await rig.Actions.ApplyAsync(new ExerciseAction.Swap(Squat, "pullup"));
        Assert.Equal(EntryStatus.Swapped, Assert.Single(Workout(rig).Entries).Status);

        await rig.Actions.ApplyAsync(new ExerciseAction.UndoSwap(Squat));
        Assert.Empty(Workout(rig).Entries);
    }

    [Fact]
    public async Task Marking_an_Exercise_as_a_benchmark_saves_it_at_once()
    {
        var rig = await OpenAsync();

        await rig.Actions.ApplyAsync(new ExerciseAction.ToggleBenchmark(Squat, true));

        Assert.True(rig.Data.Snapshot.Exercises.Single(e => e.Id == Squat).IsBenchmark);
    }

    [Fact]
    public async Task RPE_creates_the_entry_when_no_Set_is_confirmed_yet()
    {
        var rig = await OpenAsync();

        await rig.Actions.SetRpeAsync(Squat, 8);

        Assert.Equal(8, Assert.Single(Workout(rig).Entries).Rpe);
    }
}

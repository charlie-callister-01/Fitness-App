using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;
using Ui.Log;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

/// <summary>The finish flow without a page: refusals come back from the writer, and a read-only window can still dismiss the Pain check.</summary>
public sealed class FinishFlowTests
{
    private static WorkoutEntry SquatDone() =>
        new(Squat, EntryStatus.Done, null, null, null, [Set("s1", 20, 8), Set("s2", 20, 8), Set("s3", 20, 8)]);

    private static WorkoutEntry LungeDone() =>
        new(Lunge, EntryStatus.Done, null, null, null, [Set("l1", 10, 10), Set("l2", 10, 10)]);

    private static async Task<(AppData Data, FinishFlow Flow)> OpenAsync(params WorkoutEntry[] entries)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now", entries));
        return (data, new FinishFlow(data, new SaveFailureNotice(), new FixedClock(Now), new CountingIds()));
    }

    [Fact]
    public async Task Finish_with_no_confirmed_Set_is_refused_by_the_write_and_commits_nothing()
    {
        var (data, flow) = await OpenAsync();

        await flow.RequestAsync("w-now", canWrite: true);

        Assert.NotNull(flow.Message);
        Assert.Equal(FinishPhase.None, flow.Phase);
        Assert.Null(data.Snapshot.Workouts.Single().EndedAt);
        Assert.False(flow.Saving);
    }

    [Fact]
    public async Task Pain_Yes_without_an_area_is_refused_by_the_write_and_the_Pain_check_stays_open()
    {
        var (data, flow) = await OpenAsync(SquatDone(), LungeDone());
        await flow.RequestAsync("w-now", canWrite: true);
        Assert.Equal(FinishPhase.PainCheck, flow.Phase);

        await flow.AnswerAsync("w-now", new PainAnswer.Yes([], null, null, null, null), canWrite: true);

        Assert.NotNull(flow.PainMessage);
        Assert.Equal(FinishPhase.PainCheck, flow.Phase);
        Assert.Null(data.Snapshot.Workouts.Single().PainCheck);
    }

    [Fact]
    public async Task Skip_closes_the_Pain_check_even_when_the_window_can_no_longer_write()
    {
        var (_, flow) = await OpenAsync(SquatDone(), LungeDone());
        await flow.RequestAsync("w-now", canWrite: true);

        await flow.AnswerAsync("w-now", new PainAnswer.Skipped(), canWrite: false);

        Assert.Equal(FinishPhase.None, flow.Phase);
    }
}

using Core.Storage;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Saving;

public sealed class WorkoutWriterTests
{
    [Fact]
    public async Task A_change_to_a_workout_that_is_gone_says_so_through_the_notice_and_saves_nothing()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var notice = new SaveFailureNotice();
        var writer = new WorkoutWriter(data, notice);
        await data.CommitAsync(Change.Delete(RecordKind.Workout, "w-now"));

        var result = await writer.ChangeAsync("w-now", w => w with { EndedAt = Now });

        Assert.Null(result);
        Assert.Contains("no longer open", notice.Message);
        Assert.Empty(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task A_change_is_applied_to_the_latest_copy_and_a_success_clears_the_notice()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var notice = new SaveFailureNotice();
        var writer = new WorkoutWriter(data, notice);

        var result = await writer.ChangeAsync("w-now", w => w with { EndedAt = Now });

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(Now, data.Snapshot.Workouts.Single().EndedAt);
        Assert.Null(notice.Message);
    }
}

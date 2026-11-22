using Core.Rules.Finishing;
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
        var writer = new WorkoutWriter(data, new GuardedSave(data, notice));
        await data.CommitAsync(Change.Delete(RecordKind.Workout, "w-now"));

        var result = await writer.ChangeAsync("w-now", w => w with { EndedAt = Now });

        Assert.Equal(SaveStatus.Gone, result.Status);
        Assert.Contains("no longer open", notice.Message);
        Assert.Empty(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task A_refused_edit_commits_nothing_and_hands_back_its_message()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var notice = new SaveFailureNotice();
        var writer = new WorkoutWriter(data, new GuardedSave(data, notice));

        var outcome = await writer.EditAsync("w-now", _ => EditResult.Refused("Not allowed."));

        Assert.False(outcome.IsCommitted);
        Assert.Equal("Not allowed.", outcome.Refusal);
        Assert.Null(data.Snapshot.Workouts.Single().EndedAt);
        Assert.Null(notice.Message);
    }

    [Fact]
    public async Task An_edit_is_judged_against_the_latest_copy_so_a_change_made_since_is_seen_by_the_rule()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var writer = new WorkoutWriter(data, new GuardedSave(data, new SaveFailureNotice()));
        await data.CommitAsync(Change.Put(data.Snapshot.Workouts.Single() with { EndedAt = Now }));

        var outcome = await writer.EditAsync("w-now", w => w.EndedAt is null ? EditResult.Changed(w) : EditResult.Refused("Already finished."));

        Assert.Equal("Already finished.", outcome.Refusal);
    }

    [Fact]
    public async Task An_accepted_edit_is_committed()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var writer = new WorkoutWriter(data, new GuardedSave(data, new SaveFailureNotice()));

        var outcome = await writer.EditAsync("w-now", w => EditResult.Changed(w with { EndedAt = Now }));

        Assert.True(outcome.IsCommitted);
        Assert.Null(outcome.Refusal);
        Assert.Equal(Now, data.Snapshot.Workouts.Single().EndedAt);
    }

    [Fact]
    public async Task A_change_is_applied_to_the_latest_copy_and_a_success_clears_the_notice()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var notice = new SaveFailureNotice();
        var writer = new WorkoutWriter(data, new GuardedSave(data, notice));

        var result = await writer.ChangeAsync("w-now", w => w with { EndedAt = Now });

        Assert.True(result.IsSaved);
        Assert.Equal(Now, data.Snapshot.Workouts.Single().EndedAt);
        Assert.Null(notice.Message);
    }
}

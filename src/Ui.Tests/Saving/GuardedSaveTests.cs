using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Saving;

public sealed class GuardedSaveTests
{
    private static async Task<(AppData Data, FaultInjectingDataStore Store)> OpenAsync(IStorageEnvironment? environment = null)
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = environment is null ? await AppData.OpenAsync(store) : await AppData.OpenAsync(store, environment);
        return (data, store);
    }

    private static Change Weight(string id) => Change.Put(new WeighIn(id, new DateOnly(2026, 11, 16), 80.0));

    [Fact]
    public async Task A_commit_saves_clears_the_notice_and_reports_saved()
    {
        var (data, _) = await OpenAsync();
        var notice = new SaveFailureNotice();
        notice.ReportMessage("An earlier failure.");

        var result = await new GuardedSave(data, notice).CommitAsync(Weight("wi-1"));

        Assert.Equal(SaveStatus.Saved, result.Status);
        Assert.True(result.IsSaved);
        Assert.Single(data.Snapshot.WeighIns);
        Assert.Null(notice.Message);
    }

    [Fact]
    public async Task A_failed_commit_reports_to_the_notice_and_saves_nothing()
    {
        var (data, store) = await OpenAsync();
        var notice = new SaveFailureNotice();
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var result = await new GuardedSave(data, notice).CommitAsync(Weight("wi-1"));

        Assert.Equal(SaveStatus.Failed, result.Status);
        Assert.Contains("out of storage", notice.Message);
        Assert.Empty(data.Snapshot.WeighIns);
    }

    [Fact]
    public async Task A_read_only_window_refuses_without_trying_to_write()
    {
        var (data, _) = await OpenAsync(new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var notice = new SaveFailureNotice();
        var save = new GuardedSave(data, notice);

        var result = await save.CommitAsync(Weight("wi-1"));

        Assert.False(save.CanAct);
        Assert.Equal(SaveStatus.ReadOnly, result.Status);
        Assert.Null(result.Commit);
        Assert.Null(notice.Message);
        Assert.Empty(data.Snapshot.WeighIns);
    }

    [Fact]
    public async Task A_second_request_while_one_is_saving_does_nothing()
    {
        var (data, _) = await OpenAsync();
        var save = new GuardedSave(data, new SaveFailureNotice());
        var release = new TaskCompletionSource();
        var runs = 0;

        var first = save.RunAsync(async () =>
        {
            runs++;
            await release.Task;
            return SaveWork.Committed(await data.CommitAsync(Weight("wi-1")));
        });
        var second = await save.CommitAsync(Weight("wi-2"));

        Assert.True(save.IsBusy);
        Assert.False(save.CanAct);
        Assert.Equal(SaveStatus.Busy, second.Status);
        release.SetResult();
        Assert.Equal(SaveStatus.Saved, (await first).Status);
        Assert.False(save.IsBusy);
        Assert.Equal(1, runs);
        Assert.Equal("wi-1", data.Snapshot.WeighIns.Single().Id);
    }

    [Fact]
    public async Task The_guard_is_released_after_work_that_throws()
    {
        var (data, _) = await OpenAsync();
        var save = new GuardedSave(data, new SaveFailureNotice());

        await Assert.ThrowsAsync<InvalidOperationException>(() => save.RunAsync(() => throw new InvalidOperationException()));

        Assert.False(save.IsBusy);
        Assert.True((await save.CommitAsync(Weight("wi-1"))).IsSaved);
    }

    [Fact]
    public async Task A_record_that_is_gone_says_so_through_the_notice_and_saves_nothing()
    {
        var (data, _) = await OpenAsync();
        var notice = new SaveFailureNotice();

        var result = await new GuardedSave(data, notice).RunAsync(() => Task.FromResult(SaveWork.Gone("That thing is no longer there.")));

        Assert.Equal(SaveStatus.Gone, result.Status);
        Assert.Equal("That thing is no longer there.", notice.Message);
    }

    [Fact]
    public async Task Work_that_commits_nothing_leaves_the_notice_alone()
    {
        var (data, _) = await OpenAsync();
        var notice = new SaveFailureNotice();
        notice.ReportMessage("An earlier failure.");

        var result = await new GuardedSave(data, notice).RunAsync(() => Task.FromResult(SaveWork.None));

        Assert.Equal(SaveStatus.NothingToSave, result.Status);
        Assert.Equal("An earlier failure.", notice.Message);
    }

    [Fact]
    public async Task Started_is_raised_once_busy_and_before_the_work_runs()
    {
        var (data, _) = await OpenAsync();
        var save = new GuardedSave(data, new SaveFailureNotice());
        var busyWhenStarted = new List<bool>();
        save.Started += () => busyWhenStarted.Add(save.IsBusy);

        await save.CommitAsync(Weight("wi-1"));

        Assert.Equal([true], busyWhenStarted);
    }

    [Fact]
    public async Task A_live_control_can_skip_the_read_only_check_and_gets_the_commits_own_failure()
    {
        var (data, _) = await OpenAsync(new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var notice = new SaveFailureNotice();

        var result = await new GuardedSave(data, notice).CommitAsync(Weight("wi-1"), requireWrite: false);

        Assert.Equal(SaveStatus.Failed, result.Status);
        Assert.NotNull(notice.Message);
    }
}

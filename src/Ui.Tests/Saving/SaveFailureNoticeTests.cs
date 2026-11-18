using Bunit;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Ui.Layouts;
using Ui.Saving;

namespace Ui.Tests.Saving;

public sealed class SaveFailureNoticeTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private sealed class Page : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "page body");
    }

    private static async Task<CommitResult> FailedCommitAsync(AppData data) =>
        await data.CommitAsync(Change.Put(data.Snapshot.Settings with { RestTimerSeconds = 90 }));

    [Fact]
    public async Task A_store_failure_is_reported_in_plain_words_and_a_later_success_clears_it()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await AppData.OpenAsync(store);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        _notice.Report(await FailedCommitAsync(data));
        Assert.Contains("out of storage", _notice.Message);

        store.Recover();
        _notice.Report(await FailedCommitAsync(data));
        Assert.Null(_notice.Message);
    }

    [Fact]
    public async Task A_held_writer_lock_says_another_window_is_the_one_that_saves()
    {
        var environment = new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere };
        var data = await AppData.OpenAsync(new InMemoryDataStore(), environment);

        _notice.Report(await FailedCommitAsync(data));

        Assert.Contains("Another window", _notice.Message);
    }

    [Fact]
    public async Task A_blocked_startup_says_nothing_can_be_saved()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.Unexpected);
        var data = await AppData.OpenAsync(store);
        store.Recover();

        var result = await FailedCommitAsync(data);
        Assert.IsType<CommitResult.Blocked>(result);
        _notice.Report(result);

        Assert.Contains("nothing can be saved", _notice.Message);
    }

    [Fact]
    public void Unreadable_data_and_failed_storage_get_different_blocked_messages()
    {
        _notice.Report(new CommitResult.Blocked(StartupState.CannotRead));
        var unreadable = _notice.Message;
        _notice.Report(new CommitResult.Blocked(StartupState.StorageFailed));

        Assert.NotNull(unreadable);
        Assert.NotEqual(unreadable, _notice.Message);
    }

    [Fact]
    public void A_rejected_commit_shows_the_first_issues_message()
    {
        var issues = new[] { new FormatIssue("a", "x", "Weight must be a number."), new FormatIssue("b", "y", "Other.") };

        _notice.Report(new CommitResult.Rejected(issues));

        Assert.Equal("Weight must be a number.", _notice.Message);
    }

    [Fact]
    public void Each_failure_kind_has_its_own_message()
    {
        var kinds = Enum.GetValues<StoreFailureKind>().Where(k => k != StoreFailureKind.Unexpected);
        var messages = kinds.Select(kind =>
        {
            var notice = new SaveFailureNotice();
            notice.Report(new CommitResult.StoreFailed(new StoreFailure(kind)));
            return notice.Message;
        }).ToList();

        Assert.All(messages, Assert.NotNull);
        Assert.Equal(messages.Count, messages.Distinct().Count());
    }

    [Fact]
    public void Changed_is_raised_only_when_the_message_changes()
    {
        var raised = 0;
        _notice.Changed += () => raised++;

        _notice.Report(new CommitResult.Committed());
        _notice.Report(new CommitResult.StoreFailed(new StoreFailure(StoreFailureKind.StorageFull)));
        _notice.Report(new CommitResult.Committed());

        Assert.Equal(2, raised);
    }

    [Theory]
    [InlineData(typeof(MainLayout))]
    [InlineData(typeof(FullScreenLayout))]
    [InlineData(typeof(PushedPageLayout))]
    public void Every_layout_shows_the_banner_and_hides_it_again_on_success(Type layout)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<InAppHistory>();
        Services.AddSingleton(_notice);
        var cut = Render<LayoutView>(p => p.Add(l => l.Layout, layout).AddChildContent<Page>());
        Assert.Empty(cut.FindAll(".save-failure-banner"));

        cut.InvokeAsync(() => _notice.Report(new CommitResult.StoreFailed(new StoreFailure(StoreFailureKind.StorageFull))));
        Assert.Contains("out of storage", cut.Find(".save-failure-banner").TextContent);
        Assert.Equal("alert", cut.Find(".save-failure-banner").GetAttribute("role"));

        cut.InvokeAsync(() => _notice.Report(new CommitResult.Committed()));
        Assert.Empty(cut.FindAll(".save-failure-banner"));
    }
}

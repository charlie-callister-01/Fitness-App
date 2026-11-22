using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

public sealed class PageContextTests
{
    [Fact]
    public async Task A_writable_window_has_no_read_only_message_and_reads_the_active_Plan_and_today()
    {
        var data = await OpenSeededAsync(plan: Plan());

        var page = new PageContext(data, new FixedClock(Now));

        Assert.True(page.CanWrite);
        Assert.Null(page.ReadOnlyMessage);
        Assert.Equal(Plan().Id, page.ActivePlan!.Id);
        Assert.Equal(DateOnly.FromDateTime(Now.ToLocalTime().DateTime), page.Today);
    }

    [Fact]
    public async Task A_window_with_another_window_open_is_read_only_and_says_so()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });

        var page = new PageContext(locked, new FixedClock(Now));

        Assert.False(page.CanWrite);
        Assert.Contains("Another window", page.ReadOnlyMessage);
        Assert.Contains("read-only", page.ReadOnlyMessage);
    }

    [Fact]
    public async Task With_no_Plan_there_is_no_active_Plan()
    {
        var data = await OpenSeededAsync();

        Assert.Null(new PageContext(data, new FixedClock(Now)).ActivePlan);
    }
}

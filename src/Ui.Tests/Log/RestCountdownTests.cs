using System.Collections.Concurrent;
using Ui.Log;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Log;

/// <summary>The rest countdown's events, without a page.</summary>
public sealed class RestCountdownTests
{
    [Fact]
    public async Task Reaching_zero_raises_Ended_before_the_change_that_clears_the_readout()
    {
        var clock = new TestClock(Now);
        var ticker = new FakeTicker();
        using var rest = new RestCountdown(clock, ticker);
        var events = new ConcurrentQueue<string>();
        var cleared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        rest.Ended += () => events.Enqueue("ended");
        rest.Changed += () =>
        {
            if (rest.Label is null)
            {
                events.Enqueue("cleared");
                cleared.TrySetResult();
            }
        };
        rest.Start(60);

        clock.Advance(TimeSpan.FromSeconds(60));
        ticker.Tick();
        await cleared.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(["ended", "cleared"], events);
    }
}

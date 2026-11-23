using Core.Model;
using Core.Storage;
using Ui.Log;
using Ui.Preferences;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Log;

/// <summary>The rest countdown, the Wake Lock rules and the event wiring the page used to hold, without a page.</summary>
public sealed class LogSessionTests
{
    private sealed record Rig(
        LogSession Session, RecordingWakeLock Lock, FakeTicker Ticker, RestTimerSetting Setting, AppData Data, List<int> Changes);

    private static async Task<Rig> OpenAsync(int restSeconds = 60)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var clock = new TestClock(Now);
        var ticker = new FakeTicker();
        var wake = new RecordingWakeLock();
        var setting = new RestTimerSetting(new FakeDevicePreferences());
        await setting.SetAsync(restSeconds);
        var notice = new SaveFailureNotice();
        var changes = new List<int>();
        var session = new LogSession(
            data, clock, ticker, wake, setting, new FinishFlow(data, notice, clock, new CountingIds()),
            new LoggedAfterwardsFlow(data, notice), new Announcer(), () => changes.Add(1));
        return new Rig(session, wake, ticker, setting, data, changes);
    }

    [Fact]
    public async Task The_screen_lock_is_taken_once_for_an_open_Workout_and_released_when_it_is_gone()
    {
        var rig = await OpenAsync();
        var open = rig.Data.Snapshot.Workouts.Single();

        await rig.Session.SyncScreenLockAsync(open);
        await rig.Session.SyncScreenLockAsync(open);
        await rig.Session.SyncScreenLockAsync(null);

        Assert.Equal(["acquire", "release"], rig.Lock.Calls);
    }

    [Fact]
    public async Task A_Workout_logged_afterwards_does_not_take_the_screen_lock()
    {
        var rig = await OpenAsync();

        await rig.Session.SyncScreenLockAsync(rig.Data.Snapshot.Workouts.Single() with { LoggedAfterwards = true });

        Assert.Empty(rig.Lock.Calls);
    }

    [Fact]
    public async Task Disposing_while_the_screen_is_locked_releases_it_and_stops_the_rest_ticker()
    {
        var rig = await OpenAsync();
        await rig.Session.SyncScreenLockAsync(rig.Data.Snapshot.Workouts.Single());
        rig.Session.StartRest(loggedAfterwards: false);
        await Task.Yield();

        rig.Session.Dispose();

        Assert.Equal(["acquire", "release"], rig.Lock.Calls);
        Assert.Equal(0, rig.Ticker.Waiting);
    }

    [Fact]
    public async Task A_rest_starts_with_the_chosen_length_but_never_for_a_Workout_logged_afterwards()
    {
        var rig = await OpenAsync(restSeconds: 90);

        rig.Session.StartRest(loggedAfterwards: false);
        Assert.Equal("Rest 1:30", rig.Session.Rest.Label);

        rig.Session.StartRest(loggedAfterwards: true);
        Assert.Null(rig.Session.Rest.Label);
    }

    [Fact]
    public async Task Turning_the_rest_timer_off_stops_a_running_rest()
    {
        var rig = await OpenAsync();
        rig.Session.StartRest(loggedAfterwards: false);

        await rig.Setting.SetAsync(0);

        Assert.Null(rig.Session.Rest.Label);
    }

    [Fact]
    public async Task A_data_change_asks_for_a_redraw_and_a_disposed_session_no_longer_does()
    {
        var rig = await OpenAsync();
        rig.Changes.Clear();

        await rig.Data.CommitAsync(Change.Put(rig.Data.Snapshot.Workouts.Single() with { LoggedAfterwards = true }));
        Assert.NotEmpty(rig.Changes);

        rig.Session.Dispose();
        rig.Changes.Clear();
        await rig.Data.CommitAsync(Change.Put(rig.Data.Snapshot.Workouts.Single() with { LoggedAfterwards = false }));
        Assert.Empty(rig.Changes);
    }
}

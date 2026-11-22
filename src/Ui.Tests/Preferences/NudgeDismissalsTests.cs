using Ui.Preferences;
using Ui.Tests.Pages;

namespace Ui.Tests.Preferences;

public sealed class NudgeDismissalsTests
{
    private const string ThisWeek = "2026-11-16|MissedSession|lower";
    private const string NextWeek = "2026-11-23|MissedSession|lower";

    [Fact]
    public async Task A_store_never_set_reads_as_not_dismissed()
    {
        var dismissals = new NudgeDismissals(new PageFixture.FakeDevicePreferences());

        var loaded = await dismissals.LoadAsync([ThisWeek]);

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task Dismiss_then_Load_round_trips_and_returns_only_dismissed_keys()
    {
        var preferences = new PageFixture.FakeDevicePreferences();
        var dismissals = new NudgeDismissals(preferences);

        await dismissals.DismissAsync(ThisWeek);
        var loaded = await dismissals.LoadAsync([ThisWeek, "2026-11-16|NoRecentBackup|"]);

        Assert.Equal([ThisWeek], loaded);
        Assert.Equal("1", preferences.Values["nudge-dismissed:" + ThisWeek]);
    }

    [Fact]
    public async Task A_key_from_another_week_is_not_dismissed()
    {
        var dismissals = new NudgeDismissals(new PageFixture.FakeDevicePreferences());
        await dismissals.DismissAsync(ThisWeek);

        var loaded = await dismissals.LoadAsync([NextWeek]);

        Assert.Empty(loaded);
    }
}

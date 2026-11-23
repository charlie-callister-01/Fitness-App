using Bunit;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Theming;

namespace Ui.Tests.Pages;

public sealed class SettingsTests : BunitContext
{
    private async Task<(IRenderedComponent<SettingsPage> Cut, AppData Data, PageFixture.FakeBrowserCapabilities Browser)> RenderSettingsAsync(
        IStorageEnvironment? environment = null,
        PageFixture.FakeBrowserCapabilities? browser = null,
        Settings? initialSettings = null)
    {
        var store = new InMemoryDataStore();
        var data = environment is null
            ? await AppData.OpenAsync(store)
            : await AppData.OpenAsync(store, environment);

        if (initialSettings is not null)
        {
            Assert.IsType<CommitResult.Committed>(await data.CommitAsync(Change.Put(initialSettings)));
        }

        browser ??= new PageFixture.FakeBrowserCapabilities();
        PageFixture.Wire(this, data, browser: browser);
        var cut = Render<SettingsPage>();
        return (cut, data, browser);
    }

    [Fact]
    public async Task Look_section_switches_theme_and_mode_with_live_primary_button_preview()
    {
        var (cut, data, _) = await RenderSettingsAsync();

        var preview = cut.Find("[data-testid='look-preview']");
        Assert.Equal("keep", preview.GetAttribute("data-preview-theme"));
        Assert.Equal("system", preview.GetAttribute("data-preview-mode"));
        Assert.NotNull(preview.QuerySelector("button.fa-btn--primary"));

        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "Clean").Click());
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "Light").Click());

        Assert.Equal(Theme.Clean, data.Snapshot.Settings.Theme);
        Assert.Equal(Mode.Light, data.Snapshot.Settings.Mode);
        preview = cut.Find("[data-testid='look-preview']");
        Assert.Equal("clean", preview.GetAttribute("data-preview-theme"));
        Assert.Equal("light", preview.GetAttribute("data-preview-mode"));
    }

    [Fact]
    public async Task Units_and_training_section_saves_units_increment_rest_and_goal_weight_in_active_unit()
    {
        var (cut, data, _) = await RenderSettingsAsync();

        // Switch weight unit to lb and distance unit to miles
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "lb").Click());
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "miles").Click());

        Assert.Equal(WeightUnit.Lb, data.Snapshot.Settings.DisplayWeightUnit);
        Assert.Equal(DistanceUnit.Mi, data.Snapshot.Settings.DisplayDistanceUnit);

        // Goal weight is entered in lb and stored in canonical kg
        var goalInput = cut.Find("#settings-goal-weight");
        await cut.InvokeAsync(() => goalInput.Change("165"));

        Assert.NotNull(data.Snapshot.Settings.GoalWeightKg);
        Assert.Equal(165 * 0.45359237, data.Snapshot.Settings.GoalWeightKg!.Value, precision: 6);

        // Switching back to kg updates the displayed goal weight to kg
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "kg").Click());
        Assert.Equal("74.75", cut.Find("#settings-goal-weight").GetAttribute("value"));

        // Invalid weight increment shows error and does not save
        await cut.InvokeAsync(() => cut.Find("#settings-weight-increment").Change("0"));
        Assert.Contains("positive", cut.Find("[data-testid='weight-increment-error']").TextContent);
        Assert.Equal(2.5, data.Snapshot.Settings.WeightIncrementKg);

        // Valid weight increment and rest timer save immediately
        await cut.InvokeAsync(() => cut.Find("#settings-weight-increment").Change("1.25"));
        await cut.InvokeAsync(() => cut.Find("#settings-rest-timer").Change("120"));

        Assert.Equal(1.25, data.Snapshot.Settings.WeightIncrementKg);
        Assert.Equal(120, data.Snapshot.Settings.RestTimerSeconds);
    }

    [Fact]
    public async Task Streak_rules_section_saves_minimum_workouts_and_custom_workout_toggle()
    {
        var (cut, data, _) = await RenderSettingsAsync();

        await cut.InvokeAsync(() => cut.Find("#settings-streak-min").Change("0"));
        Assert.Contains("at least 1", cut.Find("[data-testid='streak-min-error']").TextContent);
        Assert.Equal(3, data.Snapshot.Settings.StreakMinWorkoutsPerWeek);

        await cut.InvokeAsync(() => cut.Find("#settings-streak-min").Change("5"));
        await cut.InvokeAsync(() => cut.Find("#settings-streak-custom").Change(true));

        Assert.Equal(5, data.Snapshot.Settings.StreakMinWorkoutsPerWeek);
        Assert.True(data.Snapshot.Settings.StreakCountsCustomWorkouts);
    }

    [Fact]
    public async Task Data_section_backs_up_requests_persistence_shows_estimate_and_triggers_install()
    {
        var env = new InMemoryStorageEnvironment
        {
            Persistence = PersistenceStatus.NotGranted,
            Estimate = new StorageEstimate(2_500_000, 100_000_000),
        };
        var browser = new PageFixture.FakeBrowserCapabilities
        {
            IsStandalone = false,
            CanInstall = true,
        };

        var (cut, data, _) = await RenderSettingsAsync(environment: env, browser: browser);

        Assert.Contains("Never", cut.Find("[data-testid='last-backup']").TextContent);
        Assert.Contains("Not granted", cut.Find("[data-testid='persistence-status']").TextContent);
        Assert.Contains("2.5 MB", cut.Find("[data-testid='storage-estimate']").TextContent);
        Assert.Contains("Ready to install", cut.Find("[data-testid='install-status']").TextContent);

        // Back up downloads a valid Backup JSON and updates LastBackupAt
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "Back up").Click());
        Assert.Single(browser.Downloads);
        var downloaded = browser.Downloads[0];
        Assert.Equal("application/json", downloaded.ContentType);
        Assert.NotNull(ExchangeFormat.Read(downloaded.Content).Document);
        Assert.Equal(PageFixture.Now, data.LastBackupAt);
        Assert.DoesNotContain("Never", cut.Find("[data-testid='last-backup']").TextContent);
        Assert.Equal($"Backup downloaded ({downloaded.FileName}).", Services.GetRequiredService<Ui.Saving.Announcer>().Message);

        // Request persistent storage updates status
        env.Persistence = PersistenceStatus.Granted;
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "Request persistent storage").Click());
        Assert.Equal(PersistenceStatus.Granted, data.Health.Persistence);
        Assert.Contains("Granted", cut.Find("[data-testid='persistence-status']").TextContent);

        // Install app triggers prompt and updates status
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent.Trim() == "Install app").Click());
        Assert.Equal(1, browser.InstallPrompts);
        Assert.Contains("Installed", cut.Find("[data-testid='install-status']").TextContent);
    }

    [Fact]
    public async Task ReadOnly_window_shows_ReadOnlyWell_and_disables_settings_writes()
    {
        var env = new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere };
        var (cut, data, _) = await RenderSettingsAsync(environment: env);

        Assert.NotNull(cut.Find("[role='status']"));
        Assert.False(data.Health.CanWrite);

        Assert.True(cut.Find("#settings-weight-increment").HasAttribute("disabled"));
        Assert.True(cut.Find("#settings-rest-timer").HasAttribute("disabled"));
        Assert.True(cut.Find("#settings-goal-weight").HasAttribute("disabled"));
        Assert.True(cut.Find("#settings-streak-min").HasAttribute("disabled"));
        Assert.True(cut.Find("#settings-streak-custom").HasAttribute("disabled"));

        var kgButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "kg");
        var backupButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Back up");
        Assert.True(kgButton.HasAttribute("disabled"));
        Assert.True(backupButton.HasAttribute("disabled"));
    }
}

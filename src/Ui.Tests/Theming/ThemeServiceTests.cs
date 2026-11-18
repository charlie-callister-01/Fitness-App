using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Ui.Theming;

namespace Ui.Tests.Theming;

public sealed class ThemeServiceTests
{
    private sealed class FakeThemeInterop : IThemeInterop
    {
        public List<(Theme Theme, Mode Mode)> Applied { get; } = [];

        public bool Throw { get; set; }

        public Task ApplyAsync(Theme theme, Mode mode)
        {
            if (Throw)
            {
                throw new InvalidOperationException("js is gone");
            }

            Applied.Add((theme, mode));
            return Task.CompletedTask;
        }
    }

    private static async Task<(ThemeService Service, AppData Data, FakeThemeInterop Interop, FaultInjectingDataStore Store)> StartAsync()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await AppData.OpenAsync(store);
        var interop = new FakeThemeInterop();
        var service = new ThemeService(data, interop);
        await service.InitialiseAsync();
        return (service, data, interop, store);
    }

    [Fact]
    public async Task Defaults_to_Keep_and_System_and_applies_them_on_start()
    {
        var (service, _, interop, _) = await StartAsync();

        Assert.Equal(Theme.Keep, service.Theme);
        Assert.Equal(Mode.System, service.Mode);
        Assert.Equal([(Theme.Keep, Mode.System)], interop.Applied);
    }

    [Fact]
    public async Task Choosing_a_theme_applies_it_commits_it_and_raises_the_event()
    {
        var (service, data, interop, _) = await StartAsync();
        var raised = 0;
        service.Changed += () => raised++;

        await service.SetThemeAsync(Theme.Clean);

        Assert.Equal(Theme.Clean, service.Theme);
        Assert.Equal((Theme.Clean, Mode.System), interop.Applied[^1]);
        Assert.Equal(Theme.Clean, data.Snapshot.Settings.Theme);
        Assert.Equal(1, raised);
        Assert.Null(service.LastSaveFailure);
    }

    [Fact]
    public async Task Choosing_a_mode_keeps_the_theme_and_the_other_settings()
    {
        var (service, data, interop, _) = await StartAsync();
        await service.SetThemeAsync(Theme.Clean);

        await service.SetModeAsync(Mode.Light);

        Assert.Equal((Theme.Clean, Mode.Light), interop.Applied[^1]);
        Assert.Equal(Settings.Default with { Theme = Theme.Clean, Mode = Mode.Light }, data.Snapshot.Settings);
    }

    [Fact]
    public async Task A_failed_save_still_applies_the_choice_and_reports_the_failure()
    {
        var (service, data, interop, store) = await StartAsync();
        store.FailAllWrites(StoreFailureKind.StorageFull);

        await service.SetModeAsync(Mode.Dark);

        Assert.Equal(Mode.Dark, service.Mode);
        Assert.Equal((Theme.Keep, Mode.Dark), interop.Applied[^1]);
        Assert.Equal(Mode.System, data.Snapshot.Settings.Mode);
        var failed = Assert.IsType<CommitResult.StoreFailed>(service.LastSaveFailure);
        Assert.Equal(StoreFailureKind.StorageFull, failed.Failure.Kind);

        store.Recover();
        await service.SetModeAsync(Mode.Dark);
        Assert.Null(service.LastSaveFailure);
    }

    [Fact]
    public async Task An_unrelated_commit_after_a_failed_save_does_not_revert_the_choice()
    {
        var (service, data, _, store) = await StartAsync();
        store.FailAllWrites(StoreFailureKind.StorageFull);
        await service.SetModeAsync(Mode.Dark);
        store.Recover();

        await data.CommitAsync(Change.Put(data.Snapshot.Settings with { RestTimerSeconds = 60 }));

        Assert.Equal(Mode.Dark, service.Mode);
    }

    [Fact]
    public async Task A_failing_interop_never_throws_into_the_caller()
    {
        var (service, data, interop, _) = await StartAsync();
        interop.Throw = true;

        await service.SetThemeAsync(Theme.Clean);

        Assert.Equal(Theme.Clean, data.Snapshot.Settings.Theme);
    }

    [Fact]
    public async Task Settings_that_change_underneath_re_apply_and_raise_the_event()
    {
        var (service, data, interop, _) = await StartAsync();
        var raised = 0;
        service.Changed += () => raised++;

        // What a Backup restore does: Settings change without going through the service.
        await data.CommitAsync(Change.Put(data.Snapshot.Settings with { Theme = Theme.Clean, Mode = Mode.Light }));

        Assert.Equal((Theme.Clean, Mode.Light), (service.Theme, service.Mode));
        Assert.Equal((Theme.Clean, Mode.Light), interop.Applied[^1]);
        Assert.Equal(1, raised);
    }
}

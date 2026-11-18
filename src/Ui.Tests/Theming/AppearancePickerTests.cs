using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ui.Theming;

namespace Ui.Tests.Theming;

public sealed class AppearancePickerTests : BunitContext
{
    private sealed class RecordingInterop : IThemeInterop
    {
        public List<(Theme, Mode)> Applied { get; } = [];

        public Task ApplyAsync(Theme theme, Mode mode)
        {
            Applied.Add((theme, mode));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Offers_every_theme_and_mode_under_a_human_label()
    {
        var data = await AppData.OpenAsync(new InMemoryDataStore());
        Services.AddSingleton(data);
        Services.AddSingleton(new ThemeService(data, new RecordingInterop(), new Ui.Saving.SaveFailureNotice()));

        var cut = Render<AppearancePicker>();

        Assert.Equal(["Keep", "Clean", "Match phone", "Dark", "Light"], cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public async Task Tapping_a_choice_restyles_marks_it_pressed_and_saves_it()
    {
        var data = await AppData.OpenAsync(new InMemoryDataStore());
        var interop = new RecordingInterop();
        Services.AddSingleton(data);
        Services.AddSingleton(new ThemeService(data, interop, new Ui.Saving.SaveFailureNotice()));

        var cut = Render<AppearancePicker>();
        Assert.Equal("true", cut.FindAll("button").Single(b => b.TextContent == "Keep").GetAttribute("aria-pressed"));

        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent == "Clean").Click());
        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent == "Light").Click());

        Assert.Equal("true", cut.FindAll("button").Single(b => b.TextContent == "Clean").GetAttribute("aria-pressed"));
        Assert.Equal("true", cut.FindAll("button").Single(b => b.TextContent == "Light").GetAttribute("aria-pressed"));
        Assert.Equal((Theme.Clean, Mode.Light), interop.Applied[^1]);
        Assert.Equal((Theme.Clean, Mode.Light), (data.Snapshot.Settings.Theme, data.Snapshot.Settings.Mode));
    }
}

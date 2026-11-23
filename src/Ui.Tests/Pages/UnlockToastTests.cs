using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Accessibility;
using Ui.Components;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The UnlockToast (C-23) on the finish summary: shown for newly unlocked Badges, auto-hides after 4 s, a tap opens the Badge.</summary>
public sealed class UnlockToastTests : BunitContext
{
    private async Task<IRenderedComponent<FinishSummary>> RenderSummaryAsync(bool unlocks)
    {
        var done = InProgress("w-now", new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("n1", 25, 8)])) with { EndedAt = Now.AddMinutes(30) };
        var data = await OpenSeededAsync(plan: Plan(), workouts: [Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8)), done]);
        await data.CommitAsync(Change.Put(new Badge("first-two", "Twice over", "sessions", unlocks ? 2 : 50)));
        Wire(this, data);
        return Render<FinishSummary>(p => p.Add(c => c.Workout, done).Add(c => c.Data, data.Snapshot).Add(c => c.Today, Today));
    }

    private async Task TickAsync(IRenderedComponent<FinishSummary> cut, int times)
    {
        var ticker = Services.GetRequiredService<FakeTicker>();
        for (var i = 0; i < times; i++)
        {
            cut.WaitForState(() => ticker.Waiting == 1);
            ticker.Tick();
            await Task.Yield();
        }
    }

    [Fact]
    public async Task A_newly_unlocked_Badge_shows_a_toast_that_links_to_the_Badge()
    {
        var cut = await RenderSummaryAsync(unlocks: true);

        var toast = cut.Find(".fa-unlock");
        Assert.Contains("Twice over", toast.TextContent);
        Assert.Equal("trophies?badge=first-two", toast.GetAttribute("href"));
    }

    [Fact]
    public async Task A_newly_unlocked_Badge_is_announced_once_through_the_shared_region_not_by_the_toast()
    {
        var cut = await RenderSummaryAsync(unlocks: true);

        Assert.Equal("Badge unlocked: Twice over", Services.GetRequiredService<Announcer>().Message);
        Assert.Null(cut.Find(".fa-unlock").GetAttribute("role"));
    }

    [Fact]
    public async Task A_toast_created_again_for_the_same_unlock_does_not_announce_it_again()
    {
        var announced = 0;
        var cut = await RenderSummaryAsync(unlocks: true);
        Services.GetRequiredService<Announcer>().Announced += () => announced++;
        var summary = cut.Instance;

        // The summary is shown again (re-opened by its URL, or the page re-created): the toast is a new component for the same unlock.
        await DisposeComponentsAsync();
        Render<FinishSummary>(p => p.Add(c => c.Workout, summary.Workout).Add(c => c.Data, summary.Data).Add(c => c.Today, Today));

        Assert.Equal(0, announced);
    }

    [Fact]
    public async Task Nothing_unlocked_shows_no_toast()
    {
        var cut = await RenderSummaryAsync(unlocks: false);

        Assert.Empty(cut.FindAll(".fa-unlock"));
    }

    [Fact]
    public async Task The_toast_hides_itself_after_four_seconds_and_not_before()
    {
        var cut = await RenderSummaryAsync(unlocks: true);

        await TickAsync(cut, 3);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".fa-unlock")));

        await TickAsync(cut, 1);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".fa-unlock")));
    }

    [Fact]
    public async Task The_toast_makes_no_sound_or_haptics_and_leaves_no_wait_running_once_disposed()
    {
        var cut = await RenderSummaryAsync(unlocks: true);
        var ticker = Services.GetRequiredService<FakeTicker>();
        cut.WaitForState(() => ticker.Waiting == 1);

        Assert.DoesNotContain("audio", cut.Markup, StringComparison.OrdinalIgnoreCase);
        await DisposeComponentsAsync();

        for (var i = 0; i < 50 && ticker.Waiting != 0; i++) await Task.Delay(20);
        Assert.Equal(0, ticker.Waiting);
    }

    [Fact]
    public void Reduced_motion_switches_the_fade_off_through_the_global_rule()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "src", "Ui"))) dir = Path.GetDirectoryName(dir);
        var toastCss = File.ReadAllText(Path.Combine(dir!, "src", "Ui", "Components", "UnlockToast.razor.css"));
        var globalCss = File.ReadAllText(Path.Combine(dir!, "src", "Ui", "wwwroot", "ui.css"));

        Assert.Contains("200ms", toastCss);
        Assert.Contains("8px", toastCss);
        Assert.Matches(@"prefers-reduced-motion: reduce\)\s*\{\s*\*, \*::before, \*::after\s*\{\s*animation: none !important", globalCss);
    }
}

using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Accessibility;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Guide sheet, the rest timer in the ConfirmBar and the Wake Lock on the log page (design 8.2, 8.4, C-06).</summary>
public sealed class WorkoutLogGuideRestTests : BunitContext
{
    private readonly FakeDevicePreferences _preferences = new();

    private static Guide SquatGuide(string? videoUrl = "https://example.com/squat") =>
        new(Squat, "Quads and glutes", "Builds the leg drive for Hyrox", null, "Sit between the heels, chest up", ["Knees out", "Brace first"],
            ["Heels lifting"], null, null, ["Back pain: stop"], videoUrl, null);

    private async Task<IRenderedComponent<WorkoutLog>> OpenAsync(Guide? guide = null, string? restSeconds = null)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        if (guide is not null)
        {
            Assert.IsType<CommitResult.Committed>(await data.CommitAsync([Change.Put(guide)]));
        }

        if (restSeconds is not null)
        {
            _preferences.Values["rest-timer-seconds"] = restSeconds;
        }

        Wire(this, data, new SaveFailureNotice(), _preferences);
        return Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));
    }

    private static IElement Weight(IRenderedComponent<WorkoutLog> cut, int set) => cut.Find($"input[aria-label='Weight, Set {set}']");

    private static IElement Reps(IRenderedComponent<WorkoutLog> cut, int set) => cut.Find($"input[aria-label='Reps, Set {set}']");

    /// <summary>
    /// Confirms the Set and waits until the page shows it done. Confirm saves first and only then starts the countdown, and a click
    /// returns before that save has finished, so a test that read the screen straight after the click could see the previous render
    /// (the flaky "Rest 1:00" this used to give under load). The done row appears in the render that follows the countdown start.
    /// </summary>
    private static void ConfirmSet(IRenderedComponent<WorkoutLog> cut, int set)
    {
        Weight(cut, set).Input("50");
        Reps(cut, set).Input("8");
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();
        cut.WaitForState(() => cut.FindAll(".fa-setrow--done").Count == set);
    }

    private static void OpenGuide(IRenderedComponent<WorkoutLog> cut)
    {
        cut.Find("button[aria-label='Menu for Goblet squat']").Click();
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains("View Guide")).Click();
    }

    private static string? Rest(IRenderedComponent<WorkoutLog> cut) => cut.FindAll(".fa-confirmbar__rest").SingleOrDefault()?.TextContent.Trim();

    private void Pass(TimeSpan time)
    {
        Services.GetRequiredService<TestClock>().Advance(time);
        Services.GetRequiredService<FakeTicker>().Tick();
    }

    // ---- Guide sheet

    [Fact]
    public async Task View_Guide_opens_one_sheet_with_the_Guide_content_the_Exercise_has()
    {
        var cut = await OpenAsync(SquatGuide());

        OpenGuide(cut);

        var dialog = Assert.Single(cut.FindAll("[role='dialog']"));
        Assert.Equal("Goblet squat Guide", dialog.QuerySelector(".fa-sheet__title")!.TextContent);
        foreach (var text in new[] { "Quads and glutes", "Builds the leg drive for Hyrox", "Sit between the heels, chest up", "Knees out", "Brace first", "Heels lifting", "Back pain: stop" })
        {
            Assert.Contains(text, dialog.TextContent);
        }
    }

    [Fact]
    public async Task An_Exercise_with_no_Guide_says_so_plainly()
    {
        var cut = await OpenAsync();

        OpenGuide(cut);

        Assert.Contains("No Guide yet", cut.Find("[role='dialog']").TextContent);
    }

    [Fact]
    public async Task The_video_link_opens_in_a_new_tab_and_says_so()
    {
        var cut = await OpenAsync(SquatGuide());

        OpenGuide(cut);

        var link = cut.Find("[role='dialog'] a.fa-guide__video");
        Assert.Equal("https://example.com/squat", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Contains("noopener", link.GetAttribute("rel"));
        Assert.Contains("opens in a new tab", link.TextContent);
    }

    [Fact]
    public async Task A_video_address_that_is_not_a_web_address_is_not_offered_as_a_link()
    {
        var cut = await OpenAsync(SquatGuide("javascript:alert(1)"));

        OpenGuide(cut);

        Assert.Empty(cut.FindAll("[role='dialog'] a.fa-guide__video"));
    }

    [Fact]
    public async Task Back_to_the_Set_closes_the_sheet_and_loses_nothing()
    {
        var cut = await OpenAsync(SquatGuide());
        cut.FindAll(".pager__chip")[1].Click();
        cut.FindAll(".pager__chip")[0].Click();
        Weight(cut, 2).Input("42");

        OpenGuide(cut);
        cut.FindAll("[role='dialog'] .fa-sheet__actions button").Single(b => b.TextContent.Trim() == "Back to the Set").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal("42", Weight(cut, 2).GetAttribute("value"));
        Assert.Equal("Goblet squat", cut.Find(".fa-exercise__name").TextContent);
    }

    // ---- Round trip to the full Guide

    private void LeaveLog(string target)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(target);
        DisposeComponentsAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Leaving_to_Home_and_reopening_starts_at_the_first_incomplete_Exercise()
    {
        var cut = await OpenAsync(SquatGuide());
        cut.FindAll(".pager__chip")[1].Click();
        var second = cut.Find(".fa-exercise__name").TextContent;

        LeaveLog("");
        var back = Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        Assert.NotEqual(second, back.Find(".fa-exercise__name").TextContent);
        Assert.Equal("Goblet squat", back.Find(".fa-exercise__name").TextContent);
    }

    [Fact]
    public async Task Going_from_the_Guide_to_Home_drops_what_was_kept()
    {
        var cut = await OpenAsync(SquatGuide());
        cut.FindAll(".pager__chip")[1].Click();
        var second = cut.Find(".fa-exercise__name").TextContent;

        LeaveLog($"exercises/{Squat}/guide");
        Services.GetRequiredService<NavigationManager>().NavigateTo("");
        var back = Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        Assert.NotEqual(second, back.Find(".fa-exercise__name").TextContent);
    }

    // ---- Rest timer

    [Fact]
    public async Task With_the_rest_timer_off_confirming_shows_no_countdown()
    {
        var cut = await OpenAsync();

        ConfirmSet(cut, 1);

        Assert.Null(Rest(cut));
    }

    [Fact]
    public async Task Confirming_a_Set_starts_the_countdown_which_counts_down_from_the_clock()
    {
        var cut = await OpenAsync(restSeconds: "90");
        Assert.Null(Rest(cut));

        ConfirmSet(cut, 1);
        Assert.Equal("Rest 1:30", Rest(cut));

        Pass(TimeSpan.FromSeconds(18));
        cut.WaitForAssertion(() => Assert.Equal("Rest 1:12", Rest(cut)));
    }

    [Fact]
    public async Task A_countdown_that_passed_while_ticks_were_missed_still_reads_the_clock()
    {
        var cut = await OpenAsync(restSeconds: "90");
        ConfirmSet(cut, 1);

        Pass(TimeSpan.FromSeconds(75)); // one tick after the phone slept

        cut.WaitForAssertion(() => Assert.Equal("Rest 0:15", Rest(cut)));
    }

    [Fact]
    public async Task Confirming_another_Set_restarts_the_countdown()
    {
        var cut = await OpenAsync(restSeconds: "90");
        ConfirmSet(cut, 1);
        Pass(TimeSpan.FromSeconds(30));
        cut.WaitForAssertion(() => Assert.Equal("Rest 1:00", Rest(cut)));

        ConfirmSet(cut, 2);

        Assert.Equal("Rest 1:30", Rest(cut));
    }

    [Fact]
    public async Task Reaching_zero_clears_the_countdown_quietly()
    {
        var cut = await OpenAsync(restSeconds: "60");
        ConfirmSet(cut, 1);

        Pass(TimeSpan.FromSeconds(60));

        cut.WaitForAssertion(() => Assert.Null(Rest(cut)));
        Assert.Equal(0, Services.GetRequiredService<FakeTicker>().Waiting);
        Assert.Empty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public async Task Reaching_zero_announces_rest_over_once_and_not_each_second()
    {
        var cut = await OpenAsync(restSeconds: "60");
        var announced = new List<string>();
        var announcer = Services.GetRequiredService<Announcer>();
        announcer.Announced += () => announced.Add(announcer.Message!);
        ConfirmSet(cut, 1);

        Pass(TimeSpan.FromSeconds(30));
        cut.WaitForAssertion(() => Assert.Equal("Rest 0:30", Rest(cut)));
        Assert.Empty(announced);
        Assert.Equal("off", cut.Find(".fa-confirmbar__rest").GetAttribute("aria-live"));

        Pass(TimeSpan.FromSeconds(30));
        cut.WaitForAssertion(() => Assert.Null(Rest(cut)));

        Assert.Equal(["Rest over"], announced);
    }

    [Fact]
    public async Task A_countdown_cut_short_by_another_Set_or_Off_announces_nothing()
    {
        var cut = await OpenAsync(restSeconds: "90");
        var announcer = Services.GetRequiredService<Announcer>();
        ConfirmSet(cut, 1);
        ConfirmSet(cut, 2);

        cut.Find("button[aria-label='Workout menu']").Click();
        cut.FindAll("[role='dialog'] [role='group'][aria-label='Rest timer'] button").Single(b => b.TextContent.Trim() == "Off").Click();

        Assert.Null(Rest(cut));
        Assert.Null(announcer.Message);
    }

    [Fact]
    public async Task A_Set_that_is_not_valid_does_not_start_the_countdown()
    {
        var cut = await OpenAsync(restSeconds: "90");
        Assert.Null(Rest(cut));

        Weight(cut, 1).Input("abc");
        Reps(cut, 1).Input("8");
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();

        Assert.Null(Rest(cut));
    }

    [Fact]
    public async Task Choosing_Off_while_counting_down_clears_it()
    {
        var cut = await OpenAsync(restSeconds: "90");
        ConfirmSet(cut, 1);
        Assert.NotNull(Rest(cut));

        cut.Find("button[aria-label='Workout menu']").Click();
        cut.FindAll("[role='dialog'] [role='group'][aria-label='Rest timer'] button").Single(b => b.TextContent.Trim() == "Off").Click();

        Assert.Null(Rest(cut));
    }

    [Fact]
    public async Task Leaving_the_page_cancels_the_countdown()
    {
        var cut = await OpenAsync(restSeconds: "90");
        ConfirmSet(cut, 1);
        var ticker = Services.GetRequiredService<FakeTicker>();
        Assert.Equal(1, ticker.Waiting);

        await DisposeComponentsAsync();

        Assert.Equal(0, ticker.Waiting);
    }

    // ---- Wake Lock

    [Fact]
    public async Task The_screen_lock_is_acquired_when_the_log_opens_a_Workout_and_released_on_leaving()
    {
        var cut = await OpenAsync();
        var wakeLock = Services.GetRequiredService<RecordingWakeLock>();

        cut.WaitForAssertion(() => Assert.Equal(["acquire"], wakeLock.Calls));

        await DisposeComponentsAsync();

        Assert.Equal(["acquire", "release"], wakeLock.Calls);
    }

    [Fact]
    public async Task A_log_with_no_open_Workout_takes_no_screen_lock()
    {
        var data = await OpenSeededAsync(plan: Plan());
        Wire(this, data);

        Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "missing"));

        Assert.Empty(Services.GetRequiredService<RecordingWakeLock>().Calls);
    }
}

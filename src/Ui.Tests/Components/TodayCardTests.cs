using Bunit;
using Core.Model;
using TodayDoneSummary = Core.Rules.Home.TodayDoneSummary;
using Ui.Components;
using TodayCardModel = Core.Rules.Home.TodayCard;

namespace Ui.Tests.Components;

public sealed class TodayCardTests : BunitContext
{
    private static readonly DateOnly Wed = new(2026, 11, 18);
    private static readonly Session Lower = new("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
        [new SessionExercise("a", 3, 5, null, null), new SessionExercise("b", 3, 5, null, null)]);

    private IRenderedComponent<Ui.Components.TodayCard> Show(TodayCardModel card, Action<ComponentParameterCollectionBuilder<Ui.Components.TodayCard>>? more = null) =>
        Render<Ui.Components.TodayCard>(p =>
        {
            p.Add(c => c.Card, card).Add(c => c.Today, Wed);
            more?.Invoke(p);
        });

    private static string[] ButtonLabels(IRenderedComponent<Ui.Components.TodayCard> cut) =>
        [.. cut.FindAll("button, a.fa-btn").Select(b => b.TextContent.Trim())];

    [Fact]
    public void Start_shows_the_Session_and_only_a_Start_button()
    {
        var cut = Show(TodayCardModel.Start(Lower));

        Assert.Contains("Lower", cut.Markup);
        Assert.Contains("Strength · 2 Exercises", cut.Markup);
        Assert.Equal(["Start"], ButtonLabels(cut));
        Assert.NotNull(cut.Find("section[aria-label='Today']"));
    }

    [Fact]
    public void Start_raises_OnStart_with_the_Session()
    {
        Session? started = null;
        var cut = Show(TodayCardModel.Start(Lower), p => p.Add(c => c.OnStart, s => started = s));

        cut.Find("button").Click();

        cut.WaitForAssertion(() => Assert.Equal(Lower, started));
    }

    [Fact]
    public void Start_is_disabled_while_Starting()
    {
        var cut = Show(TodayCardModel.Start(Lower), p => p.Add(c => c.Starting, true));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Start_is_disabled_when_read_only_and_the_reason_is_shown()
    {
        var cut = Show(TodayCardModel.Start(Lower), p => p.Add(c => c.CanWrite, false).Add(c => c.ReadOnlyMessage, "Open in one window to save."));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.Contains("Open in one window to save.", cut.Markup);
    }

    [Fact]
    public void In_progress_shows_progress_and_minutes_and_Continue_even_when_read_only()
    {
        var workout = new Workout("w", Wed, "lower", false, null, null, [], [], null);
        Workout? continued = null;
        var cut = Show(TodayCardModel.InProgress(workout, 2, 5, TimeSpan.FromMinutes(31)),
            p => p.Add(c => c.CanWrite, false).Add(c => c.OnContinue, w => continued = w));

        Assert.Contains("In progress", cut.Markup);
        Assert.Contains("2 of 5 done · 31 min", cut.Markup);
        Assert.Equal(["Continue"], ButtonLabels(cut));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Equal(workout, continued));
    }

    [Fact]
    public void In_progress_without_elapsed_omits_the_minutes()
    {
        var workout = new Workout("w", Wed, "lower", true, null, null, [], [], null);

        var cut = Show(TodayCardModel.InProgress(workout, 1, 4, null));

        Assert.Contains("1 of 4 done", cut.Markup);
        Assert.DoesNotContain("min", cut.Find(".fa-todaycard__body").TextContent);
    }

    [Fact]
    public void Rest_day_names_the_next_Session_later_this_week()
    {
        var upper = Lower with { Name = "Upper" };

        var cut = Show(TodayCardModel.RestDay(upper, Wed.AddDays(2)));

        Assert.Contains("Rest day", cut.Markup);
        Assert.Contains("Next is Upper on Friday", cut.Markup);
        Assert.DoesNotContain("next week", cut.Markup);
        Assert.Empty(ButtonLabels(cut));
    }

    [Fact]
    public void Rest_day_says_next_week_for_a_later_week()
    {
        var full = Lower with { Name = "Full body" };

        var cut = Show(TodayCardModel.RestDay(full, new DateOnly(2026, 11, 23)));

        Assert.Contains("Next is Full body on Monday next week", cut.Markup);
    }

    [Fact]
    public void Rest_day_with_no_next_Session_is_just_Rest_day()
    {
        var cut = Show(TodayCardModel.RestDay());

        Assert.Contains("Rest day", cut.Markup);
        Assert.DoesNotContain("Next is", cut.Markup);
    }

    [Fact]
    public void Done_today_shows_duration_sets_prs_and_View_summary()
    {
        var raised = false;
        var cut = Show(TodayCardModel.DoneToday(new TodayDoneSummary("w", TimeSpan.FromMinutes(45), 12, 2, false)),
            p => p.Add(c => c.OnOpenSummary, () => raised = true));

        Assert.Contains("Done today", cut.Markup);
        Assert.Contains("45 min · 12 Sets · 2 PRs", cut.Markup);
        Assert.Equal(["View summary"], ButtonLabels(cut));
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.True(raised));
    }

    [Fact]
    public void Done_today_uses_singular_wording_omits_zero_prs_and_shows_Partial()
    {
        var one = Show(TodayCardModel.DoneToday(new TodayDoneSummary("w", null, 1, 1, true)));
        var none = Show(TodayCardModel.DoneToday(new TodayDoneSummary("w", TimeSpan.FromMinutes(30), 3, 0, false)));

        Assert.Contains("1 Set · 1 PR · Partial", one.Markup);
        Assert.DoesNotContain("min", one.Find(".fa-todaycard__body").TextContent);
        Assert.DoesNotContain("PR", none.Find(".fa-todaycard__body").TextContent);
    }

    [Fact]
    public void Break_Plan_finished_and_Not_started_have_no_buttons()
    {
        var onBreak = Show(TodayCardModel.Break());
        var finished = Show(TodayCardModel.PlanFinished());
        var notStarted = Show(TodayCardModel.NotStarted(new DateOnly(2026, 11, 30)));

        Assert.Contains("On a Break", onBreak.Markup);
        Assert.Contains("Your Plan has finished", finished.Markup);
        Assert.Contains("Start a new Plan to keep going.", finished.Markup);
        Assert.Contains("Your Plan starts on Mon 30 Nov", notStarted.Markup);
        Assert.Empty(ButtonLabels(onBreak));
        Assert.Empty(ButtonLabels(finished));
        Assert.Empty(ButtonLabels(notStarted));
    }

    [Fact]
    public void No_plan_shows_the_empty_state()
    {
        var cut = Show(TodayCardModel.NoPlan());

        Assert.Contains("No active Plan", cut.Markup);
        Assert.Contains("Make a Plan active and today's Session will start from here.", cut.Markup);
        Assert.Empty(ButtonLabels(cut));
    }
}

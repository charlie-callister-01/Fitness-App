using Bunit;
using Core.Model;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class LogPastWorkoutSheetTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 11, 18);
    private static readonly Session Lower = new("lower", "L: Lower", DayOfWeek.Monday, SessionType.Strength, null, []);

    public LogPastWorkoutSheetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<LogPastWorkoutSheet> Open(Action<ComponentParameterCollectionBuilder<LogPastWorkoutSheet>>? more = null) =>
        Render<LogPastWorkoutSheet>(p =>
        {
            p.Add(s => s.Open, true).Add(s => s.Today, Today).Add(s => s.Sessions, [Lower]);
            more?.Invoke(p);
        });

    private static void Choose(IRenderedComponent<LogPastWorkoutSheet> cut, string name) =>
        cut.FindAll(".fa-pastrow").Single(b => b.TextContent.Contains(name)).Click();

    [Fact]
    public void The_date_defaults_to_yesterday_and_cannot_exceed_today()
    {
        var cut = Open();

        var input = cut.Find("input[type='date']");
        Assert.Equal("2026-11-17", input.GetAttribute("value"));
        Assert.Equal("2026-11-18", input.GetAttribute("max"));
    }

    [Fact]
    public void Picking_a_Session_raises_OnStart_with_the_date_and_Session()
    {
        (DateOnly Date, Session? Session)? got = null;
        var cut = Open(p => p.Add(s => s.OnStart, x => got = x));

        Choose(cut, "L: Lower");

        cut.WaitForAssertion(() => Assert.Equal((new DateOnly(2026, 11, 17), Lower), got));
    }

    [Fact]
    public void Custom_workout_raises_OnStart_with_a_null_Session()
    {
        (DateOnly Date, Session? Session)? got = null;
        var cut = Open(p => p.Add(s => s.OnStart, x => got = x));
        cut.Find("input[type='date']").Change("2026-11-10");

        Choose(cut, "Custom workout");

        cut.WaitForAssertion(() => Assert.Equal((new DateOnly(2026, 11, 10), (Session?)null), got));
    }

    [Fact]
    public void A_blank_date_shows_the_message_and_raises_nothing()
    {
        var raised = false;
        var cut = Open(p => p.Add(s => s.OnStart, _ => raised = true));
        cut.Find("input[type='date']").Change("");

        Choose(cut, "L: Lower");

        cut.WaitForAssertion(() => Assert.Contains("Pick the day it happened.", cut.Markup));
        Assert.False(raised);
    }

    [Fact]
    public void A_future_date_is_refused_with_the_message()
    {
        var raised = false;
        var cut = Open(p => p.Add(s => s.OnStart, _ => raised = true));
        cut.Find("input[type='date']").Change("2026-11-19");

        Choose(cut, "Custom workout");

        cut.WaitForAssertion(() => Assert.Contains("That day hasn't happened yet.", cut.Markup));
        Assert.False(raised);
    }

    [Fact]
    public void A_message_from_the_owner_is_shown()
    {
        var cut = Open(p => p.Add(s => s.Message, "That day hasn't happened yet."));

        Assert.Contains("That day hasn't happened yet.", cut.Find("[role='alert']").TextContent);
    }

    [Fact]
    public void Read_only_disables_the_choices_and_shows_the_reason()
    {
        var cut = Open(p => p.Add(s => s.CanWrite, false).Add(s => s.ReadOnlyMessage, "Open in one window to save."));

        Assert.All(cut.FindAll(".fa-pastrow"), r => Assert.True(r.HasAttribute("disabled")));
        Assert.Contains("Open in one window to save.", cut.Markup);
    }

    [Fact]
    public void Cancel_raises_OnClose_and_the_sheet_is_a_labelled_dialog()
    {
        var closed = false;
        var cut = Open(p => p.Add(s => s.OnClose, () => closed = true));

        Assert.Contains("Log a past Workout", cut.Find("[role='dialog'] h2").TextContent);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        cut.WaitForAssertion(() => Assert.True(closed));
    }
}

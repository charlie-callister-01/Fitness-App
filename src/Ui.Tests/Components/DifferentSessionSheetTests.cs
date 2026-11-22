using Bunit;
using Core.Model;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class DifferentSessionSheetTests : BunitContext
{
    private static readonly Session Lower = new("lower", "L: Lower", DayOfWeek.Monday, SessionType.Strength, null, []);
    private static readonly Session Run = new("run", "Easy run", DayOfWeek.Friday, SessionType.Cardio, null, []);

    public DifferentSessionSheetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<DifferentSessionSheet> Open(Action<ComponentParameterCollectionBuilder<DifferentSessionSheet>>? more = null, bool empty = false) =>
        Render<DifferentSessionSheet>(p =>
        {
            p.Add(s => s.Open, true).Add(s => s.Sessions, empty ? [] : [Lower, Run]);
            more?.Invoke(p);
        });

    [Fact]
    public void Rows_show_code_name_type_and_day()
    {
        var cut = Open();

        var rows = cut.FindAll(".fa-sessionrow");
        Assert.Equal(2, rows.Count);
        Assert.Equal("L", rows[0].QuerySelector(".fa-sessionrow__code")!.TextContent.Trim());
        Assert.Contains("L: Lower", rows[0].TextContent);
        Assert.Contains("Strength", rows[0].TextContent);
        Assert.Contains("Mon", rows[0].TextContent);
        Assert.Equal("E", rows[1].QuerySelector(".fa-sessionrow__code")!.TextContent.Trim());
        Assert.Contains("Cardio", rows[1].TextContent);
        Assert.Contains("Fri", rows[1].TextContent);
    }

    [Fact]
    public void Tapping_a_row_raises_OnPick_with_that_Session()
    {
        Session? picked = null;
        var cut = Open(p => p.Add(s => s.OnPick, s => picked = s));

        cut.FindAll(".fa-sessionrow")[1].Click();

        cut.WaitForAssertion(() => Assert.Equal(Run, picked));
    }

    [Fact]
    public void An_empty_list_offers_Build_a_Custom_workout()
    {
        var built = false;
        var cut = Open(p => p.Add(s => s.OnBuildCustom, () => built = true), empty: true);

        Assert.Contains("No Sessions left this week.", cut.Markup);
        Assert.Empty(cut.FindAll(".fa-sessionrow"));
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Build a Custom workout").Click();

        cut.WaitForAssertion(() => Assert.True(built));
    }

    [Fact]
    public void Saving_disables_the_rows()
    {
        var cut = Open(p => p.Add(s => s.Saving, true));

        Assert.All(cut.FindAll(".fa-sessionrow"), r => Assert.True(r.HasAttribute("disabled")));
    }

    [Fact]
    public void Read_only_disables_the_rows_and_shows_the_reason()
    {
        var cut = Open(p => p.Add(s => s.CanWrite, false).Add(s => s.ReadOnlyMessage, "Open in one window to save."));

        Assert.All(cut.FindAll(".fa-sessionrow"), r => Assert.True(r.HasAttribute("disabled")));
        Assert.Contains("Open in one window to save.", cut.Markup);
    }

    [Fact]
    public void Cancel_raises_OnClose_and_the_sheet_is_a_labelled_dialog()
    {
        var closed = false;
        var cut = Open(p => p.Add(s => s.OnClose, () => closed = true));

        Assert.Contains("Do a different Session", cut.Find("[role='dialog'] h2").TextContent);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        cut.WaitForAssertion(() => Assert.True(closed));
    }
}

using Bunit;
using Microsoft.AspNetCore.Components;
using Core.Model;
using Core.Rules.Insight;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class CalendarHeatmapTests : BunitContext
{
    private static readonly DateOnly Mon = new(2026, 11, 16);

    private static CalendarDay Day(int offset, CalendarDayState state, SessionType? type = null, int prs = 0, bool today = false, string? workout = null) =>
        new(Mon.AddDays(offset), state, type, prs, today, $"label-{offset}", workout);

    private static CalendarView View(params CalendarDay[] days) =>
        new([new CalendarWeek(Mon, 1, days), new CalendarWeek(Mon.AddDays(7), 2, [Day(7, CalendarDayState.Future)])],
            [new CalendarPhase("Base", 1, 2)], true);

    private IRenderedComponent<CalendarHeatmap> Show(CalendarView view, Action<string>? opened = null) =>
        Render<CalendarHeatmap>(p => p.Add(c => c.View, view)
            .Add(c => c.OnOpenWorkout, EventCallback.Factory.Create<string>(this, id => opened?.Invoke(id))));

    [Theory]
    [InlineData(CalendarDayState.Done, "fa-heat__cell--done")]
    [InlineData(CalendarDayState.Partial, "fa-heat__cell--partial")]
    [InlineData(CalendarDayState.Missed, "fa-heat__cell--missed")]
    [InlineData(CalendarDayState.BreakDay, "fa-heat__cell--break")]
    [InlineData(CalendarDayState.Future, "fa-heat__cell--future")]
    [InlineData(CalendarDayState.Empty, "fa-heat__cell--empty")]
    public void A_cell_state_maps_to_its_style_class_and_one_accessible_label(CalendarDayState state, string cssClass)
    {
        var cut = Show(View(Day(0, state)));

        var cell = cut.Find(".fa-heat__grid [aria-label='label-0']");
        Assert.Contains(cssClass, cell.ClassList);
    }

    [Fact]
    public void Done_cells_take_the_Session_type_colour_class_and_PR_and_today_are_marked()
    {
        var cut = Show(View(Day(0, CalendarDayState.Done, SessionType.Cardio, prs: 1, today: true)));

        var cell = cut.Find("[aria-label='label-0']");
        Assert.Contains("fa-heat__cell--cardio", cell.ClassList);
        Assert.Contains("fa-heat__cell--pr", cell.ClassList);
        Assert.Contains("fa-heat__cell--today", cell.ClassList);
    }

    [Fact]
    public void Tapping_a_day_with_a_Workout_opens_it_and_a_day_without_is_not_a_button()
    {
        string? opened = null;
        var cut = Show(View(Day(0, CalendarDayState.Done, workout: "w9"), Day(1, CalendarDayState.Missed)), id => opened = id);

        cut.Find("button[aria-label='label-0']").Click();

        Assert.Equal("w9", opened);
        Assert.Empty(cut.FindAll("button[aria-label='label-1']"));
    }

    [Fact]
    public void The_Phase_strip_shows_name_and_length_and_the_legend_decodes_the_styles()
    {
        var cut = Show(View(Day(0, CalendarDayState.Done)));

        Assert.Contains("Base · 2 weeks", cut.Find(".fa-heat__phases").TextContent);
        var legend = cut.Find(".fa-heat__legend").TextContent;
        foreach (var word in new[] { "Done", "Partial", "Missed", "Break", "PR", "Today" })
            Assert.Contains(word, legend);
    }
}

using Core.Model;
using Core.Rules.Home;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Home;

public class WeightPanelRulesTests
{
    private static WeighIn Entry(string id, DateOnly date, double kg) => new(id, date, kg);

    [Fact]
    public void Compute_with_no_weigh_ins_returns_all_null()
    {
        var panel = WeightPanelRules.Compute([], Dates.Mon);

        Assert.Null(panel.Latest);
        Assert.Null(panel.WeekAverageKg);
        Assert.Null(panel.ChangeKg);
    }

    [Fact]
    public void Compute_with_one_weigh_in_this_week_and_none_last_week_returns_average_and_null_change()
    {
        var w1 = Entry("w1", Dates.Mon.AddDays(1), 80.0);
        var panel = WeightPanelRules.Compute([w1], Dates.Mon);

        Assert.Equal(w1, panel.Latest);
        Assert.Equal(80.0, panel.WeekAverageKg);
        Assert.Null(panel.ChangeKg);
    }

    [Fact]
    public void Compute_with_both_weeks_present_calculates_change()
    {
        // Last week: Nov 10 (80kg) and Nov 12 (82kg) -> mean 81kg
        // This week: Nov 17 (82kg) and Nov 19 (84kg) -> mean 83kg
        var wLast1 = Entry("w1", Dates.Mon.AddDays(-6), 80.0);
        var wLast2 = Entry("w2", Dates.Mon.AddDays(-4), 82.0);
        var wThis1 = Entry("w3", Dates.Mon.AddDays(1), 82.0);
        var wThis2 = Entry("w4", Dates.Mon.AddDays(3), 84.0);

        var panel = WeightPanelRules.Compute([wLast1, wLast2, wThis1, wThis2], Dates.Mon);

        Assert.Equal(wThis2, panel.Latest);
        Assert.Equal(83.0, panel.WeekAverageKg);
        Assert.NotNull(panel.ChangeKg);
        Assert.Equal(2.0, panel.ChangeKg.Value, 4);
    }

    [Fact]
    public void Compute_change_is_negative_when_weight_decreased()
    {
        var wLast = Entry("w1", Dates.Mon.AddDays(-3), 85.0);
        var wThis = Entry("w2", Dates.Mon.AddDays(2), 83.0);

        var panel = WeightPanelRules.Compute([wLast, wThis], Dates.Mon);

        Assert.Equal(-2.0, panel.ChangeKg);
    }

    [Fact]
    public void Sunday_belongs_to_that_week_and_Monday_to_the_next()
    {
        // Dates.Mon is Monday 2026-11-16.
        // Last week: Mon 2026-11-09 to Sun 2026-11-15 (AddDays(-1)).
        // This week: Mon 2026-11-16 (Dates.Mon) to Sun 2026-11-22 (AddDays(6)).
        // Next week: Mon 2026-11-23 (AddDays(7)).
        var sunLastWeek = Entry("w1", Dates.Mon.AddDays(-1), 75.0);
        var monThisWeek = Entry("w2", Dates.Mon, 80.0);
        var sunThisWeek = Entry("w3", Dates.Mon.AddDays(6), 82.0);
        var monNextWeek = Entry("w4", Dates.Mon.AddDays(7), 90.0);

        var panel = WeightPanelRules.Compute([sunLastWeek, monThisWeek, sunThisWeek, monNextWeek], Dates.Mon);

        // This week mean: (80 + 82) / 2 = 81.0
        // Last week mean: 75.0
        // Change: 81.0 - 75.0 = 6.0
        Assert.Equal(81.0, panel.WeekAverageKg);
        Assert.NotNull(panel.ChangeKg);
        Assert.Equal(6.0, panel.ChangeKg.Value, 4);
        Assert.Equal(monNextWeek, panel.Latest);
    }

    [Fact]
    public void Latest_picks_the_weigh_in_with_the_latest_date()
    {
        var w1 = Entry("w1", Dates.Mon.AddDays(2), 80.0);
        var w2 = Entry("w2", Dates.Mon.AddDays(4), 81.0);
        var w3 = Entry("w3", Dates.Mon.AddDays(1), 82.0);

        var panel = WeightPanelRules.Compute([w1, w2, w3], Dates.Mon);

        Assert.Equal(w2, panel.Latest);
    }

    [Fact]
    public void Latest_picks_the_last_in_the_list_on_date_ties()
    {
        var w1 = Entry("w1", Dates.Mon.AddDays(3), 80.0);
        var w2 = Entry("w2", Dates.Mon.AddDays(3), 81.0);

        var panel = WeightPanelRules.Compute([w1, w2], Dates.Mon);

        Assert.Equal(w2, panel.Latest);
    }

    [Fact]
    public void LastLabelInfo_returns_latest_weigh_in()
    {
        Assert.Null(WeightPanelRules.LastLabelInfo([]));

        var w1 = Entry("w1", Dates.Mon.AddDays(1), 80.0);
        var w2 = Entry("w2", Dates.Mon.AddDays(3), 81.0);
        Assert.Equal(w2, WeightPanelRules.LastLabelInfo([w1, w2]));
    }
}

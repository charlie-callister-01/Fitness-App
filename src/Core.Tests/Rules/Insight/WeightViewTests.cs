using Core.Model;
using Core.Rules.Home;
using Core.Rules.Insight;
using Core.Rules.Units;

namespace Core.Tests.Rules.Insight;

public class WeightViewTests
{
    private static readonly UnitConverter Kg = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly UnitConverter Lb = new(WeightUnit.Lb, DistanceUnit.Mi, 2);
    private static readonly DateOnly Mon = new(2026, 11, 16);

    private static WeighIn W(string id, DateOnly date, double kg) => new(id, date, kg);

    private static WeightView Build(IReadOnlyList<WeighIn> w, IReadOnlyList<Badge>? badges = null, UnitConverter? units = null, DateOnly? today = null) =>
        WeightViewBuilder.Build(w, badges ?? [], today ?? Mon, units ?? Kg);

    [Fact]
    public void No_Weigh_ins_is_empty()
    {
        var view = Build([]);

        Assert.True(view.IsEmpty);
        Assert.Null(view.Now);
        Assert.Null(view.ChangeSinceStart);
        Assert.Null(view.WeekAverage);
        Assert.Empty(view.Weeks);
    }

    [Fact]
    public void One_entry_has_now_and_a_rolling_average_equal_to_it_but_no_change()
    {
        var view = Build([W("a", Mon, 90)]);

        Assert.Equal(90, view.Now);
        Assert.Null(view.ChangeSinceStart);
        Assert.Equal(90, view.Points[0].Rolling);
        Assert.True(view.Points[0].IsLowest);
    }

    [Fact]
    public void Fewer_than_seven_entries_average_what_exists()
    {
        var view = Build([W("a", Mon.AddDays(-2), 90), W("b", Mon.AddDays(-1), 88), W("c", Mon, 86)]);

        Assert.Equal([90.0, 89.0, 88.0], view.Points.Select(p => p.Rolling));
    }

    [Fact]
    public void Rolling_average_covers_the_last_seven_entries_across_gaps()
    {
        var entries = Enumerable.Range(0, 8).Select(i => W($"w{i}", Mon.AddDays(-40 + i * 5), 80 + i)).ToList();

        var view = Build(entries);

        // Entries 1..7 are 81..87, mean 84; the first entry has dropped out.
        Assert.Equal(84, view.Points[7].Rolling);
    }

    [Fact]
    public void Input_order_does_not_matter_and_ties_for_lowest_ring_the_earliest()
    {
        var view = Build([W("c", Mon, 85), W("a", Mon.AddDays(-5), 85), W("b", Mon.AddDays(-3), 90)]);

        Assert.Equal([Mon.AddDays(-5), Mon.AddDays(-3), Mon], view.Points.Select(p => p.Date));
        Assert.Equal([true, false, false], view.Points.Select(p => p.IsLowest));
    }

    [Fact]
    public void Change_since_start_is_latest_minus_first()
    {
        var view = Build([W("a", Mon.AddDays(-30), 92), W("b", Mon, 89.5)]);

        Assert.Equal(-2.5, view.ChangeSinceStart);
    }

    [Fact]
    public void Weekly_table_groups_Monday_to_Sunday_newest_first_and_skips_empty_weeks()
    {
        var view = Build(
        [
            W("a", Mon.AddDays(-15), 90), // Sun 1 Nov: week of 26 Oct
            W("b", Mon.AddDays(-14), 88), // Mon 2 Nov
            W("c", Mon.AddDays(-8), 86),  // Sun 8 Nov
            W("d", Mon, 84),
            W("e", Mon.AddDays(6), 82),   // Sun 22 Nov
        ]);

        Assert.Equal([Mon, Mon.AddDays(-14), Mon.AddDays(-21)], view.Weeks.Select(w => w.WeekStart));
        Assert.Equal([83.0, 87.0, 90.0], view.Weeks.Select(w => w.Average));
        Assert.Equal([2, 2, 1], view.Weeks.Select(w => w.Count));
    }

    [Fact]
    public void Goal_comes_from_the_first_bodyweight_Badge()
    {
        var badges = new[]
        {
            new Badge("2", "Other", "sessions", 10),
            new Badge("3", "Goal", "Bodyweight", 82),
            new Badge("4", "Later", "bodyweight", 80),
        };

        Assert.Equal(82, Build([W("a", Mon, 90)], badges).Goal);
        Assert.Null(Build([W("a", Mon, 90)], [badges[0]]).Goal);
    }

    [Fact]
    public void A_drop_Badge_puts_the_goal_at_the_first_Weigh_in_minus_the_drop()
    {
        var badges = new[] { new Badge("1", "Drop", "bodyweight", -5), new Badge("2", "Goal", "bodyweight", 82) };

        var view = Build([W("b", Mon.AddDays(7), 88), W("a", Mon, 90)], badges);

        Assert.Equal(85, view.Goal);
    }

    [Fact]
    public void A_drop_Badge_with_no_Weigh_ins_draws_no_goal()
    {
        Assert.Null(Build([], [new Badge("1", "Drop", "bodyweight", -5)]).Goal);
    }

    [Fact]
    public void A_drop_goal_follows_the_display_unit()
    {
        var view = Build([W("a", Mon, 90.72)], [new Badge("1", "Drop", "bodyweight", -4.536)], Lb);

        Assert.Equal(190, view.Goal!.Value, 1);
    }

    [Fact]
    public void Weights_are_shown_in_the_display_unit()
    {
        var view = Build([W("a", Mon, 86.18), W("b", Mon, 90.72)], [new Badge("g", "Goal", "bodyweight", 80)], Lb);

        Assert.Equal("lb", view.UnitLabel);
        Assert.Equal(200, view.Points[1].Value);
        Assert.Equal(Lb.DisplayWeight(88.45), view.Points[1].Rolling);
        Assert.Equal(Lb.DisplayWeight(80), view.Goal);
    }

    [Fact]
    public void Headline_numbers_agree_with_the_Home_weight_panel_on_the_same_data()
    {
        var entries = new[] { W("a", Mon.AddDays(-9), 91), W("b", Mon.AddDays(-2), 90), W("c", Mon, 88.2), W("d", Mon.AddDays(2), 89.1) };
        var today = Mon.AddDays(3);

        var panel = WeightPanelRules.Compute(entries, today);
        var view = Build(entries, today: today);

        Assert.Equal(Kg.DisplayWeight(panel.Latest!.WeightKg), view.Now);
        Assert.Equal(panel.Latest.Date, view.NowDate);
        Assert.Equal(Kg.DisplayWeight(panel.WeekAverageKg!.Value), view.WeekAverage);
        Assert.Equal(88.75, view.WeekAverage);
    }
}

using Core.Model;
using Core.Rules.PlanEditing;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.PlanEditing;

public class PlanProgressTests
{
    private static Phase Ph(int start, int end) => new($"p{start}", "Phase", start, end, [], []);

    [Fact]
    public void Total_weeks_is_the_last_week_of_the_last_Phase()
    {
        Assert.Equal(8, PlanProgress.TotalWeeks(PlanFixture.Make(start: Mon, phases: [Ph(5, 8), Ph(1, 4)])));
    }

    [Fact]
    public void With_no_Phases_total_weeks_runs_from_the_start_to_the_goal_date()
    {
        var plan = PlanFixture.Make(start: Mon) with { GoalDate = Mon.AddDays(7 * 4 + 2) };

        Assert.Equal(5, PlanProgress.TotalWeeks(plan));
    }

    [Fact]
    public void With_no_Phases_and_no_goal_date_total_weeks_is_unknown()
    {
        Assert.Null(PlanProgress.TotalWeeks(PlanFixture.Make(start: Mon)));
    }

    [Fact]
    public void The_active_label_says_week_n_of_m()
    {
        var plan = PlanFixture.Make(start: Mon, phases: [Ph(1, 27)]);

        Assert.Equal("Active · week 5 of 27", PlanProgress.ActiveLabel(plan, Mon.AddDays(7 * 4 + 3)));
    }

    [Fact]
    public void The_active_label_drops_the_total_when_it_is_unknown()
    {
        Assert.Equal("Active · week 1", PlanProgress.ActiveLabel(PlanFixture.Make(start: Mon), Mon));
    }

    [Fact]
    public void The_active_label_says_when_the_Plan_has_not_started_or_is_over()
    {
        var plan = PlanFixture.Make(start: Mon, phases: [Ph(1, 2)]);

        Assert.Equal("Active · starts 16 Nov 2026", PlanProgress.ActiveLabel(plan, Mon.AddDays(-1)));
        Assert.Equal("Active · finished after week 2", PlanProgress.ActiveLabel(plan, Mon.AddDays(14)));
    }

    [Fact]
    public void Total_weeks_and_active_label_read_only_the_main_Tracks_Phases()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(3, 2, false), [Ph(1, 8)]);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(75), [Ph(1, 20)]);
        var plan = PlanFixture.Make(start: Mon, tracks: [main, mobility]);
        var noMainPhases = PlanFixture.Make(start: Mon, tracks: [main with { Phases = [] }, mobility]);

        Assert.Equal(8, PlanProgress.TotalWeeks(plan));
        Assert.Equal("Active · week 5 of 8", PlanProgress.ActiveLabel(plan, Mon.AddDays(7 * 4)));
        Assert.Null(PlanProgress.TotalWeeks(noMainPhases));
        Assert.Equal("Active · week 5", PlanProgress.ActiveLabel(noMainPhases, Mon.AddDays(7 * 4)));
    }
}

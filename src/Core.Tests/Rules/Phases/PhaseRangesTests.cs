using Core.Model;
using Core.Rules.Phases;

namespace Core.Tests.Rules.Phases;

public class PhaseRangesTests
{
    private static Phase Ph(string id, int start, int end) => new(id, id, start, end, [], []);

    [Fact]
    public void Phases_that_follow_each_other_from_week_one_have_no_findings()
    {
        Assert.Empty(PhaseRanges.Check([Ph("b", 5, 8), Ph("a", 1, 4)]));
    }

    [Fact]
    public void No_Phases_have_no_findings()
    {
        Assert.Empty(PhaseRanges.Check([]));
    }

    [Fact]
    public void A_start_before_week_one_is_a_bad_start()
    {
        var finding = Assert.Single(PhaseRanges.Check([Ph("a", 0, 4)]));

        Assert.Equal(PhaseRangeProblem.BadStart, finding.Problem);
        Assert.Equal(0, finding.Index);
    }

    [Fact]
    public void An_end_before_the_start_is_reported_on_that_Phase()
    {
        var finding = Assert.Single(PhaseRanges.Check([Ph("a", 1, 4), Ph("b", 6, 5)]));

        Assert.Equal(PhaseRangeProblem.EndBeforeStart, finding.Problem);
        Assert.Equal(1, finding.Index);
    }

    [Fact]
    public void An_overlap_names_the_later_Phase_and_the_one_it_runs_into()
    {
        var finding = Assert.Single(PhaseRanges.Check([Ph("a", 1, 4), Ph("b", 4, 8)]));

        Assert.Equal(PhaseRangeProblem.Overlap, finding.Problem);
        Assert.Equal("b", finding.Phase.Id);
        Assert.Equal("a", finding.Earlier?.Id);
    }

    [Fact]
    public void A_gap_gives_the_missing_weeks_and_the_Phases_either_side()
    {
        var finding = Assert.Single(PhaseRanges.Check([Ph("a", 1, 4), Ph("b", 7, 8)]));

        Assert.Equal(PhaseRangeProblem.Gap, finding.Problem);
        Assert.Equal((5, 6), (finding.FirstWeek, finding.LastWeek));
        Assert.Equal("a", finding.Earlier?.Id);
        Assert.Equal("b", finding.Phase.Id);
    }

    [Fact]
    public void Weeks_before_the_first_Phase_are_a_leading_gap()
    {
        var finding = Assert.Single(PhaseRanges.Check([Ph("a", 3, 4)]));

        Assert.Equal(PhaseRangeProblem.LeadingGap, finding.Problem);
        Assert.Equal((1, 2), (finding.FirstWeek, finding.LastWeek));
        Assert.Null(finding.Earlier);
    }

    [Fact]
    public void A_Phase_with_a_range_problem_is_left_out_of_coverage_and_excuses_a_gap_it_starts_in()
    {
        var findings = PhaseRanges.Check([Ph("a", 1, 4), Ph("bad", 6, 5), Ph("c", 7, 8)]);

        Assert.Equal([PhaseRangeProblem.EndBeforeStart], findings.Select(f => f.Problem));
    }

    [Fact]
    public void An_unreadable_Phase_is_left_out_and_excuses_a_gap_before_the_next_one()
    {
        var unreadable = Ph("u", 0, 0);

        var findings = PhaseRanges.Check([Ph("a", 1, 4), unreadable, Ph("c", 7, 8)], p => p == unreadable);

        Assert.Empty(findings);
    }

    [Fact]
    public void Range_problems_come_before_coverage_problems()
    {
        var findings = PhaseRanges.Check([Ph("late", 5, 6), Ph("bad", 9, 8)]);

        Assert.Equal([PhaseRangeProblem.EndBeforeStart, PhaseRangeProblem.LeadingGap], findings.Select(f => f.Problem));
    }
}

using Core.Rules.Insight;

namespace Core.Tests.Rules.Insight;

public sealed class RangeResolverTests
{
    private static readonly DateOnly Today = new(2026, 11, 16);
    private static readonly DateOnly OldPlan = new(2026, 1, 5);

    [Fact]
    public void Last_12_weeks_is_84_days_ending_today()
    {
        var r = RangeResolver.Resolve(RangePreset.Last12Weeks, Today, OldPlan).Range!;
        Assert.Equal(new DateOnly(2026, 8, 25), r.From);
        Assert.Equal(Today, r.To);
    }

    [Fact]
    public void Last_4_weeks_is_28_days_ending_today()
    {
        Assert.Equal(new DateOnly(2026, 10, 20), RangeResolver.Resolve(RangePreset.Last4Weeks, Today, OldPlan).Range!.From);
    }

    [Fact]
    public void A_preset_ignores_a_Plan_start_later_than_its_own_start()
    {
        var plan = new DateOnly(2026, 11, 2);
        Assert.Equal(new DateOnly(2026, 8, 25), RangeResolver.Resolve(RangePreset.Last12Weeks, Today, plan).Range!.From);
        Assert.Equal(plan, RangeResolver.Resolve(RangePreset.WholePlan, Today, plan).Range!.From);
    }

    [Fact]
    public void Whole_Plan_runs_from_the_Plan_start_to_today()
    {
        var r = RangeResolver.Resolve(RangePreset.WholePlan, Today, OldPlan).Range!;
        Assert.Equal(OldPlan, r.From);
        Assert.Equal(Today, r.To);
    }

    [Fact]
    public void With_no_Plan_or_a_Plan_in_the_future_the_range_is_today()
    {
        Assert.Equal(Today, RangeResolver.Resolve(RangePreset.WholePlan, Today, null).Range!.From);
        Assert.Equal(Today, RangeResolver.Resolve(RangePreset.WholePlan, Today, Today.AddDays(10)).Range!.From);
    }

    [Fact]
    public void Custom_uses_the_dates_given_and_allows_one_day()
    {
        var d = new DateOnly(2026, 10, 1);
        var r = RangeResolver.Resolve(RangePreset.Custom, Today, OldPlan, d, d);
        Assert.True(r.IsValid);
        Assert.Equal(d, r.Range!.From);
        Assert.Equal(d, r.Range.To);
    }

    [Fact]
    public void Custom_with_start_after_end_is_refused_with_a_message()
    {
        var r = RangeResolver.Resolve(RangePreset.Custom, Today, OldPlan, new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1));
        Assert.False(r.IsValid);
        Assert.Equal(RangeResolver.StartAfterEnd, r.Error);
    }

    [Fact]
    public void Custom_with_a_missing_date_is_refused()
    {
        Assert.Equal(RangeResolver.MissingDate, RangeResolver.Resolve(RangePreset.Custom, Today, OldPlan, Today, null).Error);
    }

    [Fact]
    public void The_description_puts_the_range_in_words()
    {
        var r = RangeResolver.Resolve(RangePreset.Last12Weeks, Today, OldPlan).Range!;
        Assert.Equal("Last 12 weeks · 25 Aug – 16 Nov", RangeResolver.Describe(RangePreset.Last12Weeks, r));
    }
}

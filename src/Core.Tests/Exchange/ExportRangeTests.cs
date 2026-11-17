using Core.Exchange;

namespace Core.Tests.Exchange;

public class ExportRangeTests
{
    [Fact]
    public void Default_request_is_the_84_days_ending_today_inclusive_at_both_ends()
    {
        var request = ExportRequest.Default(ExchangeTestData.Today);

        Assert.Equal(new DateRange(new DateOnly(2026, 8, 26), new DateOnly(2026, 11, 17)), request.Range);
    }

    [Fact]
    public void Default_request_has_every_tick_on_except_Guides()
    {
        var request = ExportRequest.Default(ExchangeTestData.Today);

        Assert.True(request is { Workouts: true, Plan: true, WeighIns: true, PainChecks: true, Exercises: true });
        Assert.False(request.Guides);
        Assert.Equal(ExportFormat.Json, request.Format);
    }

    [Fact]
    public void Past_days_ends_today_and_covers_that_many_days()
    {
        var request = ExportRequest.PastDays(ExchangeTestData.Today, 7);

        Assert.Equal(new DateRange(new DateOnly(2026, 11, 11), new DateOnly(2026, 11, 17)), request.Range);
    }

    [Fact]
    public void Export_includes_Workouts_and_Weigh_ins_dated_on_both_ends_of_the_range_and_nothing_outside()
    {
        var request = ExportRequest.Default(ExchangeTestData.Today) with { Range = new DateRange(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 16)) };

        var result = Exporter.Export(ExchangeTestData.Snapshot(), request, "test", ExchangeTestData.Now);

        var document = ExchangeTestData.ReadBack(result.Text);
        Assert.Equal(["w-1", "w-2"], document.Workouts!.Select(w => w.Id).Order());
        Assert.Equal(["wi-1"], document.WeighIns!.Select(w => w.Id));
    }
}

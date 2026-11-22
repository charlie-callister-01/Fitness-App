using Core.Rules.Insight;

namespace Core.Tests.Rules.Insight;

public class InsightFormatTests
{
    private static readonly DateOnly Monday = new(2026, 1, 5);

    [Fact]
    public void Dates_read_the_same_way_everywhere_on_Insight()
    {
        Assert.Equal("Mon 5 Jan", InsightFormat.WeekdayDay(Monday));
        Assert.Equal("5 Jan", InsightFormat.DayMonth(Monday));
        Assert.Equal("5 Jan 2026", InsightFormat.DayMonthYear(Monday));
        Assert.Equal("2026-01-05", InsightFormat.Iso(Monday));
    }

    [Fact]
    public void Numbers_use_one_decimal_or_drop_trailing_zeros()
    {
        Assert.Equal("86.0", InsightFormat.OneDecimal(86));
        Assert.Equal("86.3", InsightFormat.OneDecimal(86.26));
        Assert.Equal("86", InsightFormat.Trimmed(86));
        Assert.Equal("86.25", InsightFormat.Trimmed(86.25));
    }
}

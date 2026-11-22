using Core.Rules.Formatting;

namespace Core.Tests.Rules.Formatting;

public class DisplayFormatTests
{
    private static readonly DateOnly Monday = new(2026, 1, 5);

    [Fact]
    public void Dates_read_the_same_way_everywhere_on_Insight()
    {
        Assert.Equal("Mon 5 Jan", DisplayFormat.WeekdayDay(Monday));
        Assert.Equal("5 Jan", DisplayFormat.DayMonth(Monday));
        Assert.Equal("5 Jan 2026", DisplayFormat.DayMonthYear(Monday));
        Assert.Equal("2026-01-05", DisplayFormat.Iso(Monday));
    }

    [Fact]
    public void Numbers_use_one_decimal_or_drop_trailing_zeros()
    {
        Assert.Equal("86.0", DisplayFormat.OneDecimal(86));
        Assert.Equal("86.3", DisplayFormat.OneDecimal(86.26));
        Assert.Equal("86", DisplayFormat.Trimmed(86));
        Assert.Equal("86.25", DisplayFormat.Trimmed(86.25));
    }

    [Fact]
    public void Rounded_drops_trailing_zeros_at_the_places_asked_for()
    {
        Assert.Equal("86.3", DisplayFormat.Rounded(86.3333, 1));
        Assert.Equal("86.25", DisplayFormat.Rounded(86.25));
        Assert.Equal("86", DisplayFormat.Rounded(86.001));
    }
}

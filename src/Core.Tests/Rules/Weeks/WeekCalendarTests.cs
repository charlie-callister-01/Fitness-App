using Core.Rules.Weeks;

namespace Core.Tests.Rules.Weeks;

public class WeekCalendarTests
{
    [Theory]
    [InlineData(2026, 11, 16, 2026, 11, 16)] // Monday
    [InlineData(2026, 11, 22, 2026, 11, 16)] // Sunday
    [InlineData(2026, 12, 31, 2026, 12, 28)] // Thursday, week crosses the year
    [InlineData(2027, 1, 2, 2026, 12, 28)]   // Saturday in the same week, next year
    [InlineData(2026, 3, 1, 2026, 2, 23)]    // Sunday, week crosses a month
    public void MondayOf_gives_the_Monday_of_the_ISO_week(int y, int m, int d, int ey, int em, int ed)
    {
        Assert.Equal(new DateOnly(ey, em, ed), WeekCalendar.MondayOf(new DateOnly(y, m, d)));
    }

    [Fact]
    public void SundayOf_is_the_last_day_of_the_week()
    {
        Assert.Equal(new DateOnly(2027, 1, 3), WeekCalendar.SundayOf(new DateOnly(2026, 12, 30)));
    }

    [Fact]
    public void AddWeeks_moves_whole_weeks_across_a_year_boundary()
    {
        Assert.Equal(new DateOnly(2026, 12, 28), WeekCalendar.AddWeeks(new DateOnly(2027, 1, 4), -1));
    }
}

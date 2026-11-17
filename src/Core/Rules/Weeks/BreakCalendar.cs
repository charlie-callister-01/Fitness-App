using Core.Model;

namespace Core.Rules.Weeks;

/// <summary>The one place Break arithmetic lives. A Break covers its start and end dates inclusive.</summary>
public static class BreakCalendar
{
    public static bool IsBreakDay(IEnumerable<Break> breaks, DateOnly day) =>
        breaks.Any(b => day >= b.StartDate && day <= b.EndDate);

    /// <summary>Whether any day of the week starting on <paramref name="monday"/> is a Break day.</summary>
    public static bool TouchesWeek(IEnumerable<Break> breaks, DateOnly monday) =>
        breaks.Any(b => b.StartDate <= monday.AddDays(6) && b.EndDate >= monday);
}

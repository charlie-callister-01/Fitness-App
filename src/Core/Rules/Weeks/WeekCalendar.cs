namespace Core.Rules.Weeks;

/// <summary>The one place week arithmetic lives. A week runs Monday to Sunday (ISO) and is identified by its Monday.</summary>
public static class WeekCalendar
{
    public static DateOnly MondayOf(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static DateOnly SundayOf(DateOnly date) => MondayOf(date).AddDays(6);

    public static DateOnly AddWeeks(DateOnly monday, int weeks) => monday.AddDays(7 * weeks);

    /// <summary>Whether <paramref name="date"/> falls Monday to Sunday in the week starting on <paramref name="monday"/>.</summary>
    public static bool InWeek(DateOnly monday, DateOnly date) => date >= monday && date <= monday.AddDays(6);

    /// <summary>The date of weekday <paramref name="day"/> in the week starting on <paramref name="monday"/>.</summary>
    public static DateOnly DefaultDate(DateOnly monday, DayOfWeek day) => monday.AddDays(((int)day + 6) % 7);

    /// <summary>The date of <paramref name="day"/> in the week starting on <paramref name="monday"/>, or null when daily.</summary>
    public static DateOnly? DefaultDate(DateOnly monday, Model.DefaultDay day) =>
        Model.DefaultDayExtensions.AsDayOfWeek(day) is { } dow ? DefaultDate(monday, dow) : null;

    /// <summary>The seven dates of the week starting on <paramref name="monday"/>, Monday first.</summary>
    public static IEnumerable<DateOnly> DaysOf(DateOnly monday) => Enumerable.Range(0, 7).Select(monday.AddDays);
}

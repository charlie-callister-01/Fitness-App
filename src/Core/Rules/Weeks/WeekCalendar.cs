namespace Core.Rules.Weeks;

/// <summary>The one place week arithmetic lives. A week runs Monday to Sunday (ISO) and is identified by its Monday.</summary>
public static class WeekCalendar
{
    public static DateOnly MondayOf(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static DateOnly SundayOf(DateOnly date) => MondayOf(date).AddDays(6);

    public static DateOnly AddWeeks(DateOnly monday, int weeks) => monday.AddDays(7 * weeks);
}

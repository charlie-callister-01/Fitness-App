using Core.Model;
using Core.Rules.Tracks;
using Core.Rules.Weeks;

namespace Core.Rules.PlanEditing;

/// <summary>How far along a Plan is, in words for the Plans list.</summary>
public static class PlanProgress
{
    /// <summary>
    /// The number of weeks the Plan covers: the last week of its last Phase. A Plan with no Phases falls back to the weeks from its
    /// start to its goal date (both weeks included); with neither, null.
    /// </summary>
    public static int? TotalWeeks(Plan plan)
    {
        var phases = TrackRules.Main(plan).Phases;
        if (phases.Count > 0)
        {
            return phases.Max(p => p.EndWeek);
        }

        return plan.GoalDate is { } goalDate && goalDate >= plan.StartDate
            ? (WeekCalendar.MondayOf(goalDate).DayNumber - WeekCalendar.MondayOf(plan.StartDate).DayNumber) / 7 + 1
            : null;
    }

    /// <summary>
    /// The line under the active Plan's name: "Active · week 5 of 27". Before the Plan starts it says when it starts, with no known
    /// length it drops "of m", and past the last week it says so.
    /// </summary>
    public static string ActiveLabel(Plan plan, DateOnly today)
    {
        if (WeekRules.PlanWeekNumber(plan, today) is not { } week)
        {
            return $"Active · starts {plan.StartDate.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)}";
        }

        return TotalWeeks(plan) switch
        {
            { } total when week > total => $"Active · finished after week {total}",
            { } total => $"Active · week {week} of {total}",
            _ => $"Active · week {week}",
        };
    }
}

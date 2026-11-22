using Core.Model;
using Core.Rules.Weeks;

namespace Core.Rules.Home;

public sealed record WeightPanel(WeighIn? Latest, double? WeekAverageKg, double? ChangeKg);

/// <summary>One week of Weigh-ins: its Monday, the average in kg and how many there were.</summary>
public sealed record WeighInWeek(DateOnly WeekStart, double AverageKg, int Count);

public static class WeightPanelRules
{
    public static WeightPanel Compute(IReadOnlyList<WeighIn> weighIns, DateOnly today)
    {
        var latest = LastLabelInfo(weighIns);

        var thisMonday = WeekCalendar.MondayOf(today);
        var lastMonday = WeekCalendar.AddWeeks(thisMonday, -1);

        var weekAverageKg = WeekAverageKg(weighIns, thisMonday);
        var lastWeekAverageKg = WeekAverageKg(weighIns, lastMonday);
        double? changeKg = weekAverageKg is not null && lastWeekAverageKg is not null
            ? weekAverageKg.Value - lastWeekAverageKg.Value
            : null;

        return new WeightPanel(latest, weekAverageKg, changeKg);
    }

    /// <summary>
    /// The one definition of a week's average Weigh-in (kg): the mean of the Weigh-ins from <paramref name="monday"/> to the Sunday, or null when
    /// there are none. Home, Weight and the Weekly review all use it.
    /// </summary>
    public static double? WeekAverageKg(IReadOnlyList<WeighIn> weighIns, DateOnly monday)
    {
        var inWeek = weighIns.Where(w => WeekCalendar.InWeek(monday, w.Date)).ToList();
        return inWeek.Count == 0 ? null : MeanKg(inWeek);
    }

    private static double MeanKg(IReadOnlyCollection<WeighIn> weighIns) => weighIns.Sum(w => w.WeightKg) / weighIns.Count;


    /// <summary>The Weigh-ins grouped by their Monday, newest week first, each with its average (<see cref="WeekAverageKg"/>) and count.</summary>
    public static IReadOnlyList<WeighInWeek> ByWeek(IReadOnlyList<WeighIn> weighIns) =>
        [.. weighIns
            .GroupBy(w => WeekCalendar.MondayOf(w.Date))
            .OrderByDescending(g => g.Key)
            .Select(g => new WeighInWeek(g.Key, MeanKg(g.ToList()), g.Count()))];

    public static WeighIn? LastLabelInfo(IReadOnlyList<WeighIn> weighIns)
    {
        WeighIn? latest = null;
        foreach (var weighIn in weighIns)
        {
            if (latest is null || weighIn.Date >= latest.Date)
            {
                latest = weighIn;
            }
        }

        return latest;
    }
}

using Core.Model;
using Core.Rules.Weeks;

namespace Core.Rules.Home;

public sealed record WeightPanel(WeighIn? Latest, double? WeekAverageKg, double? ChangeKg);

public static class WeightPanelRules
{
    public static WeightPanel Compute(IReadOnlyList<WeighIn> weighIns, DateOnly today)
    {
        var latest = LastLabelInfo(weighIns);

        var thisMonday = WeekCalendar.MondayOf(today);
        var lastMonday = WeekCalendar.AddWeeks(thisMonday, -1);

        double thisWeekSum = 0;
        var thisWeekCount = 0;
        double lastWeekSum = 0;
        var lastWeekCount = 0;

        foreach (var weighIn in weighIns)
        {
            if (WeekCalendar.InWeek(thisMonday, weighIn.Date))
            {
                thisWeekSum += weighIn.WeightKg;
                thisWeekCount++;
            }
            else if (WeekCalendar.InWeek(lastMonday, weighIn.Date))
            {
                lastWeekSum += weighIn.WeightKg;
                lastWeekCount++;
            }
        }

        double? weekAverageKg = thisWeekCount > 0 ? thisWeekSum / thisWeekCount : null;
        double? lastWeekAverageKg = lastWeekCount > 0 ? lastWeekSum / lastWeekCount : null;
        double? changeKg = weekAverageKg is not null && lastWeekAverageKg is not null
            ? weekAverageKg.Value - lastWeekAverageKg.Value
            : null;

        return new WeightPanel(latest, weekAverageKg, changeKg);
    }

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

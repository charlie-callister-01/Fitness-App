using Core.Model;
using Core.Rules.Home;
using Core.Rules.Units;
using Core.Rules.Weeks;

namespace Core.Rules.Insight;

/// <summary>One Weigh-in on the weight chart, in the display unit. <see cref="Rolling"/> is the mean of this and up to six earlier Weigh-ins.</summary>
public sealed record WeightPoint(DateOnly Date, double Value, double Rolling, bool IsLowest);

/// <summary>One Monday-to-Sunday week that has at least one Weigh-in.</summary>
public sealed record WeightWeekRow(DateOnly WeekStart, double Average, int Count);

/// <summary>
/// The Weight screen, prepared. All weights are in the display unit. Now and this week's average come from <see cref="WeightPanelRules"/>,
/// so Home and Weight cannot disagree.
/// </summary>
public sealed record WeightView(
    IReadOnlyList<WeightPoint> Points,
    double? Now,
    DateOnly? NowDate,
    double? ChangeSinceStart,
    double? WeekAverage,
    double? Goal,
    IReadOnlyList<WeightWeekRow> Weeks,
    string UnitLabel)
{
    public bool IsEmpty => Points.Count == 0;
}

public static class WeightViewBuilder
{
    /// <summary>The rolling average covers this many Weigh-ins.</summary>
    public const int RollingCount = 7;

    public static WeightView Build(IReadOnlyList<WeighIn> weighIns, IReadOnlyList<Badge> badges, DateOnly today, UnitConverter units)
    {
        var ordered = weighIns.OrderBy(w => w.Date).ThenBy(w => w.Id, StringComparer.Ordinal).ToList();
        var panel = WeightPanelRules.Compute(weighIns, today);

        var lowestKg = ordered.Count == 0 ? 0 : ordered.Min(w => w.WeightKg);
        var lowestIndex = ordered.FindIndex(w => w.WeightKg == lowestKg);
        var points = ordered
            .Select((w, i) => new WeightPoint(
                w.Date,
                units.DisplayWeight(w.WeightKg),
                units.DisplayWeight(ordered.Skip(Math.Max(0, i - RollingCount + 1)).Take(Math.Min(RollingCount, i + 1)).Average(x => x.WeightKg)),
                i == lowestIndex))
            .ToList();

        var weeks = WeightPanelRules.ByWeek(weighIns)
            .Select(w => new WeightWeekRow(w.WeekStart, units.DisplayWeight(w.AverageKg), w.Count))
            .ToList();

        double? change = ordered.Count >= 2 ? units.DisplayWeight(ordered[^1].WeightKg) - units.DisplayWeight(ordered[0].WeightKg) : null;
        var goalBadge = badges.FirstOrDefault(b => b.Metric.Trim().Equals("bodyweight", StringComparison.OrdinalIgnoreCase));
        // A positive target is the goal weight; a drop is the first Weigh-in minus the drop, and nothing is drawn before a first Weigh-in.
        double? goalKg = goalBadge is null ? null
            : goalBadge.Target > 0 ? goalBadge.Target
            : ordered.Count > 0 ? ordered[0].WeightKg + goalBadge.Target
            : null;

        return new WeightView(
            points,
            panel.Latest is { } latest ? units.DisplayWeight(latest.WeightKg) : null,
            panel.Latest?.Date,
            change,
            panel.WeekAverageKg is { } avg ? units.DisplayWeight(avg) : null,
            goalKg is { } g ? units.DisplayWeight(g) : null,
            weeks,
            units.WeightLabel);
    }
}

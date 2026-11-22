using System.Globalization;
using Core.Exchange;
using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Rules.Weeks;

namespace Core.Rules.Insight;

/// <summary>The chips on the Progress tab. An Exercise's group comes from its log kind and is never stored.</summary>
public enum ExerciseGroup
{
    Strength,
    Carries,
    Engine,
    Bodyweight,
    Body,
}

public static class ExerciseGroups
{
    /// <summary>The group an Exercise is charted under, or null for kinds that are never charted (Text and tick-only).</summary>
    public static ExerciseGroup? Of(LogKind kind) => kind switch
    {
        LogKind.Load => ExerciseGroup.Strength,
        LogKind.Carry => ExerciseGroup.Carries,
        LogKind.DistanceKm or LogKind.DistanceM or LogKind.Time or LogKind.Minutes => ExerciseGroup.Engine,
        LogKind.Reps or LogKind.Seconds or LogKind.BodyweightLoad => ExerciseGroup.Bodyweight,
        LogKind.BodyMeasurementKg or LogKind.BodyMeasurementCm => ExerciseGroup.Body,
        _ => null,
    };

    public static string Label(ExerciseGroup group) => group.ToString();
}

/// <summary>
/// One chart point in the owner's display unit. <see cref="BreaksBefore"/> is true when the gap since the previous point is long enough
/// that the line must not join them.
/// </summary>
public sealed record SeriesPoint(DateOnly Date, double Value, bool IsPr, string? PhaseFocus, bool BreaksBefore);

/// <summary>A prepared chart: the points, the best one, and the headline. Built from history, never stored.</summary>
public sealed record ExerciseSeries(
    string ExerciseId,
    string Name,
    ExerciseGroup Group,
    string UnitLabel,
    IReadOnlyList<SeriesPoint> Points,
    SeriesPoint Best,
    double Change,
    int? FirstPlanWeek,
    string BestLabel);

/// <summary>The progress series of one Exercise: the existing progress-point and PR rules, with Phase, display unit and the best-point label.</summary>
public static class ProgressSeries
{
    /// <summary>Days without a point after which the line breaks rather than joins.</summary>
    public const int GapDays = 14;

    /// <summary>
    /// The series, or null when the Exercise is never charted or has fewer than two visible points. PR flags and Phases come from the whole
    /// history; <paramref name="range"/> (null means all time) only chooses which points are shown, and Best, change and the label describe those.
    /// </summary>
    public static ExerciseSeries? Build(Exercise exercise, Plan? plan, IEnumerable<Workout> history, UnitConverter units, DateRange? range = null)
    {
        if (ExerciseGroups.Of(exercise.LogKind) is not { } group) return null;

        var workouts = history.ToList();
        var raw = ExercisePerformance.ProgressPoints(exercise, workouts);
        var visible = range is null ? raw : [.. raw.Where(p => range.Contains(p.Date))];
        if (visible.Count < 2) return null;

        var prDates = ExercisePerformance.Prs(exercise, workouts).Select(p => p.Date).ToHashSet();
        var points = visible
            .Select((p, i) => new SeriesPoint(
                p.Date,
                Display(exercise.LogKind, p, units),
                prDates.Contains(p.Date),
                PhaseFocusOf(plan, p.Date),
                i > 0 && p.Date.DayNumber - visible[i - 1].Date.DayNumber > GapDays))
            .ToList();

        var lowerIsBetter = LogKindRules.For(exercise.LogKind).LowerIsBetter;
        var best = points.Aggregate((a, b) => (lowerIsBetter ? b.Value < a.Value : b.Value > a.Value) ? b : a);
        var change = best.Value - points[0].Value;
        var firstWeek = plan is null ? null : WeekRules.PlanWeekNumber(plan, points[0].Date);
        var unit = Unit(exercise.LogKind, visible[0].Set, units);
        var label = Label(best.Value, change, firstWeek, DecimalPlaces(exercise.LogKind, visible[0].Set));

        return new ExerciseSeries(exercise.Id, exercise.Name, group, unit, points, best, change, firstWeek, label);
    }

    private static double Display(LogKind kind, ProgressPoint point, UnitConverter units) => kind switch
    {
        LogKind.Load or LogKind.BodyMeasurementKg => units.DisplayWeight(point.Value, rounded: false),
        LogKind.BodyweightLoad or LogKind.Carry when point.Set.WeightKg is > 0 => units.DisplayWeight(point.Value, rounded: false),
        LogKind.DistanceKm => units.DisplayDistance(point.Value, rounded: false),
        LogKind.Minutes => point.Value / 60,
        _ => point.Value,
    };

    private static string Unit(LogKind kind, WorkoutSet first, UnitConverter units) => kind switch
    {
        LogKind.Load or LogKind.BodyMeasurementKg => units.WeightLabel,
        LogKind.BodyweightLoad => first.WeightKg is > 0 ? units.WeightLabel : "reps",
        LogKind.Carry => first.WeightKg is > 0 ? units.WeightLabel : "m",
        LogKind.DistanceKm => units.DistanceLabel,
        LogKind.DistanceM => "m",
        LogKind.Reps => "reps",
        LogKind.Seconds or LogKind.Time => "s",
        LogKind.Minutes => "min",
        LogKind.BodyMeasurementCm => "cm",
        _ => "",
    };

    private static int DecimalPlaces(LogKind kind, WorkoutSet first) => kind switch
    {
        LogKind.Reps or LogKind.Seconds or LogKind.Time or LogKind.DistanceM => 0,
        LogKind.BodyweightLoad => first.WeightKg is > 0 ? 1 : 0,
        LogKind.Carry => first.WeightKg is > 0 ? 1 : 0,
        _ => 1,
    };

    private static string? PhaseFocusOf(Plan? plan, DateOnly date)
    {
        if (plan is null || WeekRules.PlanWeekNumber(plan, date) is not { } week) return null;
        return plan.Phases.FirstOrDefault(p => week >= p.StartWeek && week <= p.EndWeek)?.Focus;
    }

    private static string Label(double best, double change, int? firstWeek, int decimals)
    {
        var format = decimals == 0 ? "0" : "0.0";
        var sign = Math.Round(change, decimals) < 0 ? "-" : "+";
        var since = firstWeek is { } w ? $"W{w}" : "first entry";
        return
            $"Best {best.ToString(format, CultureInfo.InvariantCulture)} · {sign}{Math.Abs(change).ToString(format, CultureInfo.InvariantCulture)} since {since}";
    }
}

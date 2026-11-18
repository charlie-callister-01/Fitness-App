using System.Globalization;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.Performance;
using Core.Rules.Units;

namespace Ui.Components;

/// <summary>Plain-text results for the Exercise header ("22 kg × 8"), in the owner's display units.</summary>
public static class SetText
{
    private const double SecondsPerMinute = 60;

    /// <summary>A Set as text for its log kind. Empty when the Set lacks what the kind needs (a text Exercise has no Set to show).</summary>
    public static string Result(
        LogKind kind, WorkoutSet set, UnitConverter units, WeightUnit unit, DistanceUnit distanceUnit = DistanceUnit.Km) => kind switch
    {
        LogKind.Load => Weighted(set.WeightKg, set.Reps, units, unit),
        LogKind.BodyweightLoad when set.WeightKg is > 0 => $"bw + {Weighted(set.WeightKg, set.Reps, units, unit)}",
        LogKind.BodyweightLoad => $"bw × {set.Reps}",
        LogKind.Carry when set.WeightKg is { } kg && set.DistanceM is { } m => $"{WeightText(kg, units, unit)} × {Number(m)} m",
        LogKind.Reps when set.Reps is { } reps => $"{reps} reps",
        LogKind.Seconds when set.DurationSeconds is { } s => $"{s} s",
        LogKind.Minutes when set.DurationSeconds is { } s => $"{Number(s / SecondsPerMinute)} min",
        LogKind.Time when set.DurationSeconds is { } s => RowPrefill.Clock(s),
        LogKind.DistanceKm when set.DistanceKm is { } km =>
            $"{Number(units.DisplayDistance(km))} {(distanceUnit == DistanceUnit.Km ? "km" : "mi")}",
        LogKind.DistanceM when set.DistanceM is { } m => $"{Number(m)} m",
        LogKind.BodyMeasurementKg when set.WeightKg is { } kg => WeightText(kg, units, unit),
        LogKind.BodyMeasurementCm when set.MeasurementCm is { } cm => $"{Number(cm)} cm",
        LogKind.TickOnly => "Done",
        _ => "",
    };

    /// <summary>A result without the weight unit, for the "beats 22 × 8" half of the PR toast; every other kind reads as <see cref="Result"/>.</summary>
    public static string Short(
        LogKind kind, WorkoutSet set, UnitConverter units, WeightUnit unit, DistanceUnit distanceUnit = DistanceUnit.Km) => kind switch
    {
        LogKind.Load => $"{Number(units.DisplayWeight(set.WeightKg ?? 0))} × {set.Reps}",
        LogKind.BodyweightLoad when set.WeightKg is > 0 => $"bw + {Number(units.DisplayWeight(set.WeightKg.Value))} × {set.Reps}",
        _ => Result(kind, set, units, unit, distanceUnit),
    };

    public static string Target(
        LogKind kind, SuggestedTarget target, UnitConverter units, WeightUnit unit, DistanceUnit distanceUnit = DistanceUnit.Km) =>
        Result(
            kind,
            new WorkoutSet("", target.WeightKg, target.Reps, target.DistanceKm, target.DurationSeconds, target.DistanceM, false),
            units, unit, distanceUnit);

    private static string Weighted(double? kg, int? reps, UnitConverter units, WeightUnit unit) =>
        $"{WeightText(kg ?? 0, units, unit)} × {reps}";

    private static string WeightText(double kg, UnitConverter units, WeightUnit unit) =>
        $"{Number(units.DisplayWeight(kg))} {(unit == WeightUnit.Kg ? "kg" : "lb")}";

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

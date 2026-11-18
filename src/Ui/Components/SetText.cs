using System.Globalization;
using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Units;

namespace Ui.Components;

/// <summary>Plain-text results for the Exercise header ("22 kg × 8"), in the owner's display units.</summary>
public static class SetText
{
    public static string Result(LogKind kind, WorkoutSet set, UnitConverter units, WeightUnit unit) => kind switch
    {
        LogKind.Load => Weighted(set.WeightKg, set.Reps, units, unit),
        LogKind.BodyweightLoad when set.WeightKg is > 0 => $"bw + {Weighted(set.WeightKg, set.Reps, units, unit)}",
        LogKind.BodyweightLoad => $"bw × {set.Reps}",
        _ => set.Reps is { } reps ? $"{reps} reps" : "",
    };

    /// <summary>A result without the unit, for the "beats 22 × 8" half of the PR toast.</summary>
    public static string Short(LogKind kind, WorkoutSet set, UnitConverter units) => kind switch
    {
        LogKind.Load => $"{units.DisplayWeight(set.WeightKg ?? 0).ToString("0.##", CultureInfo.InvariantCulture)} × {set.Reps}",
        LogKind.BodyweightLoad when set.WeightKg is > 0 => $"bw + {units.DisplayWeight(set.WeightKg.Value).ToString("0.##", CultureInfo.InvariantCulture)} × {set.Reps}",
        LogKind.BodyweightLoad => $"bw × {set.Reps}",
        _ => set.Reps is { } reps ? $"{reps} reps" : "",
    };

    public static string Target(LogKind kind, SuggestedTarget target, UnitConverter units, WeightUnit unit) =>
        Result(kind, new WorkoutSet("", target.WeightKg, target.Reps, null, null, null, false), units, unit);

    private static string Weighted(double? kg, int? reps, UnitConverter units, WeightUnit unit)
    {
        var weight = units.DisplayWeight(kg ?? 0).ToString("0.##", CultureInfo.InvariantCulture);
        return $"{weight} {(unit == WeightUnit.Kg ? "kg" : "lb")} × {reps}";
    }
}

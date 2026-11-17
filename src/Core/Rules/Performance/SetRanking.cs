using Core.Model;

namespace Core.Rules.Performance;

/// <summary>
/// Turns a Set into a sortable key so that "better" always means a larger key, for every log kind.
/// A null key means the Set cannot be ranked for that kind (missing fields, or a kind with no best).
/// </summary>
internal static class SetRanking
{
    public static double[]? KeyFor(LogKind kind, WorkoutSet set) => kind switch
    {
        LogKind.Load => set is { WeightKg: { } w, Reps: { } r } ? [EstimatedOneRepMax(w, r)] : null,
        LogKind.BodyweightLoad => BodyweightKey(set),
        LogKind.Carry => set is { DistanceM: { } d } ? [set.WeightKg ?? 0, d] : null,
        LogKind.Reps => set.Reps is { } r ? [r] : null,
        LogKind.Seconds or LogKind.Minutes => set.DurationSeconds is { } s ? [s] : null,
        LogKind.Time => set.DurationSeconds is { } s ? [-s] : null,
        LogKind.DistanceKm => set.DistanceKm is { } km ? [km] : null,
        LogKind.DistanceM => set.DistanceM is { } m ? [m] : null,
        _ => null,
    };

    /// <summary>Epley: kg x (1 + reps / 30).</summary>
    public static double EstimatedOneRepMax(double weightKg, int reps) => weightKg * (1 + reps / 30.0);

    /// <summary>
    /// The y value drawn on the Exercise's progress chart for a Set, in the Exercise's natural measure.
    /// A body measurement in kg is read from <see cref="WorkoutSet.WeightKg"/>, one in cm from <see cref="WorkoutSet.MeasurementCm"/>.
    /// </summary>
    public static double? ChartValue(LogKind kind, WorkoutSet set) => kind switch
    {
        LogKind.Load => set is { WeightKg: { } w, Reps: { } r } ? EstimatedOneRepMax(w, r) : null,
        LogKind.BodyweightLoad => set.Reps is not { } r ? null : set.WeightKg is > 0 and var w ? EstimatedOneRepMax(w, r) : r,
        LogKind.Carry => set.DistanceM is null ? null : set.WeightKg is > 0 ? set.WeightKg : set.DistanceM,
        LogKind.Reps => set.Reps,
        LogKind.Seconds or LogKind.Minutes or LogKind.Time => set.DurationSeconds,
        LogKind.DistanceKm => set.DistanceKm,
        LogKind.DistanceM => set.DistanceM,
        LogKind.BodyMeasurementKg => set.WeightKg,
        LogKind.BodyMeasurementCm => set.MeasurementCm,
        _ => null,
    };

    public static int Compare(double[] a, double[] b)
    {
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var c = a[i].CompareTo(b[i]);
            if (c != 0) return c;
        }
        return 0;
    }

    private static double[]? BodyweightKey(WorkoutSet set)
    {
        if (set.Reps is not { } reps) return null;
        return set.WeightKg is > 0 and var w
            ? [1, EstimatedOneRepMax(w, reps)]
            : [0, reps];
    }
}

using Core.Model;
using Core.Rules.Performance;

namespace Core.Rules.LogKinds;

/// <summary>
/// What is genuinely per <see cref="LogKind"/>, in one place: how a Set ranks and charts, whether the kind is a body
/// measurement or lower-is-better, the plain-language "expected input" message, and the one-step progression of a
/// single-number kind. Parsing shapes and the load, carry and time progression rules differ per kind in structure,
/// so they stay as switches in their own files and read only the facts here. An undefined kind gets <see cref="Unsupported"/>.
/// </summary>
internal sealed record LogKindRules
{
    /// <summary>Seconds a time (or a seconds Exercise) improves by in one progression step.</summary>
    public const int SecondStep = 5;

    private const double SecondsPerMinute = 60;

    private static readonly Dictionary<LogKind, LogKindRules> Table = new()
    {
        [LogKind.Load] = new()
        {
            RawKey = s => s is { WeightKg: { } w, Reps: { } r } ? [SetRanking.EstimatedOneRepMax(w, r)] : null,
            ChartValue = s => s is { WeightKg: { } w, Reps: { } r } ? SetRanking.EstimatedOneRepMax(w, r) : null,
            Expected = "Expected weight x reps, like 20x10",
        },
        [LogKind.BodyweightLoad] = new()
        {
            RawKey = s => s.Reps is not { } reps
                ? null
                : s.WeightKg is > 0 and var w ? [1, SetRanking.EstimatedOneRepMax(w, reps)] : [0, reps],
            ChartValue = s => s.Reps is not { } r ? null : s.WeightKg is > 0 and var w ? SetRanking.EstimatedOneRepMax(w, r) : r,
            Expected = "Expected bwxreps or added weight x reps, like bwx8 or 10x8",
        },
        [LogKind.Carry] = new()
        {
            RawKey = s => s is { DistanceM: { } d } ? [s.WeightKg ?? 0, d] : null,
            ChartValue = s => s.DistanceM is null ? null : s.WeightKg is > 0 ? s.WeightKg : s.DistanceM,
            Expected = "Expected weight x metres, like 32x40",
        },
        [LogKind.Reps] = Single(
            s => s.Reps,
            "Expected a whole number of reps, like 12",
            new ScalarProgression(1, 1, "rep", Pluralise: true, s => s.Reps, (v, reason) => new(null, (int)v, null, null, null, reason))),
        [LogKind.Seconds] = Single(
            s => s.DurationSeconds,
            "Expected seconds, like 45 or 45s",
            new ScalarProgression(SecondStep, 1, "s", Pluralise: false, s => s.DurationSeconds, ForDuration)),
        [LogKind.Minutes] = Single(
            s => s.DurationSeconds,
            "Expected minutes, like 3 or 3min",
            new ScalarProgression(SecondsPerMinute, SecondsPerMinute, "min", Pluralise: false, s => s.DurationSeconds, ForDuration)),
        [LogKind.DistanceKm] = Single(
            s => s.DistanceKm,
            "Expected a distance in km, like 5 or 5km",
            new ScalarProgression(0.5, 1, "km", Pluralise: false, s => s.DistanceKm, (v, reason) => new(null, null, null, null, v, reason))),
        [LogKind.DistanceM] = Single(
            s => s.DistanceM,
            "Expected a distance in metres, like 400 or 400m",
            new ScalarProgression(10, 1, "m", Pluralise: false, s => s.DistanceM, (v, reason) => new(null, null, v, null, null, reason))),
        [LogKind.Time] = new()
        {
            RawKey = s => s.DurationSeconds is { } secs ? [secs] : null,
            ChartValue = s => s.DurationSeconds,
            Expected = "Expected a time like 4:32, 1:04:10 or seconds",
            LowerIsBetter = true,
        },
        [LogKind.BodyMeasurementKg] = new()
        {
            RawKey = _ => null,
            ChartValue = s => s.WeightKg,
            Expected = "Expected a weight, like 82.3",
            IsMeasurement = true,
        },
        [LogKind.BodyMeasurementCm] = new()
        {
            RawKey = _ => null,
            ChartValue = s => s.MeasurementCm,
            Expected = "Expected a length in cm, like 92.5",
            IsMeasurement = true,
        },
        [LogKind.Text] = new()
        {
            RawKey = _ => null,
            ChartValue = _ => null,
            Expected = "Expected some text",
        },
        [LogKind.TickOnly] = new()
        {
            RawKey = _ => null,
            ChartValue = _ => null,
            Expected = "Nothing to type: tick it done",
        },
    };

    /// <summary>The kind's ranking key before direction is applied, larger being better; null when the Set cannot be ranked.</summary>
    public required Func<WorkoutSet, double[]?> RawKey { get; init; }

    /// <summary>The y value drawn on the progress chart, in the Exercise's natural measure; null when the Set has none.</summary>
    public required Func<WorkoutSet, double?> ChartValue { get; init; }

    /// <summary>Plain-language reason shown when typed input does not fit the kind.</summary>
    public required string Expected { get; init; }

    /// <summary>A body measurement (kg or cm): charted as its readings, not as a best.</summary>
    public bool IsMeasurement { get; init; }

    /// <summary>A smaller value beats a larger one (a time). Ranking negates the raw key for these kinds.</summary>
    public bool LowerIsBetter { get; init; }

    /// <summary>The progression of a single-number kind (reps, seconds, minutes, km, metres); null for any other kind.</summary>
    public ScalarProgression? Scalar { get; init; }

    /// <summary>The entry for a kind that is undefined or not handled: it cannot be ranked, charted or typed.</summary>
    public static LogKindRules Unsupported { get; } = new()
    {
        RawKey = _ => null,
        ChartValue = _ => null,
        Expected = "This kind of Exercise can't be logged from typed input",
    };

    public static LogKindRules For(LogKind kind) => Table.GetValueOrDefault(kind, Unsupported);

    /// <summary>A key where "better" always means larger, for every kind; null when the Set cannot be ranked.</summary>
    public double[]? Key(WorkoutSet set) =>
        RawKey(set) is { } key ? (LowerIsBetter ? [.. key.Select(k => -k)] : key) : null;

    /// <summary>A kind whose Set is one number that ranks and charts as itself (reps, seconds, minutes, km, metres).</summary>
    private static LogKindRules Single(Func<WorkoutSet, double?> measure, string expected, ScalarProgression progression) => new()
    {
        RawKey = s => measure(s) is { } v ? [v] : null,
        ChartValue = measure,
        Expected = expected,
        Scalar = progression,
    };

    private static SuggestedTarget ForDuration(double seconds, string reason) => new(null, null, null, (int)seconds, null, reason);
}

/// <summary>
/// One progression step for a single-number kind. <see cref="Amount"/> is in the stored unit and is shown as
/// <see cref="Amount"/> / <see cref="PerDisplayUnit"/> of <see cref="Unit"/>. <see cref="Measure"/> reads the stored number
/// from a Set and <see cref="Target"/> builds the Suggested target holding a number.
/// </summary>
internal sealed record ScalarProgression(
    double Amount,
    double PerDisplayUnit,
    string Unit,
    bool Pluralise,
    Func<WorkoutSet, double?> Measure,
    Func<double, string, SuggestedTarget> Target);

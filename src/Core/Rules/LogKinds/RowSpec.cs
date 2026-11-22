using System.Globalization;
using Core.Rules.Formatting;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.Units;

namespace Core.Rules.LogKinds;

/// <summary>How many inputs a Set row has: two (first × second), one wide input, or a single tick button.</summary>
public enum RowShape
{
    Pair,
    Wide,
    Tick,
}

/// <summary>Which of a row's two fields.</summary>
public enum RowField
{
    First,
    Second,
}

/// <summary>The starting text of a row's fields, in display units, in a form the input parser accepts back unchanged.</summary>
public readonly record struct RowFields(string First, string Second = "");

/// <summary>
/// Everything that is per log kind about a row of the log, in one table (design 8.1): its shape, labels and keypad, the text
/// a Set shows as and fills the fields with, the hint in an empty field, and what its steppers do. Adding a loggable kind
/// means adding one entry here. A kind with no entry (free text: a Set cannot hold text) has no row.
/// </summary>
public sealed record RowSpec
{
    private const double SecondsPerMinute = 60;
    private const int RepStep = 1;

    private static readonly Dictionary<LogKind, RowSpec> Table = new()
    {
        [LogKind.Load] = new()
        {
            Shape = RowShape.Pair, FirstLabel = "Weight", SecondLabel = "Reps", FirstMode = "decimal",
            HintsPlannedReps = true,
            Fill = (s, u) => s is { WeightKg: > 0 and var w, Reps: { } r } ? new(Weight(u, w), Whole(r)) : null,
            Result = (s, u) => $"{WeightText(s.WeightKg ?? 0, u)} × {s.Reps}",
            Short = (s, u) => $"{Number(u.DisplayWeight(s.WeightKg ?? 0))} × {s.Reps}",
            Step = (u, increment) => new(StepField.First, u.DisplayWeight(increment), u.WeightLabel),
        },
        [LogKind.BodyweightLoad] = new()
        {
            Shape = RowShape.Pair, FirstLabel = "Added weight", SecondLabel = "Reps", FirstMode = "decimal",
            HintsPlannedReps = true,
            FirstHint = u => $"{RowInput.Bodyweight} or {u.WeightLabel}",
            Fill = (s, u) => s.Reps is { } r
                ? new(s.WeightKg is > 0 and var w ? Weight(u, w) : RowInput.Bodyweight, Whole(r))
                : null,
            Result = (s, u) => s.WeightKg is > 0
                ? $"{RowInput.Bodyweight} + {WeightText(s.WeightKg.Value, u)} × {s.Reps}"
                : $"{RowInput.Bodyweight} × {s.Reps}",
            Short = (s, u) => s.WeightKg is > 0 and var w
                ? $"{RowInput.Bodyweight} + {Number(u.DisplayWeight(w))} × {s.Reps}"
                : $"{RowInput.Bodyweight} × {s.Reps}",
            Step = (_, _) => new(StepField.Second, RepStep, "rep"),
        },
        [LogKind.Carry] = new()
        {
            Shape = RowShape.Pair, FirstLabel = "Weight per hand", SecondLabel = "Metres", FirstMode = "decimal",
            Fill = (s, u) => s is { WeightKg: > 0 and var w, DistanceM: { } m } ? new(Weight(u, w), Number(m)) : null,
            Result = (s, u) => s is { WeightKg: { } kg, DistanceM: { } m } ? $"{WeightText(kg, u)} × {Number(m)} m" : null,
            Step = (u, increment) => new(StepField.First, u.DisplayWeight(increment), u.WeightLabel),
        },
        [LogKind.Reps] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Reps", FirstMode = "numeric", HintsPlannedReps = true,
            Fill = (s, _) => s.Reps is { } r ? new(Whole(r)) : null,
            Result = (s, _) => s.Reps is { } r ? $"{r} reps" : null,
        },
        [LogKind.Seconds] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Seconds", FirstMode = "numeric",
            Fill = (s, _) => s.DurationSeconds is { } v ? new(Whole(v)) : null,
            Result = (s, _) => s.DurationSeconds is { } v ? $"{v} s" : null,
            Step = (_, _) => new(StepField.First, LogKindRules.SecondStep, "s"),
        },
        [LogKind.Minutes] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Minutes", FirstMode = "decimal",
            Fill = (s, _) => s.DurationSeconds is { } v ? new(Number(v / SecondsPerMinute)) : null,
            Result = (s, _) => s.DurationSeconds is { } v ? $"{Number(v / SecondsPerMinute)} min" : null,
        },
        [LogKind.Time] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Time", FirstMode = "text",
            FirstHint = _ => "m:ss",
            Fill = (s, _) => s.DurationSeconds is { } v ? new(Clock(v)) : null,
            Result = (s, _) => s.DurationSeconds is { } v ? Clock(v) : null,
        },
        [LogKind.DistanceKm] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Distance", FirstMode = "decimal",
            FirstHint = u => u.DistanceLabel,
            Fill = (s, u) => s.DistanceKm is { } km ? new(Number(u.DisplayDistance(km))) : null,
            Result = (s, u) => s.DistanceKm is { } km ? $"{Number(u.DisplayDistance(km))} {u.DistanceLabel}" : null,
        },
        [LogKind.DistanceM] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Distance", FirstMode = "decimal",
            FirstHint = _ => "m",
            Fill = (s, _) => s.DistanceM is { } m ? new(Number(m)) : null,
            Result = (s, _) => s.DistanceM is { } m ? $"{Number(m)} m" : null,
        },
        [LogKind.BodyMeasurementKg] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Body weight", FirstMode = "decimal",
            FirstHint = u => u.WeightLabel,
            Fill = (s, u) => s.WeightKg is { } kg ? new(Weight(u, kg)) : null,
            Result = (s, u) => s.WeightKg is { } kg ? WeightText(kg, u) : null,
        },
        [LogKind.BodyMeasurementCm] = new()
        {
            Shape = RowShape.Wide, FirstLabel = "Measurement", FirstMode = "decimal",
            FirstHint = _ => "cm",
            Fill = (s, _) => s.MeasurementCm is { } cm ? new(Number(cm)) : null,
            Result = (s, _) => s.MeasurementCm is { } cm ? $"{Number(cm)} cm" : null,
        },
        [LogKind.TickOnly] = new()
        {
            Shape = RowShape.Tick, FirstLabel = "Done", FirstMode = "none",
            Fill = (_, _) => null,
            Result = (_, _) => "Done",
        },
    };

    public required RowShape Shape { get; init; }

    /// <summary>The first field's accessible name, without the "Set N" part.</summary>
    public required string FirstLabel { get; init; }

    /// <summary>The second field's accessible name; null for a wide or tick row.</summary>
    public string? SecondLabel { get; init; }

    /// <summary>The HTML inputmode of the first field.</summary>
    public required string FirstMode { get; init; }

    /// <summary>The HTML inputmode of the second field.</summary>
    public string SecondMode { get; init; } = "numeric";

    /// <summary>True when the planned reps (for example "8" or "8-12") are the hint for an empty row with no history.</summary>
    public bool HintsPlannedReps { get; init; }

    /// <summary>What an empty first field hints at (its unit or format); the default is no hint.</summary>
    public Func<UnitConverter, string?> FirstHint { get; init; } = _ => null;

    /// <summary>The row's fields for a Set (or a Suggested target as a Set), or null when it lacks what the kind needs.</summary>
    public required Func<WorkoutSet, UnitConverter, RowFields?> Fill { get; init; }

    /// <summary>A Set as text, such as "22 kg × 8"; null when the Set lacks what the kind needs.</summary>
    public required Func<WorkoutSet, UnitConverter, string?> Result { get; init; }

    /// <summary>A Set as text without the weight unit, for the "beats 22 × 8" half of the PR toast; null reads as <see cref="Result"/>.</summary>
    public Func<WorkoutSet, UnitConverter, string>? Short { get; init; }

    /// <summary>The stepper's field, amount and unit word given the Plan's weight increment (kg); null for no steppers.</summary>
    public Func<UnitConverter, double, StepSize>? Step { get; init; }

    /// <summary>The spec for a kind, or null when the kind cannot be logged in a row.</summary>
    public static RowSpec? For(LogKind kind) => Table.GetValueOrDefault(kind);

    /// <summary>Whether the kind has a row at all.</summary>
    public static bool IsLoggable(LogKind kind) => Table.ContainsKey(kind);

    /// <summary>The step for the kind's steppers, or null when it has none (time, distance and the rest).</summary>
    public StepSize? StepFor(UnitConverter units, double planIncrementKg) => Step?.Invoke(units, planIncrementKg);

    /// <summary>"m:ss", or "h:mm:ss" from an hour up: the forms the parser accepts for a time.</summary>
    public static string Clock(int totalSeconds)
    {
        const int secondsPerHour = 3600;
        var hours = totalSeconds / secondsPerHour;
        var minutes = totalSeconds % secondsPerHour / 60;
        var seconds = totalSeconds % 60;
        return hours > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:00}:{seconds:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{minutes}:{seconds:00}");
    }

    private static string WeightText(double kg, UnitConverter units) => $"{Number(units.DisplayWeight(kg))} {units.WeightLabel}";

    private static string Weight(UnitConverter units, double kg) => Number(units.DisplayWeight(kg));

    private static string Whole(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(double value) => DisplayFormat.Trimmed(value);
}

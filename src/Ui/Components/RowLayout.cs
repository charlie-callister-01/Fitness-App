using Core.Model;

namespace Ui.Components;

/// <summary>How many inputs a Set row has: two (first × second), one wide input, or a single tick button.</summary>
public enum RowShape
{
    Pair,
    Wide,
    Tick,
}

/// <summary>What a Set row looks like for a log kind (design 8.1): its shape, field labels and phone keypad modes.</summary>
/// <param name="FirstLabel">The first field's accessible name, without the "set N" part.</param>
/// <param name="SecondLabel">The second field's accessible name; null for a wide or tick row.</param>
/// <param name="FirstMode">The HTML inputmode of the first field.</param>
/// <param name="FirstUnit">The kind of unit the first field's placeholder names, if any.</param>
public sealed record RowLayout(
    RowShape Shape, string FirstLabel, string? SecondLabel, string FirstMode, string SecondMode = "numeric", RowUnit FirstUnit = RowUnit.None)
{
    /// <summary>The layout for a kind, or null when the kind cannot be logged in a row (free text: a Set cannot hold text).</summary>
    public static RowLayout? For(LogKind kind) => kind switch
    {
        LogKind.Load => new(RowShape.Pair, "Weight", "Reps", "decimal"),
        LogKind.BodyweightLoad => new(RowShape.Pair, "Added weight", "Reps", "decimal"),
        LogKind.Carry => new(RowShape.Pair, "Weight per hand", "Metres", "decimal"),
        LogKind.Reps => new(RowShape.Wide, "Reps", null, "numeric"),
        LogKind.Seconds => new(RowShape.Wide, "Seconds", null, "numeric"),
        LogKind.Minutes => new(RowShape.Wide, "Minutes", null, "decimal"),
        LogKind.Time => new(RowShape.Wide, "Time", null, "text", FirstUnit: RowUnit.Clock),
        LogKind.DistanceKm => new(RowShape.Wide, "Distance", null, "decimal", FirstUnit: RowUnit.Distance),
        LogKind.DistanceM => new(RowShape.Wide, "Distance", null, "decimal", FirstUnit: RowUnit.Metres),
        LogKind.BodyMeasurementKg => new(RowShape.Wide, "Body weight", null, "decimal", FirstUnit: RowUnit.Weight),
        LogKind.BodyMeasurementCm => new(RowShape.Wide, "Measurement", null, "decimal", FirstUnit: RowUnit.Centimetres),
        LogKind.TickOnly => new(RowShape.Tick, "Done", null, "none"),
        _ => null,
    };
}

/// <summary>The unit a wide row's empty field hints at.</summary>
public enum RowUnit
{
    None,
    Clock,
    Distance,
    Metres,
    Weight,
    Centimetres,
}

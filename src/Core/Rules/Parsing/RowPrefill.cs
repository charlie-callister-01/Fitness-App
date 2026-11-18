using System.Globalization;
using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Units;

namespace Core.Rules.Parsing;

/// <summary>Where a row's starting values came from.</summary>
public enum PrefillSource
{
    /// <summary>Nothing to start from: the fields are empty.</summary>
    None,

    /// <summary>The same-numbered Set of the Exercise's Last Workout.</summary>
    LastWorkout,

    /// <summary>The Suggested target.</summary>
    SuggestedTarget,
}

/// <summary>
/// The text to show in a row's one or two fields, in display units, in a form <see cref="RowInput.Join"/> and
/// <see cref="InputParser"/> accept back unchanged. <see cref="RepsHint"/> is the planned reps (for example "8" or "8-12"),
/// set only when there is nothing to start from. Never stored.
/// </summary>
public sealed record RowPrefillValues(string First, string Second, string? RepsHint, PrefillSource Source)
{
    public static RowPrefillValues Empty(string? hint = null) => new("", "", hint, PrefillSource.None);
}

/// <summary>
/// The starting values of a Set row: the same-numbered Set of the Exercise's Last Workout, else the Suggested target,
/// else empty with the planned reps as a hint. Pure; the result is never stored, and weights and distances are
/// converted to the display units by <see cref="UnitConverter"/>.
/// </summary>
public static class RowPrefill
{
    private const double SecondsPerMinute = 60;
    private const int SecondsPerHour = 3600;

    /// <param name="kind">The Exercise's log kind.</param>
    /// <param name="setNumber">The row's Set number, counting from 1.</param>
    /// <param name="snapshot">Last and Suggested target for the Exercise; null when it has no history.</param>
    /// <param name="planned">The Exercise's planned row, or null when there is no Plan.</param>
    public static RowPrefillValues For(
        LogKind kind,
        int setNumber,
        ExerciseSnapshot? snapshot,
        SessionExercise? planned,
        UnitConverter units)
    {
        if (snapshot?.Last is { } last
            && setNumber >= 1 && setNumber <= last.Sets.Count
            && Fields(kind, FromSet(last.Sets[setNumber - 1]), units) is { } fromLast)
        {
            return fromLast with { Source = PrefillSource.LastWorkout };
        }

        if (snapshot?.Target is { } target && Fields(kind, FromTarget(target), units) is { } fromTarget)
        {
            return fromTarget with { Source = PrefillSource.SuggestedTarget };
        }

        return RowPrefillValues.Empty(Hint(kind, planned));
    }

    /// <summary>
    /// The fields of a stored Set in display units, in the same form as a prefill, so a confirmed row shows (and a reopened row
    /// starts from) what was saved. Null when the Set lacks what the kind needs.
    /// </summary>
    public static RowPrefillValues? FromStored(LogKind kind, WorkoutSet set, UnitConverter units) =>
        Fields(kind, FromSet(set), units);

    private static ParsedSet FromSet(WorkoutSet s) =>
        new(s.WeightKg, s.Reps, s.DistanceKm, s.DurationSeconds, s.DistanceM, s.MeasurementCm, null);

    private static ParsedSet FromTarget(SuggestedTarget t) =>
        new(t.WeightKg, t.Reps, t.DistanceKm, t.DurationSeconds, t.DistanceM, null, null);

    /// <summary>The fields for a value, or null when the value lacks what the kind needs (so the next source is tried).</summary>
    private static RowPrefillValues? Fields(LogKind kind, ParsedSet v, UnitConverter units) => kind switch
    {
        LogKind.Load when v is { WeightKg: > 0 and var w, Reps: { } r } => Two(Weight(units, w), Whole(r)),
        LogKind.BodyweightLoad when v.Reps is { } r =>
            Two(v.WeightKg is > 0 and var w ? Weight(units, w) : RowInput.Bodyweight, Whole(r)),
        LogKind.Carry when v is { WeightKg: > 0 and var w, DistanceM: { } m } => Two(Weight(units, w), Number(m)),
        LogKind.Reps when v.Reps is { } r => One(Whole(r)),
        LogKind.Seconds when v.DurationSeconds is { } s => One(Whole(s)),
        LogKind.Minutes when v.DurationSeconds is { } s => One(Number(s / SecondsPerMinute)),
        LogKind.Time when v.DurationSeconds is { } s => One(Clock(s)),
        LogKind.DistanceKm when v.DistanceKm is { } km => One(Number(units.DisplayDistance(km))),
        LogKind.DistanceM when v.DistanceM is { } m => One(Number(m)),
        LogKind.BodyMeasurementKg when v.WeightKg is { } kg => One(Weight(units, kg)),
        LogKind.BodyMeasurementCm when v.MeasurementCm is { } cm => One(Number(cm)),
        _ => null,
    };

    private static string? Hint(LogKind kind, SessionExercise? planned) => kind switch
    {
        LogKind.Load or LogKind.BodyweightLoad or LogKind.Reps => planned switch
        {
            { Reps: { } low, RepsMax: { } high } when high > low => $"{low}-{high}",
            { Reps: { } reps } => Whole(reps),
            _ => null,
        },
        _ => null,
    };

    private static RowPrefillValues One(string first) => new(first, "", null, PrefillSource.None);

    private static RowPrefillValues Two(string first, string second) => new(first, second, null, PrefillSource.None);

    private static string Weight(UnitConverter units, double kg) => Number(units.DisplayWeight(kg));

    private static string Whole(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>"m:ss", or "h:mm:ss" from an hour up: the forms the parser accepts for a time.</summary>
    private static string Clock(int totalSeconds)
    {
        var hours = totalSeconds / SecondsPerHour;
        var minutes = totalSeconds % SecondsPerHour / 60;
        var seconds = totalSeconds % 60;
        return hours > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{hours}:{minutes:00}:{seconds:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{minutes}:{seconds:00}");
    }
}

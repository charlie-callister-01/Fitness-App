using System.Globalization;
using Core.Model;
using Core.Rules.LogKinds;
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
/// converted to the display units by <see cref="UnitConverter"/>. What fills a row per kind is in <see cref="RowSpec"/>.
/// </summary>
public static class RowPrefill
{
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
            && Fields(kind, last.Sets[setNumber - 1], units) is { } fromLast)
        {
            return fromLast with { Source = PrefillSource.LastWorkout };
        }

        if (snapshot?.Target is { } target && Fields(kind, AsSet(target), units) is { } fromTarget)
        {
            return fromTarget with { Source = PrefillSource.SuggestedTarget };
        }

        return RowPrefillValues.Empty(Hint(kind, planned));
    }

    /// <summary>
    /// The fields of a stored Set in display units, in the same form as a prefill, so a confirmed row shows (and a reopened row
    /// starts from) what was saved. Null when the Set lacks what the kind needs.
    /// </summary>
    public static RowPrefillValues? FromStored(LogKind kind, WorkoutSet set, UnitConverter units) => Fields(kind, set, units);

    /// <summary>A Suggested target as the Set it proposes, so rows and text read it like any other Set.</summary>
    public static WorkoutSet AsSet(SuggestedTarget t) => new("", t.WeightKg, t.Reps, t.DistanceKm, t.DurationSeconds, t.DistanceM, false);

    /// <summary>The fields for a Set, or null when the Set lacks what the kind needs (so the next source is tried).</summary>
    private static RowPrefillValues? Fields(LogKind kind, WorkoutSet set, UnitConverter units) =>
        RowSpec.For(kind)?.Fill(set, units) is { } fields ? new(fields.First, fields.Second, null, PrefillSource.None) : null;

    private static string? Hint(LogKind kind, SessionExercise? planned) =>
        RowSpec.For(kind) is { HintsPlannedReps: true }
            ? planned switch
            {
                { Reps: { } low, RepsMax: { } high } when high > low => $"{low}-{high}",
                { Reps: { } reps } => reps.ToString(CultureInfo.InvariantCulture),
                _ => null,
            }
            : null;
}

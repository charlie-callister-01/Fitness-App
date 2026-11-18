using Core.Model;
using Core.Rules.LogKinds;

namespace Core.Rules.Performance;

/// <summary>
/// Where an open Workout stands against an Exercise's history. <see cref="WorkoutBest"/> is the Workout's best Set so far
/// (the only Set that carries the PR marker, and only when <see cref="IsPr"/>); <see cref="PreviousBest"/> is the Best
/// before this Workout, for the "beats" and "was" text. Both are null when there is nothing rankable.
/// </summary>
public sealed record SetPrFeedback(WorkoutSet? WorkoutBest, bool IsPr, BestResult? PreviousBest);

/// <summary>
/// Per-Set PR feedback while a Workout is open. Derived on every call and never stored; it agrees with
/// <see cref="ExercisePerformance.Prs"/> once the Workout is finished.
/// </summary>
public static class SetPrFeedbackRule
{
    /// <param name="history">Every other Workout, without the open one.</param>
    /// <param name="currentSets">The open Workout's confirmed Sets so far for this Exercise, in order.</param>
    public static SetPrFeedback For(Exercise exercise, IEnumerable<Workout> history, IEnumerable<WorkoutSet> currentSets)
    {
        var previous = ExercisePerformance.Best(exercise, history);
        var current = ExercisePerformance.BestAmong(exercise.LogKind, currentSets);
        if (current is null) return new SetPrFeedback(null, false, previous);

        // The first ever result is a baseline. A Max set in history outranks any non-Max Set, whatever its key.
        var isPr = previous is not null
            && (current.IsMax || !previous.IsMax)
            && Key(exercise, current.Set) is { } currentKey
            && Key(exercise, previous.Set) is { } previousKey
            && SetRanking.Compare(currentKey, previousKey) > 0;
        return new SetPrFeedback(current.Set, isPr, previous);
    }

    /// <summary>The ranking key of a Set; null when it cannot be ranked (both Sets compared here are, having been ranked as bests).</summary>
    private static double[]? Key(Exercise exercise, WorkoutSet set) => LogKindRules.For(exercise.LogKind).Key(set);
}

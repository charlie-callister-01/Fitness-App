using Core.Model;
using Core.Rules.Outcomes;
using Core.Rules.Performance;

namespace Core.Rules.Summary;

/// <summary>
/// A PR set by the Workout: one per Exercise, its best Set, with the best before this Workout (null never occurs for a PR,
/// because the first ever result is a baseline).
/// </summary>
public sealed record SummaryPr(string ExerciseId, WorkoutSet Set, BestResult? PreviousBest);

/// <summary>
/// One Exercise line of the summary. <see cref="ExerciseId"/> is the Exercise shown (the replacement for a swap);
/// <see cref="OriginalExerciseId"/> is the planned Exercise a swap replaced, <see cref="SkipReason"/> a skip's optional reason.
/// An extra is an Exercise added beyond the plan.
/// </summary>
public sealed record SummaryRow(
    string ExerciseId,
    RowOutcomeKind Kind,
    string? SkipReason,
    string? OriginalExerciseId,
    int? Rpe,
    bool IsExtra);

/// <summary>Everything the finish summary shows. Derived, never stored.</summary>
public sealed record WorkoutSummary(
    int SetCount,
    int DoneCount,
    int PlannedCount,
    bool IsPartial,
    IReadOnlyList<SummaryPr> Prs,
    IReadOnlyList<SummaryRow> Rows,
    string PainCheck,
    string? StreakLine,
    IReadOnlyList<Badge> UnlockedBadges,
    TimeSpan? Duration)
{
    public int PrCount => Prs.Count;
}

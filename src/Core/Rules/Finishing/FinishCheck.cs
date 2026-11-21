using Core.Model;
using Core.Rules.Outcomes;

namespace Core.Rules.Finishing;

/// <summary>
/// What finishing an open Workout would do, for the Partial prompt: whether it would be a Partial workout and how many planned
/// Exercises are done. Worked out from the same rules <see cref="WorkoutFinishing.Finish"/> applies, so the prompt never disagrees
/// with the write. A Workout with no confirmed Set has a <see cref="Refusal"/> and no counts.
/// </summary>
public sealed record FinishCheck(string? Refusal, bool WouldBePartial, int DoneCount, int PlannedCount)
{
    public static FinishCheck Of(Workout workout)
    {
        var asPartial = WorkoutFinishing.Finish(workout, true, DateTimeOffset.UnixEpoch);
        if (asPartial.Workout is not { } finished)
        {
            return new FinishCheck(asPartial.Refusal, false, 0, workout.Planned.Count);
        }

        var outcome = WorkoutOutcomes.Of(finished);
        var done = outcome.Rows.Count(r => r.Kind is RowOutcomeKind.Done or RowOutcomeKind.Swapped && r.Entry is { } e && WorkoutOutcomes.HasSets(e));
        return new FinishCheck(null, outcome.IsPartial, done, workout.Planned.Count);
    }
}

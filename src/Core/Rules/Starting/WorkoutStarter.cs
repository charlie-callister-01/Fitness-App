using Core.Ids;
using Core.Model;
using Core.Storage;
using Core.Time;

namespace Core.Rules.Starting;

/// <summary>
/// What starting a Workout did. <see cref="Workout"/> is the one to open (null when nothing could be started);
/// <see cref="Commit"/> is the save's result, for the caller to report (null when nothing was saved).
/// </summary>
public sealed record StartOutcome(Workout? Workout, CommitResult? Commit)
{
    /// <summary>A Workout was already in progress: open it, save nothing.</summary>
    public static StartOutcome Continued(Workout workout) => new(workout, null);

    /// <summary>The new Workout was saved, or the save failed and there is nothing to open.</summary>
    public static StartOutcome Saved(Workout workout, CommitResult result) =>
        new(result is CommitResult.Committed ? workout : null, result);
}

/// <summary>Creates a Workout from a Session and saves it, unless one is already in progress (there is at most one).</summary>
public sealed class WorkoutStarter(AppData data, IIdGenerator ids, IClock clock)
{
    public async Task<StartOutcome> StartAsync(Plan plan, Session session, DateOnly today)
    {
        if (WorkoutStart.InProgress(data.Snapshot.Workouts) is { } existing)
        {
            return StartOutcome.Continued(existing);
        }

        var workout = WorkoutStart.Create(plan, session, today, ids, clock);
        return StartOutcome.Saved(workout, await data.CommitAsync(Change.Put(workout)));
    }
}

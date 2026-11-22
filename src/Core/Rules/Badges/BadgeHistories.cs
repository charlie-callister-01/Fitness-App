using Core.Rules.PlanEditing;
using Core.Model;
using Core.Storage;

namespace Core.Rules.Badges;

/// <summary>Builds the <see cref="BadgeHistory"/> the Badge and summary rules read from the stored data.</summary>
public static class BadgeHistories
{
    /// <summary>
    /// The history around one Workout. Its Plan is the one that holds the Workout's Session, else the active Plan; with neither,
    /// an empty Plan (a Custom workout in an app with no Plan).
    /// </summary>
    public static BadgeHistory For(DataSnapshot snapshot, Workout workout) => new(
        PlanOf(snapshot, workout),
        snapshot.Exercises,
        snapshot.Workouts,
        snapshot.Breaks,
        snapshot.WeighIns);

    private static Plan PlanOf(DataSnapshot snapshot, Workout workout) =>
        snapshot.Plans.FirstOrDefault(p => p.Sessions.Any(s => s.Id == workout.SessionId))
            ?? ActivePlan.Of(snapshot.Plans)
            ?? new Plan("none", "No Plan", false, workout.Date, null, null, 2.5,
                [new Track("none-main", "Main", new TrackRhythm.Weekly(0, 1, false), [])], []);
}

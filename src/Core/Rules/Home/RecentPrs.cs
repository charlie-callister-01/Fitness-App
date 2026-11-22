using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Performance;
using Core.Rules.Summary;
using Core.Storage;

namespace Core.Rules.Home;

public sealed record RecentPr(
    DateOnly WorkoutDate,
    string ExerciseId,
    string ExerciseName,
    WorkoutSet Set,
    BestResult? PreviousBest);

public static class RecentPrs
{
    public static IReadOnlyList<RecentPr> Latest(
        DataSnapshot snapshot,
        DateOnly today,
        int count = 5)
    {
        if (count <= 0) return [];

        var prs = new List<RecentPr>();
        var finished = snapshot.Workouts
            .Where(w => w.EndedAt is not null)
            .OrderByDescending(w => w.Date)
            .ThenByDescending(w => w.EndedAt.GetValueOrDefault());

        foreach (var workout in finished)
        {
            var history = BadgeHistories.For(snapshot, workout);
            var summary = WorkoutSummaryBuilder.Build(workout, history, snapshot.Badges, today);

            foreach (var pr in summary.Prs)
            {
                var exercise = snapshot.Exercises.FirstOrDefault(e => e.Id == pr.ExerciseId);
                var exerciseName = exercise?.Name ?? pr.ExerciseId;
                prs.Add(new RecentPr(workout.Date, pr.ExerciseId, exerciseName, pr.Set, pr.PreviousBest));

                if (prs.Count == count)
                {
                    return prs;
                }
            }
        }

        return prs;
    }
}

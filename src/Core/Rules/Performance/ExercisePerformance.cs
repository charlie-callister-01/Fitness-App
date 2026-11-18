using Core.Model;
using Core.Rules.LogKinds;

namespace Core.Rules.Performance;

/// <summary>
/// Best Set, PRs, progress points and the Last/Best snapshot for an Exercise, always worked out from the given history
/// in date order and never stored.
/// </summary>
public static class ExercisePerformance
{
    /// <summary>The best Set across history; the best Max set when any Max set exists. Null when nothing rankable was logged.</summary>
    public static BestResult? Best(Exercise exercise, IEnumerable<Workout> history) =>
        PickBest(Candidates(exercise, history));

    /// <summary>The best of some loose Sets (for example an open Workout's), with the same Max set precedence as <see cref="Best"/>.</summary>
    internal static BestResult? BestAmong(LogKind kind, IEnumerable<WorkoutSet> sets) =>
        PickBest(sets.Select(s => Candidate.Of(kind, s, default)).OfType<Candidate>());

    /// <summary>Every Workout that set a PR, in date order. The first ever result is a baseline and is not included.</summary>
    public static IReadOnlyList<PersonalRecord> Prs(Exercise exercise, IEnumerable<Workout> history)
    {
        var prs = new List<PersonalRecord>();
        var seen = new List<Candidate>();
        BestResult? previous = null;

        foreach (var visit in Visits(exercise, history))
        {
            var fromThisWorkout = CandidatesOf(exercise.LogKind, visit).ToList();
            if (fromThisWorkout.Count == 0) continue;

            seen.AddRange(fromThisWorkout);
            var current = PickBest(seen)!;
            if (previous is not null
                && current.WorkoutId == visit.Workout.Id
                && SetRanking.Compare(
                    LogKindRules.For(exercise.LogKind).Key(current.Set)!,
                    LogKindRules.For(exercise.LogKind).Key(previous.Set)!) > 0)
            {
                prs.Add(new PersonalRecord(current.WorkoutId, current.Date, current.Set));
            }
            previous = current;
        }
        return prs;
    }

    /// <summary>
    /// One point per day (the day's best Set), in date order. Empty unless the Exercise was logged on two or more days.
    /// For a body measurement the day's point is its last reading.
    /// </summary>
    public static IReadOnlyList<ProgressPoint> ProgressPoints(Exercise exercise, IEnumerable<Workout> history)
    {
        var kind = exercise.LogKind;
        var points = Visits(exercise, history)
            .SelectMany(v => v.Sets.Select(s => (v.Workout.Date, Set: s)))
            .GroupBy(x => x.Date)
            .Select(day => DayPoint(kind, day.Key, day.Select(x => x.Set).ToList()))
            .OfType<ProgressPoint>()
            .ToList();
        return points.Count >= 2 ? points : [];
    }

    /// <summary>First reading, latest reading and the change between them. Null for non-measurements or with no readings.</summary>
    public static MeasurementChange? BodyChange(Exercise exercise, IEnumerable<Workout> history)
    {
        if (!LogKindRules.For(exercise.LogKind).IsMeasurement) return null;
        var readings = Visits(exercise, history)
            .SelectMany(v => v.Sets)
            .Select(s => LogKindRules.For(exercise.LogKind).ChartValue(s))
            .OfType<double>()
            .ToList();
        return readings.Count == 0 ? null : new MeasurementChange(readings[0], readings[^1], readings[^1] - readings[0]);
    }

    /// <summary>Last and Best only; there is no Plan, so no Suggested target.</summary>
    public static ExerciseSnapshot Snapshot(Exercise exercise, IEnumerable<Workout> history) =>
        Snapshot(exercise, history, null);

    /// <summary>
    /// Last, Best and Suggested target for the log screen. The weight increment comes from the Plan, and the first row
    /// for the Exercise in Plan order gives the rep range. A null Plan gives no target.
    /// </summary>
    public static ExerciseSnapshot Snapshot(Exercise exercise, IEnumerable<Workout> history, Plan? plan)
    {
        var all = history.ToList();
        var lastVisit = Visits(exercise, all).LastOrDefault();
        var last = lastVisit is null
            ? null
            : new LastResult(
                lastVisit.Workout.Id,
                lastVisit.Workout.Date,
                lastVisit.Sets,
                lastVisit.Rpe,
                PickBest(CandidatesOf(exercise.LogKind, lastVisit))?.Set);
        var best = Best(exercise, all);
        var row = plan?.Sessions.SelectMany(s => s.Exercises).FirstOrDefault(r => r.ExerciseId == exercise.Id);
        var target = plan is null ? null : TargetRules.For(exercise.LogKind, last, best, plan, row);
        return new ExerciseSnapshot(exercise.Id, last, best, target);
    }

    private static ProgressPoint? DayPoint(LogKind kind, DateOnly date, IReadOnlyList<WorkoutSet> sets)
    {
        var rules = LogKindRules.For(kind);
        WorkoutSet? chosen = rules.IsMeasurement
            ? sets.LastOrDefault(s => rules.ChartValue(s) is not null)
            : sets
                .Select(s => Candidate.Of(kind, s, date))
                .OfType<Candidate>()
                .Aggregate((Candidate?)null, (best, c) => best is null || SetRanking.Compare(c.Key, best.Key) > 0 ? c : best)
                ?.Set;

        return chosen is not null && LogKindRules.For(kind).ChartValue(chosen) is { } value
            ? new ProgressPoint(date, value, chosen)
            : null;
    }

    private static BestResult? PickBest(IEnumerable<Candidate> candidates)
    {
        var list = candidates.ToList();
        var pool = list.Any(c => c.Set.IsMax) ? list.Where(c => c.Set.IsMax) : list;
        Candidate? best = null;
        foreach (var c in pool)
        {
            if (best is null || SetRanking.Compare(c.Key, best.Key) > 0) best = c;
        }
        return best is null ? null : new BestResult(best.Set, best.WorkoutId, best.Date, best.Set.IsMax);
    }

    private static IEnumerable<Candidate> Candidates(Exercise exercise, IEnumerable<Workout> history) =>
        Visits(exercise, history).SelectMany(v => CandidatesOf(exercise.LogKind, v));

    private static IEnumerable<Candidate> CandidatesOf(LogKind kind, Visit visit) =>
        visit.Sets
            .Select(s => Candidate.Of(kind, s, visit.Workout.Date, visit.Workout.Id))
            .OfType<Candidate>();

    /// <summary>The Workouts where the Exercise was actually done or swapped in, in chronological order.</summary>
    private static IEnumerable<Visit> Visits(Exercise exercise, IEnumerable<Workout> history) =>
        history
            .OrderBy(w => w.Date)
            .ThenBy(w => w.StartedAt ?? DateTimeOffset.MinValue)
            .ThenBy(w => w.Id, StringComparer.Ordinal)
            .Select(w =>
            {
                var entries = w.Entries
                    .Where(e => e.ExerciseId == exercise.Id && e.Status is EntryStatus.Done or EntryStatus.Swapped)
                    .ToList();
                return entries.Count == 0
                    ? null
                    : new Visit(w, entries.SelectMany(e => e.Sets).ToList(), entries.Select(e => e.Rpe).FirstOrDefault(r => r is not null));
            })
            .OfType<Visit>();

    private sealed record Visit(Workout Workout, IReadOnlyList<WorkoutSet> Sets, int? Rpe);

    private sealed record Candidate(WorkoutSet Set, double[] Key, DateOnly Date, string WorkoutId)
    {
        public static Candidate? Of(LogKind kind, WorkoutSet set, DateOnly date, string workoutId = "") =>
            LogKindRules.For(kind).Key(set) is { } key ? new Candidate(set, key, date, workoutId) : null;
    }
}

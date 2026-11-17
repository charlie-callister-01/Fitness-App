using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.Weeks;

namespace Core.Rules.Rearrangement;

/// <summary>One outstanding Session proposed for a date this week.</summary>
public sealed record ProposedPlacement(string SessionId, DateOnly Date);

/// <summary>The outcome of asking for a rearranged week. It is only ever a proposal; nothing is stored or applied.</summary>
public abstract record RearrangementResult
{
    private RearrangementResult() { }

    /// <summary>The week is already workable, so the UI should stay quiet.</summary>
    public sealed record NothingToRearrange : RearrangementResult;

    /// <summary>A draft the caller can accept or edit, ordered by date. <see cref="Unplaced"/> holds Sessions with no coming non-Break day.</summary>
    public sealed record Proposal(
        DateOnly Monday, IReadOnlyList<ProposedPlacement> Placements, IReadOnlyList<string> Unplaced) : RearrangementResult;
}

/// <summary>Week-rearrangement suggestions after a Break or a pile-up. "Today" is always passed in.</summary>
public static class WeekRearrangement
{
    /// <summary>
    /// Outstanding Sessions in today's week (Missed, or not yet reached) are re-placed on the coming non-Break days
    /// (today to Sunday) in Plan order. Each goes on the day with the fewest Sessions already, then the fewest
    /// shared <c>muscleGroups</c> tags with Sessions on the same or an adjacent day, then the earliest.
    /// Returns <see cref="RearrangementResult.NothingToRearrange"/> when nothing is outstanding or the outstanding
    /// Sessions already sit on distinct coming non-Break days with no back-to-back shared muscle group.
    /// </summary>
    public static RearrangementResult Suggest(
        Plan plan, IReadOnlyList<Exercise> exercises, IReadOnlyList<WeekSchedule> schedules,
        IReadOnlyList<Break> breaks, IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var layout = WeekScheduleRules.Layout(plan, schedules, breaks, today);
        var sunday = WeekCalendar.SundayOf(layout.Monday);
        var missed = WeekScheduleRules.MissedInWeek(plan, schedules, breaks, workouts, today, today)
            .Select(m => m.SessionId).ToHashSet();

        var doneThisWeek = workouts
            .Where(w => WeekCalendar.InWeek(layout.Monday, w.Date))
            .Select(w => w.SessionId).OfType<string>().ToHashSet();
        var placedById = ByIdFirstWins(layout.Sessions, l => l.SessionId);
        var outstanding = plan.Sessions
            .Select(s => placedById.GetValueOrDefault(s.Id))
            .OfType<ScheduledSession>()
            .Where(l => missed.Contains(l.SessionId) || (l.Date >= today && !doneThisWeek.Contains(l.SessionId)))
            .ToList();
        if (outstanding.Count == 0) return new RearrangementResult.NothingToRearrange();

        var exercisesById = ByIdFirstWins(exercises, e => e.Id);
        var groups = outstanding.ToDictionary(l => l.SessionId, l => MuscleGroups(l.Session, exercisesById));
        if (IsWorkable(outstanding, groups, today)) return new RearrangementResult.NothingToRearrange();

        var days = WeekCalendar.DaysOf(layout.Monday)
            .Where(d => d >= today && d <= sunday && !layout.BreakDays.Contains(d)).ToList();
        return Place(layout.Monday, outstanding.Select(o => o.SessionId).ToList(), groups, days);
    }

    private static bool IsWorkable(
        IReadOnlyList<ScheduledSession> outstanding, Dictionary<string, HashSet<string>> groups, DateOnly today)
    {
        if (outstanding.Any(o => o.Date < today || o.IsBreakDay)) return false;
        if (outstanding.Select(o => o.Date).Distinct().Count() != outstanding.Count) return false;
        return !outstanding.Any(a => outstanding.Any(b =>
            a.Date.DayNumber + 1 == b.Date.DayNumber && groups[a.SessionId].Overlaps(groups[b.SessionId])));
    }

    private static RearrangementResult.Proposal Place(
        DateOnly monday, IReadOnlyList<string> sessionIds, Dictionary<string, HashSet<string>> groups, List<DateOnly> days)
    {
        var placed = new List<ProposedPlacement>();
        var unplaced = new List<string>();
        foreach (var id in sessionIds)
        {
            if (days.Count == 0) { unplaced.Add(id); continue; }
            var best = days
                .OrderBy(d => placed.Count(p => p.Date == d))
                .ThenBy(d => placed.Count(p => Math.Abs(p.Date.DayNumber - d.DayNumber) <= 1 && groups[p.SessionId].Overlaps(groups[id])))
                .ThenBy(d => d)
                .First();
            placed.Add(new(id, best));
        }

        return new(monday, placed.OrderBy(p => p.Date).ToList(), unplaced);
    }

    /// <summary>Indexes by key; on a repeated key the first wins (the validator rejects repeats, but a rule must not throw on them).</summary>
    private static Dictionary<string, T> ByIdFirstWins<T>(IEnumerable<T> items, Func<T, string> key)
    {
        var byId = new Dictionary<string, T>();
        foreach (var item in items) byId.TryAdd(key(item), item);
        return byId;
    }

    private static HashSet<string> MuscleGroups(Session session, Dictionary<string, Exercise> byId)
    {
        return session.Exercises
            .Select(r => byId.GetValueOrDefault(r.ExerciseId)?.Tags?.MuscleGroups)
            .OfType<IReadOnlyList<string>>()
            .SelectMany(g => g)
            .ToHashSet();
    }
}

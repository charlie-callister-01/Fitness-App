using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>Cuts the part of a snapshot a request asks for, as an exchange document. Pure; the format and wording are applied elsewhere.</summary>
internal static class ExportSlicer
{
    public static ExchangeDocument Slice(DataSnapshot snapshot, ExportRequest request)
    {
        if (request.IsBackup)
        {
            return snapshot.ToDocument();
        }

        var plans = request.Plan ? ActivePlan(snapshot) : null;
        var workouts = SliceWorkouts(snapshot, request, plans);
        var referenced = ReferencedExerciseIds(plans, workouts);
        var exercises = request.Exercises ? snapshot.Exercises : [.. snapshot.Exercises.Where(e => referenced.Contains(e.Id))];

        return new ExchangeDocument(
            FormatVersion: ExchangeFormat.CurrentVersion,
            ExportedAt: null,
            ExportedFrom: null,
            Exercises: exercises,
            Settings: null,
            WeighIns: request.WeighIns ? [.. snapshot.WeighIns.Where(w => InRange(request, w.Date))] : null,
            Breaks: request.Plan ? [.. snapshot.Breaks.Where(b => request.Range is null || request.Range.Overlaps(b.StartDate, b.EndDate))] : null,
            Badges: null,
            Guides: request.Guides ? SliceGuides(snapshot, exercises) : null,
            Plans: plans,
            Workouts: workouts,
            WeekSchedules: plans is null ? null : SliceWeekSchedules(snapshot, request, plans));
    }

    private static bool InRange(ExportRequest request, DateOnly date) => request.Range?.Contains(date) ?? true;

    private static IReadOnlyList<Plan> ActivePlan(DataSnapshot snapshot) => [.. snapshot.Plans.Where(p => p.IsActive).Take(1)];

    private static IReadOnlyList<Workout>? SliceWorkouts(DataSnapshot snapshot, ExportRequest request, IReadOnlyList<Plan>? plans)
    {
        if (!request.Workouts && !request.PainChecks)
        {
            return null;
        }

        var sessionIds = plans?.SelectMany(p => p.Sessions).Select(s => s.Id).ToHashSet();
        var inRange = snapshot.Workouts.Where(w => InRange(request, w.Date));
        var shaped = request.Workouts
            ? inRange.Select(w => request.PainChecks ? w : w with { PainCheck = null })
            : inRange.Where(w => w.PainCheck is not null).Select(w => w with { Planned = [], Entries = [] });

        // A Workout of a Plan that is not in this Export keeps its date, rows and Sets but no Session link, or the file would point at nothing.
        return [.. shaped.Select(w => sessionIds is not null && w.SessionId is { } id && !sessionIds.Contains(id) ? w with { SessionId = null } : w)];
    }

    private static IReadOnlyList<WeekSchedule> SliceWeekSchedules(DataSnapshot snapshot, ExportRequest request, IReadOnlyList<Plan> plans)
    {
        var sessionIds = plans.SelectMany(p => p.Sessions).Select(s => s.Id).ToHashSet();
        return
        [
            .. snapshot.WeekSchedules
                .Where(w => request.Range is null || request.Range.Overlaps(w.WeekStart, w.WeekStart.AddDays(6)))
                .Select(w => w with { Moves = [.. w.Moves.Where(m => sessionIds.Contains(m.SessionId))] }),
        ];
    }

    /// <summary>The Guides of the exported Exercises, without the owner's personal notes (only a Backup keeps those).</summary>
    private static IReadOnlyList<Guide> SliceGuides(DataSnapshot snapshot, IReadOnlyList<Exercise> exercises)
    {
        var ids = exercises.Select(e => e.Id).ToHashSet();
        return [.. snapshot.Guides.Where(g => ids.Contains(g.ExerciseId)).Select(g => g with { PersonalNotes = null })];
    }

    /// <summary>Every Exercise ID that the exported Plan and Workouts name, so the file is readable and importable on its own.</summary>
    private static HashSet<string> ReferencedExerciseIds(IReadOnlyList<Plan>? plans, IReadOnlyList<Workout>? workouts)
    {
        var rows = (plans ?? []).SelectMany(p => p.Sessions.SelectMany(s => s.Exercises)
            .Concat(p.Phases.SelectMany(ph => ph.SessionOverrides.SelectMany(o => o.Exercises))));
        var goals = (plans ?? []).SelectMany(p => p.Phases.SelectMany(ph => ph.Goals)).Select(g => g.ExerciseId);
        var fromWorkouts = (workouts ?? []).SelectMany(w =>
            w.Planned.Select(r => r.ExerciseId)
                .Concat(w.Entries.SelectMany(e => new[] { e.ExerciseId, e.SwappedFromExerciseId }.OfType<string>()))
                .Concat(new[] { w.PainCheck?.ExerciseId }.OfType<string>()));
        return [.. rows.Select(r => r.ExerciseId).Concat(goals).Concat(fromWorkouts)];
    }
}

using Core.Ids;
using Core.Model;
using Core.Rules.Tracks;

namespace Core.Rules.PlanEditing;

/// <summary>The rules for editing a Plan's Sessions and the Exercise rows in them. Past Workouts hold their own planned rows, so they never change.</summary>
public static class SessionRowRules
{
    /// <summary>Why a row's numbers cannot be saved, or null: Sets at least 1, Reps at least 1 when given, the maximum not below Reps.</summary>
    public static string? ValidateRow(SessionExercise row)
    {
        if (row.Sets < 1)
        {
            return "Sets must be at least 1.";
        }

        if (row.Reps is < 1)
        {
            return "Reps must be at least 1.";
        }

        if (row.RepsMax is { } max && (row.Reps is null || max < row.Reps))
        {
            return "The highest Reps cannot be below Reps.";
        }

        return null;
    }

    /// <summary>A row in a few words: "3 x 8", "3 x 8 to 12" or, with no Reps, "3 sets".</summary>
    public static string Summary(SessionExercise row) => (row.Reps, row.RepsMax) switch
    {
        (null, _) => $"{row.Sets} sets",
        (var reps, null) => $"{row.Sets} x {reps}",
        (var reps, var max) when max == reps => $"{row.Sets} x {reps}",
        (var reps, var max) => $"{row.Sets} x {reps} to {max}",
    };

    /// <summary>The first reason any row cannot be saved, or null when all can.</summary>
    public static string? ValidateRows(IEnumerable<SessionExercise> rows) => rows.Select(ValidateRow).FirstOrDefault(r => r is not null);

    /// <summary>The rows with the one at <paramref name="index"/> swapped with the one above it. The first row (or a bad index) stays put.</summary>
    public static IReadOnlyList<SessionExercise> MoveUp(IReadOnlyList<SessionExercise> rows, int index) => Swap(rows, index, index - 1);

    /// <summary>The rows with the one at <paramref name="index"/> swapped with the one below it. The last row (or a bad index) stays put.</summary>
    public static IReadOnlyList<SessionExercise> MoveDown(IReadOnlyList<SessionExercise> rows, int index) => Swap(rows, index, index + 1);

    /// <summary>The rows without the one at <paramref name="index"/>. Removing the last row leaves a Session with no Exercises.</summary>
    public static IReadOnlyList<SessionExercise> Remove(IReadOnlyList<SessionExercise> rows, int index) =>
        index < 0 || index >= rows.Count ? rows : [.. rows.Where((_, i) => i != index)];

    /// <summary>The rows with <paramref name="exerciseId"/> appended at the end (3 sets of 8), unless the Session already has it.</summary>
    public static IReadOnlyList<SessionExercise> AddFromLibrary(IReadOnlyList<SessionExercise> rows, string exerciseId) =>
        rows.Any(r => r.ExerciseId == exerciseId) ? rows : [.. rows, new SessionExercise(exerciseId, 3, 8, null, null)];

    /// <summary>The Plan with the Session's default Exercise rows replaced (a Phase's override rows are not touched).</summary>
    public static Plan WithRows(Plan plan, string sessionId, IReadOnlyList<SessionExercise> rows) =>
        ReplaceSession(plan, sessionId, s => s with { Exercises = rows });

    /// <summary>The Plan with the Session's name, default day and type changed. A blank name, a day change on a daily Session, or daily on a weekly Track is refused.</summary>
    public static PlanEditResult<Plan> EditSession(Plan plan, string sessionId, string? name, DefaultDay day, SessionType type)
    {
        if (plan.Sessions.FirstOrDefault(s => s.Id == sessionId) is not { } existing)
        {
            return Invalid("That Session is no longer in the Plan.");
        }

        return EditSession(plan, sessionId, name, day, type, type == SessionType.Test ? existing.BenchmarkIntervalWeeks : null);
    }

    /// <summary>
    /// The Plan with the Session's name, default day, type and benchmark interval changed. A blank name, a day change on a daily Session,
    /// daily on a weekly Track, or a benchmark interval off the main Track (or on a non-Test Session) is refused.
    /// </summary>
    public static PlanEditResult<Plan> EditSession(
        Plan plan, string sessionId, string? name, DefaultDay day, SessionType type, int? benchmarkIntervalWeeks)
    {
        if (plan.Sessions.FirstOrDefault(s => s.Id == sessionId) is not { } existing)
        {
            return Invalid("That Session is no longer in the Plan.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Invalid("Give the Session a name.");
        }

        var track = TrackRules.TrackOf(plan, existing);
        var isDaily = track?.Rhythm is TrackRhythm.Daily || existing.DefaultDay == DefaultDay.Daily;
        if (isDaily && day != DefaultDay.Daily)
        {
            return Invalid("A daily Session is on every day and its day cannot be changed.");
        }

        if (!isDaily && day == DefaultDay.Daily)
        {
            return Invalid("A weekly Track Session must be on a day of the week, not daily.");
        }

        if (ValidateBenchmark(plan, track, type, benchmarkIntervalWeeks) is { } benchmarkError)
        {
            return Invalid(benchmarkError);
        }

        return new PlanEditResult<Plan>.Valid(ReplaceSession(plan, sessionId, s => s with
        {
            Name = name.Trim(),
            DefaultDay = day,
            Type = type,
            BenchmarkIntervalWeeks = type == SessionType.Test ? benchmarkIntervalWeeks : null,
        }));
    }

    /// <summary>
    /// The Plan with a new, empty Session added at the end on <paramref name="trackId"/>. A daily Track always gets
    /// <see cref="DefaultDay.Daily"/>; <see cref="DefaultDay.Daily"/> on a weekly Track, a blank name, and a benchmark interval off the
    /// main Track are refused.
    /// </summary>
    public static PlanEditResult<Plan> AddSession(
        Plan plan, string trackId, string? name, DefaultDay day, SessionType type, IIdGenerator ids, int? benchmarkIntervalWeeks = null)
    {
        if (plan.Tracks.FirstOrDefault(t => t.Id == trackId) is not { } track)
        {
            return Invalid("That Track is no longer in the Plan.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Invalid("Give the Session a name.");
        }

        DefaultDay resolvedDay;
        if (track.Rhythm is TrackRhythm.Daily)
        {
            resolvedDay = DefaultDay.Daily;
        }
        else if (day == DefaultDay.Daily)
        {
            return Invalid("A weekly Track Session must be on a day of the week, not daily.");
        }
        else
        {
            resolvedDay = day;
        }

        if (ValidateBenchmark(plan, track, type, benchmarkIntervalWeeks) is { } benchmarkError)
        {
            return Invalid(benchmarkError);
        }

        var session = new Session(ids.NewId(), name.Trim(), track.Id, resolvedDay, type, type == SessionType.Test ? benchmarkIntervalWeeks : null, []);
        return new PlanEditResult<Plan>.Valid(plan with { Sessions = [.. plan.Sessions, session] });
    }

    private static string? ValidateBenchmark(Plan plan, Track? track, SessionType type, int? benchmarkIntervalWeeks)
    {
        if (benchmarkIntervalWeeks is not { } interval)
        {
            return null;
        }

        if (plan.Tracks.Count == 0 || track?.Id != TrackRules.Main(plan).Id)
        {
            return "Only a Session on the main Track can have a benchmark interval.";
        }

        if (type != SessionType.Test)
        {
            return "Only a Test Session can have a benchmark interval.";
        }

        if (interval < 1)
        {
            return "The benchmark interval must be at least 1 week.";
        }

        return null;
    }

    private static PlanEditResult<Plan> Invalid(string message) => new PlanEditResult<Plan>.Invalid(message);

    private static IReadOnlyList<SessionExercise> Swap(IReadOnlyList<SessionExercise> rows, int a, int b)
    {
        if (a < 0 || b < 0 || a >= rows.Count || b >= rows.Count)
        {
            return rows;
        }

        var swapped = rows.ToList();
        (swapped[a], swapped[b]) = (swapped[b], swapped[a]);
        return swapped;
    }

    private static Plan ReplaceSession(Plan plan, string sessionId, Func<Session, Session> change) =>
        plan with { Sessions = [.. plan.Sessions.Select(s => s.Id == sessionId ? change(s) : s)] };
}

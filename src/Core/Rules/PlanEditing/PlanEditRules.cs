using Core.Ids;
using Core.Model;
using Core.Rules.Weeks;

namespace Core.Rules.PlanEditing;

/// <summary>The Plan-level values the Settings tab edits.</summary>
public sealed record PlanSettings(
    DateOnly StartDate,
    string? Goal,
    DateOnly? GoalDate,
    int WeeklyTarget,
    int StreakMinWorkoutsPerWeek,
    bool StreakCountsCustomWorkouts,
    double WeightIncrementKg);

/// <summary>What an edit that can be refused came to: the changed value, or the plain reason it was refused (nothing changes).</summary>
public abstract record PlanEditResult<T>
{
    public sealed record Valid(T Value) : PlanEditResult<T>;

    public sealed record Invalid(string Message) : PlanEditResult<T>;
}

/// <summary>
/// The rules for editing a Plan. Every rule returns a changed copy and never touches Workouts: a Workout already holds its own planned
/// rows, so edits apply going forward and past Workouts are unchanged.
/// </summary>
public static class PlanEditRules
{
    /// <summary>The Plan with its Settings replaced, or the plain reason they are refused.</summary>
    public static PlanEditResult<Plan> WithSettings(Plan plan, PlanSettings settings)
    {
        if (settings.WeeklyTarget < 1)
        {
            return Invalid<Plan>("The weekly target must be at least 1.");
        }

        if (settings.StreakMinWorkoutsPerWeek < 1)
        {
            return Invalid<Plan>("The Streak minimum must be at least 1.");
        }

        if (settings.GoalDate is { } goalDate && goalDate < settings.StartDate)
        {
            return Invalid<Plan>("The goal date cannot be before the start date.");
        }

        if (settings.WeightIncrementKg <= 0)
        {
            return Invalid<Plan>("The weight increment must be more than 0.");
        }

        return new PlanEditResult<Plan>.Valid(plan with
        {
            StartDate = settings.StartDate,
            Goal = string.IsNullOrWhiteSpace(settings.Goal) ? null : settings.Goal.Trim(),
            GoalDate = settings.GoalDate,
            WeeklyTarget = settings.WeeklyTarget,
            StreakMinWorkoutsPerWeek = settings.StreakMinWorkoutsPerWeek,
            StreakCountsCustomWorkouts = settings.StreakCountsCustomWorkouts,
            WeightIncrementKg = settings.WeightIncrementKg,
        });
    }

    // ---- Phases

    /// <summary>The first and last day of a Phase, from the Plan's start week (weeks are numbered from 1 and run Monday to Sunday).</summary>
    public static (DateOnly Start, DateOnly End) PhaseDates(Plan plan, Phase phase)
    {
        var firstMonday = WeekCalendar.MondayOf(plan.StartDate);
        return (WeekCalendar.AddWeeks(firstMonday, phase.StartWeek - 1), WeekCalendar.AddWeeks(firstMonday, phase.EndWeek - 1).AddDays(6));
    }

    /// <summary>
    /// Why the Plan's Phases cannot be saved, or null when they can: a Phase must end no earlier than it starts and start at week 1 or
    /// later; two Phases may not share a week (the message names both); and Phases must start at week 1 and leave no gap.
    /// </summary>
    public static string? ValidatePhases(IReadOnlyList<Phase> phases)
    {
        foreach (var phase in phases)
        {
            if (phase.StartWeek < 1)
            {
                return $"{Name(phase)} must start at week 1 or later.";
            }

            if (phase.EndWeek < phase.StartWeek)
            {
                return $"{Name(phase)} ends (week {phase.EndWeek}) before it starts (week {phase.StartWeek}).";
            }
        }

        var ordered = phases.OrderBy(p => p.StartWeek).ThenBy(p => p.EndWeek).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var earlier = ordered[i - 1];
            var later = ordered[i];
            if (later.StartWeek <= earlier.EndWeek)
            {
                return $"{Name(later)} (weeks {later.StartWeek} to {later.EndWeek}) overlaps {Name(earlier)} (weeks {earlier.StartWeek} to {earlier.EndWeek}).";
            }

            if (later.StartWeek > earlier.EndWeek + 1)
            {
                return $"Weeks {earlier.EndWeek + 1} to {later.StartWeek - 1} are between {Name(earlier)} and {Name(later)} and in no Phase.";
            }
        }

        return ordered.Count > 0 && ordered[0].StartWeek > 1
            ? $"Weeks 1 to {ordered[0].StartWeek - 1} are before {Name(ordered[0])} and in no Phase. Phases must start at week 1."
            : null;
    }

    /// <summary>The Plan with <paramref name="phases"/> as its Phases, or the plain reason they are refused (see <see cref="ValidatePhases"/>).</summary>
    public static PlanEditResult<Plan> WithPhases(Plan plan, IReadOnlyList<Phase> phases) =>
        ValidatePhases(phases) is { } reason
            ? Invalid<Plan>(reason)
            : new PlanEditResult<Plan>.Valid(plan with { Phases = [.. phases.OrderBy(p => p.StartWeek)] });

    private static string Name(Phase phase) => string.IsNullOrWhiteSpace(phase.Focus) ? "A Phase" : $"\"{phase.Focus}\"";

    // ---- Session rows

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

    // ---- Sessions

    /// <summary>The Plan with the Session's name, default day and type changed. A blank name is refused.</summary>
    public static PlanEditResult<Plan> EditSession(Plan plan, string sessionId, string? name, DayOfWeek day, SessionType type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Invalid<Plan>("Give the Session a name.");
        }

        return new PlanEditResult<Plan>.Valid(ReplaceSession(plan, sessionId, s => s with
        {
            Name = name.Trim(),
            DefaultDay = day,
            Type = type,
            BenchmarkIntervalWeeks = type == SessionType.Test ? s.BenchmarkIntervalWeeks : null,
        }));
    }

    /// <summary>The Plan with a new, empty Session added at the end. A blank name is refused.</summary>
    public static PlanEditResult<Plan> AddSession(Plan plan, string? name, DayOfWeek day, SessionType type, IIdGenerator ids)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Invalid<Plan>("Give the Session a name.");
        }

        var session = new Session(ids.NewId(), name.Trim(), day, type, null, []);
        return new PlanEditResult<Plan>.Valid(plan with { Sessions = [.. plan.Sessions, session] });
    }

    private static Plan ReplaceSession(Plan plan, string sessionId, Func<Session, Session> change) =>
        plan with { Sessions = [.. plan.Sessions.Select(s => s.Id == sessionId ? change(s) : s)] };

    // ---- Plans

    /// <summary>
    /// A copy of the Plan under <paramref name="name"/>: same content, inactive, with fresh ids for the Plan, its Sessions and its Phases
    /// (a Phase's overrides follow their Sessions' new ids). Exercises are shared, so their ids stay.
    /// </summary>
    public static Plan Copy(Plan plan, string name, IIdGenerator ids)
    {
        var newSessionIds = plan.Sessions.ToDictionary(s => s.Id, _ => ids.NewId());
        var sessions = plan.Sessions.Select(s => s with { Id = newSessionIds[s.Id] }).ToList();
        var phases = plan.Phases.Select(p => p with
        {
            Id = ids.NewId(),
            SessionOverrides = [.. p.SessionOverrides.Select(o => o with { SessionId = newSessionIds.GetValueOrDefault(o.SessionId, o.SessionId) })],
        }).ToList();
        return plan with { Id = ids.NewId(), Name = name.Trim(), IsActive = false, Phases = phases, Sessions = sessions };
    }

    /// <summary>
    /// A new, inactive Plan with no Phases and no Sessions, starting on <paramref name="startDate"/> with the app's defaults. A blank name
    /// is refused.
    /// </summary>
    public static PlanEditResult<Plan> FromScratch(string? name, DateOnly startDate, IIdGenerator ids) =>
        string.IsNullOrWhiteSpace(name)
            ? Invalid<Plan>("Give the Plan a name.")
            : new PlanEditResult<Plan>.Valid(
                new Plan(ids.NewId(), name.Trim(), false, startDate, null, null, 3, 2, false, 2.5, [], []));

    /// <summary>
    /// The Plans that change when <paramref name="planId"/> becomes the active one: that Plan and the one that was active, so one commit
    /// leaves exactly one active Plan. Empty when it already is the active one or does not exist.
    /// </summary>
    public static IReadOnlyList<Plan> MakeActive(IReadOnlyList<Plan> plans, string planId)
    {
        if (plans.FirstOrDefault(p => p.Id == planId) is not { IsActive: false } target)
        {
            return [];
        }

        return [.. plans.Where(p => p.IsActive && p.Id != planId).Select(p => p with { IsActive = false }), target with { IsActive = true }];
    }

    private static PlanEditResult<T> Invalid<T>(string message) => new PlanEditResult<T>.Invalid(message);
}

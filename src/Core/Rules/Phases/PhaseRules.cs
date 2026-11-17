using Core.Model;
using Core.Rules.Weeks;

namespace Core.Rules.Phases;

/// <summary>Where "today" sits in the Plan.</summary>
public enum PhaseState
{
    /// <summary>Today is before the Plan's start date.</summary>
    BeforeStart,

    /// <summary>Today falls in a Phase's week range.</summary>
    InPhase,

    /// <summary>Today is after the last Phase's final week.</summary>
    AfterEnd,

    /// <summary>The Plan has started but no Phase covers this week (a gap, or a Plan with no Phases).</summary>
    NoPhase,
}

/// <summary>The Plan week, and when in a Phase, the Phase with "week N of M". Phase and week fields are null unless <see cref="State"/> is InPhase.</summary>
public sealed record PhasePosition(PhaseState State, int? PlanWeek, Phase? Phase, int? WeekInPhase, int? PhaseLengthWeeks);

public sealed record GoalStatus(PhaseGoal Goal, bool Met);

public sealed record PhaseReadiness(IReadOnlyList<GoalStatus> Goals, bool AllMet, string Message);

/// <summary>Phase position, Phase goal readiness and Override row resolution, all derived from the data and an explicit date.</summary>
public static class PhaseRules
{
    public static PhasePosition Position(Plan plan, DateOnly today)
    {
        if (WeekRules.PlanWeekNumber(plan, today) is not { } week)
            return new(PhaseState.BeforeStart, null, null, null, null);

        var phase = PhaseOfWeek(plan, week);
        if (phase is not null)
            return new(PhaseState.InPhase, week, phase, week - phase.StartWeek + 1, phase.EndWeek - phase.StartWeek + 1);

        var state = plan.Phases.Count > 0 && week > plan.Phases.Max(p => p.EndWeek) ? PhaseState.AfterEnd : PhaseState.NoPhase;
        return new(state, week, null, null, null);
    }

    /// <summary>
    /// A goal is met when any done or swapped-in Set of its Exercise has weight at least the goal weight and reps at least
    /// the goal reps (assumption 8). A Phase with no goals is never ready.
    /// </summary>
    public static PhaseReadiness GoalReadiness(Phase phase, IEnumerable<Workout> history)
    {
        if (phase.Goals.Count == 0) return new([], false, "This Phase has no goals set.");

        var entries = history.SelectMany(w => w.Entries)
            .Where(e => e.Status is EntryStatus.Done or EntryStatus.Swapped)
            .ToList();
        var goals = phase.Goals
            .Select(g => new GoalStatus(g, entries.Any(e => e.ExerciseId == g.ExerciseId && e.Sets.Any(s => Meets(s, g)))))
            .ToList();
        var met = goals.Count(g => g.Met);
        return goals.Count == met
            ? new(goals, true, "All Phase goals met: ready to move on early.")
            : new(goals, false, $"{met} of {goals.Count} Phase goals met.");
    }

    /// <summary>The Exercise rows of a Session today: the current Phase's Override when it has one, else the Plan default. Empty for an unknown Session.</summary>
    public static IReadOnlyList<SessionExercise> ExerciseRows(Plan plan, string sessionId, DateOnly today) =>
        ExerciseRows(plan, CurrentPhase(plan, today), sessionId);

    /// <summary>As <see cref="ExerciseRows(Plan, string, DateOnly)"/>, for a caller that has already resolved the current Phase (null when none).</summary>
    public static IReadOnlyList<SessionExercise> ExerciseRows(Plan plan, Phase? currentPhase, string sessionId)
    {
        var session = plan.Sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session is null) return [];
        var over = currentPhase?.SessionOverrides.FirstOrDefault(o => o.SessionId == sessionId);
        return over?.Exercises ?? session.Exercises;
    }

    /// <summary>The Phase whose week range contains today, or null before the Plan starts, in a gap, or after the last Phase.</summary>
    public static Phase? CurrentPhase(Plan plan, DateOnly today) =>
        WeekRules.PlanWeekNumber(plan, today) is { } week ? PhaseOfWeek(plan, week) : null;

    private static Phase? PhaseOfWeek(Plan plan, int week) =>
        plan.Phases.FirstOrDefault(p => week >= p.StartWeek && week <= p.EndWeek);

    /// <summary>
    /// A goal's Set meets it when it has a weight and reps, each at least the goal's. A Set missing either (a time, a distance)
    /// never meets a weight-and-reps goal.
    /// </summary>
    private static bool Meets(WorkoutSet set, PhaseGoal goal) =>
        set.WeightKg is { } weight && set.Reps is { } reps && weight >= goal.WeightKg && reps >= goal.Reps;
}

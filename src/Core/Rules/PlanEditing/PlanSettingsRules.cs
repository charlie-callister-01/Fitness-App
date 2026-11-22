using Core.Model;

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

/// <summary>The rule for editing a Plan's Settings. It returns a changed copy and never touches Workouts.</summary>
public static class PlanSettingsRules
{
    /// <summary>The Plan with its Settings replaced, or the plain reason they are refused.</summary>
    public static PlanEditResult<Plan> WithSettings(Plan plan, PlanSettings settings)
    {
        if (settings.WeeklyTarget < 1)
        {
            return Invalid("The weekly target must be at least 1.");
        }

        if (settings.StreakMinWorkoutsPerWeek < 1)
        {
            return Invalid("The Streak minimum must be at least 1.");
        }

        if (settings.GoalDate is { } goalDate && goalDate < settings.StartDate)
        {
            return Invalid("The goal date cannot be before the start date.");
        }

        if (settings.WeightIncrementKg <= 0)
        {
            return Invalid("The weight increment must be more than 0.");
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

    private static PlanEditResult<Plan> Invalid(string message) => new PlanEditResult<Plan>.Invalid(message);
}

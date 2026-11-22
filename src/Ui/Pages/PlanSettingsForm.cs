using System.Globalization;
using Core.Model;
using Core.Rules.PlanEditing;

namespace Ui.Pages;

/// <summary>
/// The Settings tab's fields as the owner typed them. Text stays text until it is read (<see cref="Read"/>), so a half-typed number is
/// kept on screen and only blocks Save, and the form survives a switch of tab. Two forms are equal when every field is.
/// </summary>
public sealed record PlanSettingsForm
{
    private const string DateFormat = "yyyy-MM-dd";

    public string StartDate { get; set; } = "";

    public string Goal { get; set; } = "";

    public string GoalDate { get; set; } = "";

    public string WeeklyTarget { get; set; } = "";

    public string StreakMinimum { get; set; } = "";

    public bool CustomWorkoutsCount { get; set; }

    public string WeightIncrement { get; set; } = "";

    public static PlanSettingsForm From(Plan plan) => new()
    {
        StartDate = plan.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
        Goal = plan.Goal ?? "",
        GoalDate = plan.GoalDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "",
        WeeklyTarget = plan.WeeklyTarget.ToString(CultureInfo.InvariantCulture),
        StreakMinimum = plan.StreakMinWorkoutsPerWeek.ToString(CultureInfo.InvariantCulture),
        CustomWorkoutsCount = plan.StreakCountsCustomWorkouts,
        WeightIncrement = plan.WeightIncrementKg.ToString("0.##", CultureInfo.InvariantCulture),
    };

    /// <summary>The settings these fields spell out, or the plain reason they cannot be read or are refused by the Plan rules.</summary>
    public PlanEditResult<PlanSettings> Read(Plan plan)
    {
        if (!DateOnly.TryParseExact(StartDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            return Invalid("Pick a start date.");
        }

        DateOnly? goalDate = null;
        if (!string.IsNullOrWhiteSpace(GoalDate))
        {
            if (!DateOnly.TryParseExact(GoalDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return Invalid("The goal date is not a date.");
            }

            goalDate = parsed;
        }

        if (!int.TryParse(WeeklyTarget, NumberStyles.None, CultureInfo.InvariantCulture, out var target))
        {
            return Invalid("The weekly target must be a whole number, at least 1.");
        }

        if (!int.TryParse(StreakMinimum, NumberStyles.None, CultureInfo.InvariantCulture, out var minimum))
        {
            return Invalid("The Streak minimum must be a whole number, at least 1.");
        }

        if (!double.TryParse(WeightIncrement.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var increment))
        {
            return Invalid("The weight increment must be a number, more than 0.");
        }

        var settings = new PlanSettings(start, Goal, goalDate, target, minimum, CustomWorkoutsCount, increment);
        return PlanEditRules.WithSettings(plan, settings) is PlanEditResult<Plan>.Invalid refused
            ? Invalid(refused.Message)
            : new PlanEditResult<PlanSettings>.Valid(settings);
    }

    private static PlanEditResult<PlanSettings> Invalid(string message) => new PlanEditResult<PlanSettings>.Invalid(message);
}

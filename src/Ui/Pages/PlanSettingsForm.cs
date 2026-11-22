using System.Globalization;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.PlanEditing;
using Core.Rules.Tracks;

namespace Ui.Pages;

/// <summary>The Plan settings and main Track settings parsed from <see cref="PlanSettingsForm"/>.</summary>
public sealed record PlanFormSettings(PlanSettings Plan, TrackSettings Track);

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

    public static PlanSettingsForm From(Plan plan)
    {
        var weekly = TrackRules.Main(plan).Rhythm as TrackRhythm.Weekly;
        return new()
        {
            StartDate = plan.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            Goal = plan.Goal ?? "",
            GoalDate = plan.GoalDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "",
            WeeklyTarget = (weekly?.WeeklyTarget ?? 0).ToString(CultureInfo.InvariantCulture),
            StreakMinimum = (weekly?.StreakMinWorkoutsPerWeek ?? 0).ToString(CultureInfo.InvariantCulture),
            CustomWorkoutsCount = weekly?.StreakCountsCustomWorkouts ?? false,
            WeightIncrement = plan.WeightIncrementKg.ToString("0.##", CultureInfo.InvariantCulture),
        };
    }

    /// <summary>The settings these fields spell out, or the plain reason they cannot be read or are refused by the Plan or Track rules.</summary>
    public PlanEditResult<PlanFormSettings> Read(Plan plan)
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

        if (!DecimalText.TryParse(WeightIncrement, out var increment))
        {
            return Invalid("The weight increment must be a number, more than 0.");
        }

        var planSettings = new PlanSettings(start, Goal, goalDate, increment);
        var trackSettings = new TrackSettings.Weekly(target, minimum, CustomWorkoutsCount);
        if (!PlanSettingsRules.WithTrackSettings(TrackRules.Main(plan), trackSettings).TryGet(out _, out var trackReason))
        {
            return Invalid(trackReason);
        }

        return PlanSettingsRules.WithSettings(plan, planSettings)
            .Match<PlanEditResult<PlanFormSettings>>(
                _ => new PlanEditResult<PlanFormSettings>.Valid(new PlanFormSettings(planSettings, trackSettings)),
                Invalid);
    }

    private static PlanEditResult<PlanFormSettings> Invalid(string message) => new PlanEditResult<PlanFormSettings>.Invalid(message);
}

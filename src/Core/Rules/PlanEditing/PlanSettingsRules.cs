using Core.Model;

namespace Core.Rules.PlanEditing;

/// <summary>The Plan-level values the Settings tab edits.</summary>
public sealed record PlanSettings(
    DateOnly StartDate,
    string? Goal,
    DateOnly? GoalDate,
    double WeightIncrementKg);

/// <summary>The cadence-specific values a Track's Settings edit.</summary>
public abstract record TrackSettings
{
    private TrackSettings()
    {
    }

    public sealed record Weekly(
        int WeeklyTarget,
        int StreakMinWorkoutsPerWeek,
        bool StreakCountsCustomWorkouts) : TrackSettings;

    public sealed record Daily(int DayDonePercent) : TrackSettings;
}

/// <summary>The rules for editing Plan settings and Track settings. They return changed copies and never touch Workouts.</summary>
public static class PlanSettingsRules
{
    /// <summary>The Plan with its Plan-level Settings replaced, or the plain reason they are refused.</summary>
    public static PlanEditResult<Plan> WithSettings(Plan plan, PlanSettings settings)
    {
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
            WeightIncrementKg = settings.WeightIncrementKg,
        });
    }

    /// <summary>The Track with its rhythm settings replaced, or the plain reason they are refused. Cadence cannot change.</summary>
    public static PlanEditResult<Track> WithTrackSettings(Track track, TrackSettings settings) =>
        (track.Rhythm, settings) switch
        {
            (TrackRhythm.Weekly, TrackSettings.Weekly w) when w.WeeklyTarget < 1 =>
                Invalid<Track>("The weekly target must be at least 1."),
            (TrackRhythm.Weekly, TrackSettings.Weekly w) when w.StreakMinWorkoutsPerWeek < 1 =>
                Invalid<Track>("The Streak minimum must be at least 1."),
            (TrackRhythm.Weekly, TrackSettings.Weekly w) =>
                new PlanEditResult<Track>.Valid(track with
                {
                    Rhythm = new TrackRhythm.Weekly(w.WeeklyTarget, w.StreakMinWorkoutsPerWeek, w.StreakCountsCustomWorkouts),
                }),
            (TrackRhythm.Daily, TrackSettings.Daily d) when d.DayDonePercent is < 1 or > 100 =>
                Invalid<Track>("The day-done percentage must be between 1 and 100."),
            (TrackRhythm.Daily, TrackSettings.Daily d) =>
                new PlanEditResult<Track>.Valid(track with { Rhythm = new TrackRhythm.Daily(d.DayDonePercent) }),
            _ => Invalid<Track>("A Track's cadence cannot be changed."),
        };

    private static PlanEditResult<T> Invalid<T>(string message) => new PlanEditResult<T>.Invalid(message);
}

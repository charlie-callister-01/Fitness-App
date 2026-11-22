using System.Globalization;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.PlanEditing;

namespace Ui.Pages;

/// <summary>
/// One Track's Settings tab fields as the owner typed them. Text stays text until it is read (<see cref="ReadSettings"/>), so a half-typed
/// number or name is kept on screen across tab and Track switches and only blocks Save.
/// </summary>
public sealed record TrackSettingsForm
{
    public string Name { get; set; } = "";

    public TrackCadence Cadence { get; init; }

    public string WeeklyTarget { get; set; } = "";

    public string StreakMinimum { get; set; } = "";

    public bool CustomWorkoutsCount { get; set; }

    public string DayDonePercent { get; set; } = "";

    public static TrackSettingsForm From(Track track) => track.Rhythm switch
    {
        TrackRhythm.Weekly weekly => new()
        {
            Name = track.Name,
            Cadence = TrackCadence.Weekly,
            WeeklyTarget = weekly.WeeklyTarget.ToString(CultureInfo.InvariantCulture),
            StreakMinimum = weekly.StreakMinWorkoutsPerWeek.ToString(CultureInfo.InvariantCulture),
            CustomWorkoutsCount = weekly.StreakCountsCustomWorkouts,
        },
        TrackRhythm.Daily daily => new()
        {
            Name = track.Name,
            Cadence = TrackCadence.Daily,
            DayDonePercent = daily.DayDonePercent.ToString(CultureInfo.InvariantCulture),
        },
        _ => throw new InvalidOperationException(),
    };

    /// <summary>The cadence-specific settings these fields spell out, or the plain reason they cannot be read.</summary>
    public PlanEditResult<TrackSettings> ReadSettings(Track track) => track.Rhythm switch
    {
        TrackRhythm.Weekly => ReadWeekly(),
        TrackRhythm.Daily => ReadDaily(),
        _ => new PlanEditResult<TrackSettings>.Invalid("A Track's cadence cannot be changed."),
    };

    private PlanEditResult<TrackSettings> ReadWeekly()
    {
        if (!int.TryParse(WeeklyTarget, NumberStyles.None, CultureInfo.InvariantCulture, out var target))
        {
            return new PlanEditResult<TrackSettings>.Invalid("The weekly target must be a whole number, at least 1.");
        }

        if (!int.TryParse(StreakMinimum, NumberStyles.None, CultureInfo.InvariantCulture, out var minimum))
        {
            return new PlanEditResult<TrackSettings>.Invalid("The Streak minimum must be a whole number, at least 1.");
        }

        return new PlanEditResult<TrackSettings>.Valid(new TrackSettings.Weekly(target, minimum, CustomWorkoutsCount));
    }

    private PlanEditResult<TrackSettings> ReadDaily()
    {
        if (!int.TryParse(DayDonePercent, NumberStyles.Integer, CultureInfo.InvariantCulture, out var percent))
        {
            return new PlanEditResult<TrackSettings>.Invalid("The day-done percentage must be between 1 and 100.");
        }

        return new PlanEditResult<TrackSettings>.Valid(new TrackSettings.Daily(percent));
    }
}

/// <summary>
/// The Settings tab's Plan-level and per-Track fields as the owner typed them. Text stays text until it is read (<see cref="ApplyTo"/>),
/// so a half-typed number is kept on screen and only blocks Save, and the form survives a switch of tab or Track. Two forms are equal when
/// every Plan field and every Track form is.
/// </summary>
public sealed record PlanSettingsForm
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly Dictionary<string, TrackSettingsForm> _tracks = [];

    public string StartDate { get; set; } = "";

    public string Goal { get; set; } = "";

    public string GoalDate { get; set; } = "";

    public string WeightIncrement { get; set; } = "";

    public static PlanSettingsForm From(Plan plan)
    {
        var form = new PlanSettingsForm
        {
            StartDate = plan.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            Goal = plan.Goal ?? "",
            GoalDate = plan.GoalDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "",
            WeightIncrement = plan.WeightIncrementKg.ToString("0.##", CultureInfo.InvariantCulture),
        };
        foreach (var track in plan.Tracks)
        {
            form._tracks[track.Id] = TrackSettingsForm.From(track);
        }

        return form;
    }

    /// <summary>The form fields for <paramref name="track"/>, creating them from the Track if not yet present.</summary>
    public TrackSettingsForm ForTrack(Track track)
    {
        if (!_tracks.TryGetValue(track.Id, out var trackForm))
        {
            trackForm = TrackSettingsForm.From(track);
            _tracks[track.Id] = trackForm;
        }

        return trackForm;
    }

    /// <summary>A deep copy of this form and each Track's fields, for snapshot caching.</summary>
    public PlanSettingsForm Copy()
    {
        var copy = new PlanSettingsForm
        {
            StartDate = StartDate,
            Goal = Goal,
            GoalDate = GoalDate,
            WeightIncrement = WeightIncrement,
        };
        foreach (var (trackId, trackForm) in _tracks)
        {
            copy._tracks[trackId] = trackForm with { };
        }

        return copy;
    }

    public bool Equals(PlanSettingsForm? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (StartDate != other.StartDate
            || Goal != other.Goal
            || GoalDate != other.GoalDate
            || WeightIncrement != other.WeightIncrement
            || _tracks.Count != other._tracks.Count)
        {
            return false;
        }

        foreach (var (trackId, trackForm) in _tracks)
        {
            if (!other._tracks.TryGetValue(trackId, out var otherTrackForm) || trackForm != otherTrackForm)
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode() => HashCode.Combine(StartDate, Goal, GoalDate, WeightIncrement, _tracks.Count);

    /// <summary>
    /// Applies the Plan settings and every Track's name and rhythm settings to <paramref name="plan"/>, or returns the plain reason they
    /// cannot be read or are refused by the Plan or Track rules (checking the selected Track first so its message is shown on screen).
    /// </summary>
    public PlanEditResult<Plan> ApplyTo(Plan plan, string selectedTrackId)
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

        if (!DecimalText.TryParse(WeightIncrement, out var increment))
        {
            return Invalid("The weight increment must be a number, more than 0.");
        }

        if (plan.Tracks.FirstOrDefault(t => t.Id == selectedTrackId) is { } selectedTrack)
        {
            var selectedCheck = ValidateAndApplyTrack(plan, selectedTrack);
            if (!selectedCheck.TryGet(out _, out var selectedReason))
            {
                return Invalid(selectedReason);
            }
        }

        var planSettings = new PlanSettings(start, Goal, goalDate, increment);
        if (!PlanSettingsRules.WithSettings(plan, planSettings).TryGet(out var current, out var planReason))
        {
            return Invalid(planReason);
        }

        foreach (var track in plan.Tracks)
        {
            if (!ValidateAndApplyTrack(current, track).TryGet(out var updated, out var trackReason))
            {
                return Invalid(trackReason);
            }

            current = updated;
        }

        return new PlanEditResult<Plan>.Valid(current);
    }

    private PlanEditResult<Plan> ValidateAndApplyTrack(Plan current, Track track)
    {
        var trackForm = ForTrack(track);
        if (!TrackEditRules.RenameTrack(current, track.Id, trackForm.Name).TryGet(out var renamed, out var nameReason))
        {
            return Invalid(nameReason);
        }

        if (!trackForm.ReadSettings(track).TryGet(out var trackSettings, out var settingsReason))
        {
            return Invalid(settingsReason);
        }

        return TrackEditRules.WithSettings(renamed, track.Id, trackSettings);
    }

    private static PlanEditResult<Plan> Invalid(string message) => new PlanEditResult<Plan>.Invalid(message);
}

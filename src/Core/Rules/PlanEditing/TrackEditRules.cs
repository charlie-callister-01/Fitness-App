using Core.Ids;
using Core.Model;

namespace Core.Rules.PlanEditing;

/// <summary>
/// The rules for adding, renaming, configuring and reordering Tracks in a Plan.
/// Tracks cannot be removed and a Track's cadence never changes after creation.
/// </summary>
public static class TrackEditRules
{
    private const string MissingTrack = "That Track is no longer in the Plan.";

    /// <summary>
    /// Appends a new Track with a fresh id and the app's defaults (weekly: target 3, Streak minimum 2, Custom workouts off;
    /// daily: 75% day-done). Blank or duplicate names (ignoring case and surrounding spaces) are refused.
    /// </summary>
    public static PlanEditResult<Plan> AddTrack(Plan plan, string? name, TrackCadence cadence, IIdGenerator ids)
    {
        if (!ValidName(plan, trackId: null, name).TryGet(out var validName, out var reason))
        {
            return Invalid(reason);
        }

        if (plan.Tracks.Count == 0 && cadence == TrackCadence.Daily)
        {
            return Invalid("The main Track must be weekly, so a daily Track cannot be first.");
        }

        TrackRhythm rhythm = cadence switch
        {
            TrackCadence.Weekly => new TrackRhythm.Weekly(3, 2, false),
            TrackCadence.Daily => new TrackRhythm.Daily(75),
            _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, null),
        };

        var track = new Track(ids.NewId(), validName, rhythm, []);
        return new PlanEditResult<Plan>.Valid(plan with { Tracks = [.. plan.Tracks, track] });
    }

    /// <summary>Renames one Track. Blank or duplicate names (ignoring case and surrounding spaces) are refused.</summary>
    public static PlanEditResult<Plan> RenameTrack(Plan plan, string trackId, string? name)
    {
        if (plan.Tracks.All(t => t.Id != trackId))
        {
            return Invalid(MissingTrack);
        }

        if (!ValidName(plan, trackId, name).TryGet(out var validName, out var reason))
        {
            return Invalid(reason);
        }

        return new PlanEditResult<Plan>.Valid(ReplaceTrack(plan, trackId, t => t with { Name = validName }));
    }

    /// <summary>Replaces one Track's rhythm settings (delegating validation to <see cref="PlanSettingsRules.WithTrackSettings"/>).</summary>
    public static PlanEditResult<Plan> WithSettings(Plan plan, string trackId, TrackSettings settings)
    {
        if (plan.Tracks.FirstOrDefault(t => t.Id == trackId) is not { } track)
        {
            return Invalid(MissingTrack);
        }

        return PlanSettingsRules.WithTrackSettings(track, settings)
            .Match<PlanEditResult<Plan>>(
                updated => new PlanEditResult<Plan>.Valid(ReplaceTrack(plan, trackId, _ => updated)),
                Invalid);
    }

    /// <summary>Renames one Track and updates its rhythm settings together.</summary>
    public static PlanEditResult<Plan> EditTrack(Plan plan, string trackId, string? name, TrackSettings settings) =>
        RenameTrack(plan, trackId, name)
            .Match(
                renamed => WithSettings(renamed, trackId, settings),
                Invalid);

    /// <summary>Moves a Track one position earlier. Moving past the start or putting a daily Track first is refused.</summary>
    public static PlanEditResult<Plan> MoveUp(Plan plan, string trackId) => Move(plan, trackId, delta: -1);

    /// <summary>Moves a Track one position later. Moving past the end or putting a daily Track first is refused.</summary>
    public static PlanEditResult<Plan> MoveDown(Plan plan, string trackId) => Move(plan, trackId, delta: 1);

    private static PlanEditResult<Plan> Move(Plan plan, string trackId, int delta)
    {
        var index = FindIndex(plan.Tracks, trackId);
        if (index < 0)
        {
            return Invalid(MissingTrack);
        }

        var target = index + delta;
        if (target < 0)
        {
            return Invalid("This Track is already first.");
        }

        if (target >= plan.Tracks.Count)
        {
            return Invalid("This Track is already last.");
        }

        var reordered = plan.Tracks.ToList();
        (reordered[index], reordered[target]) = (reordered[target], reordered[index]);
        if (reordered[0].Rhythm is TrackRhythm.Daily)
        {
            return Invalid("The main Track must be weekly, so a daily Track cannot be first.");
        }

        if (reordered[0].Id != plan.Tracks[0].Id
            && plan.Sessions.Any(s => s.TrackId == plan.Tracks[0].Id && s.BenchmarkIntervalWeeks is not null))
        {
            return Invalid("Only a Session on the main Track can have a benchmark interval.");
        }

        return new PlanEditResult<Plan>.Valid(plan with { Tracks = reordered });
    }

    private static int FindIndex(IReadOnlyList<Track> tracks, string trackId)
    {
        for (var i = 0; i < tracks.Count; i++)
        {
            if (tracks[i].Id == trackId)
            {
                return i;
            }
        }

        return -1;
    }

    private static PlanEditResult<string> ValidName(Plan plan, string? trackId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new PlanEditResult<string>.Invalid("Give the Track a name.");
        }

        var trimmed = name.Trim();
        var existing = plan.Tracks.FirstOrDefault(t => t.Id != trackId && string.Equals(t.Name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
        return existing is not null
            ? new PlanEditResult<string>.Invalid($"A Track named \"{existing.Name}\" already exists in this Plan.")
            : new PlanEditResult<string>.Valid(trimmed);
    }

    private static Plan ReplaceTrack(Plan plan, string trackId, Func<Track, Track> change) =>
        plan with { Tracks = [.. plan.Tracks.Select(t => t.Id == trackId ? change(t) : t)] };

    private static PlanEditResult<Plan> Invalid(string message) => new PlanEditResult<Plan>.Invalid(message);
}

namespace Core.Model;

/// <summary>A body-weight entry made on any day, independent of any Workout.</summary>
public sealed record WeighIn(string Id, DateOnly Date, double WeightKg);

/// <summary>A date range (inclusive of both ends) excused from the weekly target and Streak.</summary>
public sealed record Break(string Id, DateOnly StartDate, DateOnly EndDate, string Reason);

/// <summary>An achievement definition. Progress is always worked out from history and never stored.</summary>
public sealed record Badge(string Id, string Name, string Metric, double Target);

/// <summary>
/// The reference page for an Exercise, keyed by Exercise ID. Everything except <see cref="PersonalNotes"/> is Guide content
/// that an Import may replace; personal notes are the owner's own and are kept apart so replacing content never overwrites them.
/// </summary>
public sealed record Guide(
    string ExerciseId,
    string? Works,
    string? WhyInPlan,
    string? Setup,
    string? HowTo,
    IReadOnlyList<string> Cues,
    IReadOnlyList<string> CommonMistakes,
    string? Easier,
    string? Harder,
    IReadOnlyList<string> WatchOuts,
    string? VideoUrl,
    string? PersonalNotes);

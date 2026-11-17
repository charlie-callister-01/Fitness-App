using System.Collections.Immutable;
using Core.Model;
using Core.Serialisation;

namespace Core.Storage;

/// <summary>
/// The whole dataset as one immutable value, passed to the rules as arguments. Every collection is present (empty, never missing)
/// and Settings is always set. A snapshot never changes after it is built; a later commit builds a new one.
/// </summary>
public sealed record DataSnapshot
{
    public IReadOnlyList<Exercise> Exercises { get; init; } = [];

    public Settings Settings { get; init; } = Settings.Default;

    public IReadOnlyList<WeighIn> WeighIns { get; init; } = [];

    public IReadOnlyList<Break> Breaks { get; init; } = [];

    public IReadOnlyList<Badge> Badges { get; init; } = [];

    public IReadOnlyList<Guide> Guides { get; init; } = [];

    public IReadOnlyList<Plan> Plans { get; init; } = [];

    public IReadOnlyList<Workout> Workouts { get; init; } = [];

    public IReadOnlyList<WeekSchedule> WeekSchedules { get; init; } = [];

    public static DataSnapshot Empty { get; } = new();

    /// <summary>True when no record of any kind exists. Settings do not count: they always exist, so an empty dataset still has them.</summary>
    public bool IsEmpty =>
        Exercises.Count + WeighIns.Count + Breaks.Count + Badges.Count + Guides.Count + Plans.Count + Workouts.Count + WeekSchedules.Count == 0;

    /// <summary>This snapshot as an exchange document, for validation and for writing a Backup.</summary>
    public ExchangeDocument ToDocument() =>
        new(ExchangeFormat.CurrentVersion, null, null, Exercises, Settings, WeighIns, Breaks, Badges, Guides, Plans, Workouts, WeekSchedules);

    /// <summary>Copies a read document's collections into frozen lists, so nothing that read it can change the snapshot afterwards.</summary>
    internal static DataSnapshot From(ExchangeDocument document) => new()
    {
        Exercises = Freeze(document.Exercises),
        Settings = document.Settings ?? Settings.Default,
        WeighIns = Freeze(document.WeighIns),
        Breaks = Freeze(document.Breaks),
        Badges = Freeze(document.Badges),
        Guides = Freeze(document.Guides),
        Plans = Freeze(document.Plans),
        Workouts = Freeze(document.Workouts),
        WeekSchedules = Freeze(document.WeekSchedules),
    };

    private static ImmutableArray<T> Freeze<T>(IReadOnlyList<T>? items) => items is null ? [] : [.. items];
}

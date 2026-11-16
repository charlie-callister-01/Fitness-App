namespace Core.Model;

/// <summary>
/// The single root object of the exchange format (Backup, Export and Import are all this document).
/// A null collection means "not part of this document", which is how partial documents work.
/// </summary>
public sealed record ExchangeDocument(
    int FormatVersion,
    DateTimeOffset? ExportedAt,
    string? ExportedFrom,
    IReadOnlyList<Exercise>? Exercises,
    Settings? Settings,
    IReadOnlyList<WeighIn>? WeighIns = null,
    IReadOnlyList<Break>? Breaks = null,
    IReadOnlyList<Badge>? Badges = null,
    IReadOnlyList<Guide>? Guides = null,
    IReadOnlyList<Plan>? Plans = null,
    IReadOnlyList<Workout>? Workouts = null);

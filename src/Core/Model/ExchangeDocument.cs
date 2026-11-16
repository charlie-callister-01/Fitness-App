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
    Settings? Settings);

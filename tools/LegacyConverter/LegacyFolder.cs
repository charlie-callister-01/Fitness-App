namespace LegacyConverter;

/// <summary>
/// In-memory representation of a legacy Obsidian Fitness folder so conversion is pure and testable without I/O.
/// </summary>
public sealed record LegacyFolder(
    string ConfigMarkdown,
    IReadOnlyDictionary<string, string> Guides,
    IReadOnlyDictionary<string, string> Logs,
    string? LifeSettingsMarkdown = null,
    string? WeightsCsv = null);

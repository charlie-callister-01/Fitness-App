using Ui.Components;

namespace Ui;

/// <summary>
/// The screens listed on the More page, defined once. The More page lists them, and the TabBar keeps the More tab active on all of them
/// (and their nested routes). Adding a screen to More is one edit here. Hrefs are single segments, relative to the document base.
/// </summary>
public static class MoreDestinations
{
    public sealed record Entry(string Label, IconName Icon, string Href);

    /// <summary>The More page itself, which the More tab links to.</summary>
    public const string MoreHref = "more";

    public static IReadOnlyList<Entry> Entries { get; } =
    [
        new("Plans", IconName.Phase, "plans"),
        new("Exercise library", IconName.Guide, "exercises"),
        new("Weight", IconName.Weight, "weight"),
        new("Weekly review", IconName.Calendar, "weekly-review"),
        new("Export and Import", IconName.Export, "backup"),
        new("Settings", IconName.Settings, "settings"),
    ];

    /// <summary>True when the first path segment is the More page or one of its entries.</summary>
    public static bool Owns(string firstSegment) =>
        firstSegment == MoreHref || Entries.Any(entry => entry.Href == firstSegment);
}

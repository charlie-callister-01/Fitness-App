using Core.Model;
using Core.Storage;

namespace Core.Exchange;

public enum ImportItemAction
{
    /// <summary>A record the Import adds.</summary>
    Add,

    /// <summary>A record the Import replaces or updates.</summary>
    Change,

    /// <summary>A record the Import leaves as it is, though the file has a different version of it.</summary>
    Keep,

    /// <summary>Part of the file the chosen kind does not use.</summary>
    Ignore,

    /// <summary>Something that is allowed but the owner should know about.</summary>
    Note,

    Warning,

    /// <summary>Stops the Import until the file or the choices change.</summary>
    Blocker,
}

/// <summary>
/// One line of the preview. <see cref="Kind"/> is null when the line is about the file as a whole. <see cref="Key"/> is the record's key
/// (an Exercise ID for a Guide), or the document path for a finding. <see cref="OffersReplace"/> is true for a <see cref="ImportItemAction.Keep"/>
/// the owner may turn into a replace through <see cref="ImportDecisions"/>.
/// </summary>
public sealed record ImportItem(ImportItemAction Action, RecordKind? Kind, string Key, string Text, bool OffersReplace = false);

/// <summary>
/// Everything the owner needs to confirm an Import, and everything the apply step needs to commit it. <see cref="Changes"/> is the exact
/// batch that will be committed (empty for a kind that replaces everything, Restore); <see cref="Source"/> is the snapshot the preview was built from,
/// so a later apply can refuse a preview that is out of date. <see cref="Replacement"/> is set only for Restore: the whole document to pass to
/// <c>AppData.ReplaceAllAsync</c>, with <see cref="BackupStamp"/> (the file's <c>exportedAt</c>, else the time the caller gave) as its Backup date.
/// </summary>
public sealed record ImportPreview(
    ImportKind Kind,
    IReadOnlyList<string> Summary,
    IReadOnlyList<ImportItem> Items,
    IReadOnlyList<ExerciseMatch> Matches,
    IReadOnlyList<Change> Changes,
    DataSnapshot Source,
    ExchangeDocument? Replacement = null,
    DateTimeOffset? BackupStamp = null)
{
    /// <summary>The near-matches still waiting for the owner.</summary>
    public IEnumerable<ExerciseMatch> OpenChoices => Matches.Where(m => m.Outcome == ExerciseOutcome.NeedsDecision);

    public IEnumerable<ImportItem> Blockers => Items.Where(i => i.Action == ImportItemAction.Blocker);

    /// <summary>True only when nothing blocks the Import and no choice is open.</summary>
    public bool IsReady => !Blockers.Any() && !OpenChoices.Any();
}

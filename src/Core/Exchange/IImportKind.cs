using Core.Storage;

namespace Core.Exchange;

/// <summary>What a kind's rules get to work with: the library, the file, the Exercise matching and the owner's answers.</summary>
internal sealed record ImportContext(DataSnapshot Snapshot, Inspected Inspected, ExerciseMatching Matching, ImportDecisions Decisions);

/// <summary>What a kind decided: the changes to commit, the preview lines for them and the summary sentences.</summary>
internal sealed record KindResult(IReadOnlyList<Change> Changes, IReadOnlyList<ImportItem> Items, IReadOnlyList<string> Summary)
{
    /// <summary>This result followed by <paramref name="next"/>: changes, items and sentences in that order.</summary>
    public KindResult Then(KindResult next) =>
        new([.. Changes, .. next.Changes], [.. Items, .. next.Items], [.. Summary, .. next.Summary]);

    /// <summary>
    /// The closing sentence for a kind that never touches Workouts: with no changes, "Nothing to change." first and then the sentences
    /// so far (what was kept); otherwise the sentences so far and "Touches no Workouts."
    /// </summary>
    public KindResult ClosedForKindsThatLeaveWorkoutsAlone() =>
        this with { Summary = Changes.Count == 0 ? ["Nothing to change.", .. Summary] : [.. Summary, "Touches no Workouts."] };
}

/// <summary>
/// The rules of one Import kind. <see cref="ImportPreviewer"/> owns everything the kinds share: matching, listing what the kind ignores,
/// dropping findings about ignored parts, validating the result and assembling the preview.
/// </summary>
internal interface IImportKind
{
    /// <summary>How the preview names this kind in a sentence, such as "A Plan import".</summary>
    string Name { get; }

    /// <summary>The top-level collections of the file (their wire names) this kind reads. Every other collection present is ignored.</summary>
    IReadOnlySet<string> Reads { get; }

    KindResult Build(ImportContext context);
}
